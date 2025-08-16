using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using shelf_project.Data;
using shelf_project.Models;
using shelf_project.Services;

namespace shelf_project.Controllers
{
    [Authorize]
    public class DistributorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IQRCodeService _qrCodeService;

        public DistributorController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IQRCodeService qrCodeService)
        {
            _context = context;
            _userManager = userManager;
            _qrCodeService = qrCodeService;
        }

        public async Task<IActionResult> Index(int? locationId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Distributor)
            {
                return Forbid();
            }

            // Get all distributors - if user is head office, get all in company; otherwise just user's distributors
            var userDistributors = await _context.Distributors
                .Include(d => d.Company)
                .Where(d => d.UserId == user.Id && d.IsActive)
                .ToListAsync();

            List<Distributor> distributors;
            
            // Check if user has head office role in a chain company
            var headOfficeDistributor = userDistributors.FirstOrDefault(d => 
                d.DistributorType == DistributorType.HeadOffice && 
                d.Company?.CompanyType == CompanyType.Chain);

            if (headOfficeDistributor != null && locationId.HasValue)
            {
                // Head office user accessing specific location - get that specific distributor
                var targetDistributor = await _context.Distributors
                    .Include(d => d.Company)
                    .Include(d => d.Subscriptions)
                    .Include(d => d.DistributorProducts)
                    .ThenInclude(dp => dp.Product)
                    .FirstOrDefaultAsync(d => d.Id == locationId.Value && 
                                           d.CompanyId == headOfficeDistributor.CompanyId && 
                                           d.IsActive);
                
                if (targetDistributor != null)
                {
                    return View(targetDistributor);
                }
            }

            // Get distributors with full data for normal processing
            distributors = await _context.Distributors
                .Include(d => d.Company)
                .Include(d => d.Subscriptions)
                .Include(d => d.DistributorProducts)
                .ThenInclude(dp => dp.Product)
                .Where(d => d.UserId == user.Id && d.IsActive)
                .ToListAsync();

            if (!distributors.Any())
            {
                // 新規契約の場合は契約ページにリダイレクト
                return RedirectToAction("NewContract");
            }

            // If user has multiple locations and specific locationId is provided
            if (distributors.Count > 1 && locationId.HasValue)
            {
                var selectedDistributor = distributors.FirstOrDefault(d => d.Id == locationId.Value);
                if (selectedDistributor != null)
                {
                    return View(selectedDistributor);
                }
            }
            
            // For chain stores (multiple locations), redirect to Company Dashboard
            if (distributors.Count > 1)
            {
                var headOffice = distributors.FirstOrDefault(d => d.DistributorType == DistributorType.HeadOffice);
                if (headOffice?.Company?.CompanyType == CompanyType.Chain)
                {
                    return RedirectToAction("Dashboard", "Company");
                }
            }

            return View(distributors.First());
        }


        public async Task<IActionResult> NewContract()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Distributor)
            {
                return Forbid();
            }

            // 既に代理店プロファイルがある場合はダッシュボードにリダイレクト
            var existingDistributor = await _context.Distributors
                .FirstOrDefaultAsync(d => d.UserId == user.Id && d.IsActive);

            if (existingDistributor != null)
            {
                return RedirectToAction("Index");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> NewContract(string companyName, string locationName, string? address, string? phoneNumber, string? headOfficeCode)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Distributor)
            {
                return Forbid();
            }

            // 既に代理店プロファイルがある場合はエラー
            var existingDistributor = await _context.Distributors
                .FirstOrDefaultAsync(d => d.UserId == user.Id && d.IsActive);

            if (existingDistributor != null)
            {
                TempData["Error"] = "既に契約が存在します。";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(companyName) || string.IsNullOrWhiteSpace(locationName))
            {
                TempData["Error"] = "会社名と拠点名は必須項目です。";
                return View();
            }

            Company? parentCompany = null;
            DistributorType distributorType = DistributorType.Individual;

            // 本社コードが入力されている場合、チェーン店として登録
            if (!string.IsNullOrWhiteSpace(headOfficeCode))
            {
                parentCompany = await _context.Companies
                    .FirstOrDefaultAsync(c => c.HeadOfficeCode == headOfficeCode && c.IsActive);

                if (parentCompany == null)
                {
                    TempData["Error"] = "指定された本社コードが見つかりません。";
                    return View();
                }

                distributorType = DistributorType.Store;
            }
            else
            {
                // 本社コードがない場合は独立した代理店として新しい会社を作成
                var companyCode = GenerateUniqueCompanyCode();
                parentCompany = new Company
                {
                    CompanyName = companyName,
                    CompanyType = CompanyType.Individual,
                    OwnerUserId = user.Id,
                    HeadOfficeCode = companyCode,
                    HeadquartersAddress = address,
                    IsActive = true
                };
                _context.Companies.Add(parentCompany);
                await _context.SaveChangesAsync();

                distributorType = DistributorType.Individual;
            }

            // 新しい代理店を作成
            var newDistributor = new Distributor
            {
                UserId = user.Id,
                CompanyId = parentCompany.Id,
                CompanyName = companyName,
                LocationName = locationName,
                Address = address,
                PhoneNumber = phoneNumber,
                DistributorType = distributorType,
                IsHeadquarters = distributorType == DistributorType.Individual,
                ContractStartDate = DateTime.Now,
                IsActive = true
            };

            _context.Distributors.Add(newDistributor);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"契約が完了しました。{(distributorType == DistributorType.Store ? "チェーン店" : "独立代理店")}として登録されました。";
            return RedirectToAction("Index");
        }

        private string GenerateUniqueCompanyCode()
        {
            string code;
            do
            {
                // 8桁のランダムコードを生成
                code = new Random().Next(10000000, 99999999).ToString();
            }
            while (_context.Companies.Any(c => c.HeadOfficeCode == code));

            return code;
        }

        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Distributor)
            {
                return Forbid();
            }

            var distributor = await _context.Distributors
                .Include(d => d.Sales)
                .ThenInclude(s => s.Order)
                .ThenInclude(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(d => d.UserId == user.Id);

            if (distributor == null)
            {
                return NotFound();
            }

            ViewBag.MonthlySales = distributor.Sales?
                .Where(s => s.SaleDate.Month == DateTime.Now.Month && s.SaleDate.Year == DateTime.Now.Year)
                .Sum(s => s.TotalAmount) ?? 0;

            ViewBag.MonthlyCommission = distributor.Sales?
                .Where(s => s.SaleDate.Month == DateTime.Now.Month && s.SaleDate.Year == DateTime.Now.Year)
                .Sum(s => s.DistributorCommission) ?? 0;

            return View(distributor);
        }

        public async Task<IActionResult> Products(string? search, string? category, int? manufacturerId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Distributor)
            {
                return Forbid();
            }

            var distributor = await _context.Distributors
                .FirstOrDefaultAsync(d => d.UserId == user.Id);

            if (distributor == null)
            {
                return NotFound();
            }

            var assignedProducts = await _context.DistributorProducts
                .Include(dp => dp.Product)
                .ThenInclude(p => p.Manufacturer)
                .Where(dp => dp.DistributorId == distributor.Id && dp.IsActive)
                .ToListAsync();

            var availableProductsQuery = _context.Products
                .Include(p => p.Manufacturer)
                .Where(p => p.IsActive && !assignedProducts.Select(ap => ap.ProductId).Contains(p.Id));

            // Apply search filters
            if (!string.IsNullOrEmpty(search))
            {
                availableProductsQuery = availableProductsQuery.Where(p => 
                    p.Name.Contains(search) || 
                    p.Description!.Contains(search) || 
                    p.Manufacturer.CompanyName.Contains(search));
            }

            if (!string.IsNullOrEmpty(category))
            {
                availableProductsQuery = availableProductsQuery.Where(p => p.Category == category);
            }

            if (manufacturerId.HasValue)
            {
                availableProductsQuery = availableProductsQuery.Where(p => p.ManufacturerId == manufacturerId.Value);
            }

            var availableProducts = await availableProductsQuery.ToListAsync();

            // Get categories and manufacturers for filters
            var categories = await _context.Products
                .Where(p => p.IsActive && !string.IsNullOrEmpty(p.Category))
                .Select(p => p.Category!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            var manufacturers = await _context.Manufacturers
                .Where(m => m.IsActive)
                .OrderBy(m => m.CompanyName)
                .ToListAsync();

            ViewBag.AssignedProducts = assignedProducts;
            ViewBag.MaxProducts = distributor.ProductSelectionCount;
            ViewBag.Categories = categories;
            ViewBag.Manufacturers = manufacturers;
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentCategory = category;
            ViewBag.CurrentManufacturerId = manufacturerId;

            return View(availableProducts);
        }

        public async Task<IActionResult> Manufacturers()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Distributor)
            {
                return Forbid();
            }

            var manufacturers = await _context.Manufacturers
                .Where(m => m.IsActive)
                .Include(m => m.Products)
                .OrderBy(m => m.CompanyName)
                .ToListAsync();

            return View(manufacturers);
        }

        public async Task<IActionResult> ManufacturerProducts(int manufacturerId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Distributor)
            {
                return Forbid();
            }

            var distributor = await _context.Distributors
                .FirstOrDefaultAsync(d => d.UserId == user.Id);

            if (distributor == null)
            {
                return NotFound();
            }

            var manufacturer = await _context.Manufacturers
                .Include(m => m.Products)
                .FirstOrDefaultAsync(m => m.Id == manufacturerId && m.IsActive);

            if (manufacturer == null)
            {
                return NotFound();
            }

            var assignedProductIds = await _context.DistributorProducts
                .Where(dp => dp.DistributorId == distributor.Id && dp.IsActive)
                .Select(dp => dp.ProductId)
                .ToListAsync();

            var assignedProducts = await _context.DistributorProducts
                .Include(dp => dp.Product)
                .ThenInclude(p => p.Manufacturer)
                .Where(dp => dp.DistributorId == distributor.Id && dp.IsActive)
                .ToListAsync();

            var availableProducts = manufacturer.Products?
                .Where(p => p.IsActive && !assignedProductIds.Contains(p.Id))
                .ToList() ?? new List<Product>();

            ViewBag.Manufacturer = manufacturer;
            ViewBag.AssignedProducts = assignedProducts;
            ViewBag.MaxProducts = distributor.ProductSelectionCount;

            return View(availableProducts);
        }

        [HttpPost]
        public async Task<IActionResult> AddProduct(int productId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Distributor)
            {
                return Forbid();
            }

            var distributor = await _context.Distributors
                .Include(d => d.DistributorProducts)
                .FirstOrDefaultAsync(d => d.UserId == user.Id);

            if (distributor == null)
            {
                return NotFound();
            }

            var activeProductsCount = distributor.DistributorProducts?.Count(dp => dp.IsActive) ?? 0;
            if (activeProductsCount >= distributor.ProductSelectionCount)
            {
                TempData["Error"] = "選択可能な商品数の上限に達しています。";
                return RedirectToAction("Products");
            }

            var distributorProduct = new DistributorProduct
            {
                DistributorId = distributor.Id,
                ProductId = productId
            };

            _context.DistributorProducts.Add(distributorProduct);
            await _context.SaveChangesAsync();

            TempData["Success"] = "商品を追加しました。";
            return RedirectToAction("Products");
        }

        [HttpPost]
        public async Task<IActionResult> RemoveProduct(int productId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Distributor)
            {
                return Forbid();
            }

            var distributor = await _context.Distributors
                .FirstOrDefaultAsync(d => d.UserId == user.Id);

            if (distributor == null)
            {
                return NotFound();
            }

            var distributorProduct = await _context.DistributorProducts
                .FirstOrDefaultAsync(dp => dp.DistributorId == distributor.Id && dp.ProductId == productId && dp.IsActive);

            if (distributorProduct != null)
            {
                distributorProduct.IsActive = false;
                distributorProduct.RemovedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                TempData["Success"] = "商品を削除しました。";
            }

            return RedirectToAction("Products");
        }

        public async Task<IActionResult> Sales(int? locationId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Distributor)
            {
                return Forbid();
            }

            // Get all distributors for the user
            var userDistributors = await _context.Distributors
                .Include(d => d.Company)
                .Where(d => d.UserId == user.Id && d.IsActive)
                .ToListAsync();

            if (!userDistributors.Any())
            {
                return NotFound();
            }

            // Check if user has head office role and accessing specific location
            var headOfficeDistributor = userDistributors.FirstOrDefault(d => 
                d.DistributorType == DistributorType.HeadOffice && 
                d.Company?.CompanyType == CompanyType.Chain);

            Distributor selectedDistributor;
            List<Distributor> allDistributors;

            if (headOfficeDistributor != null && locationId.HasValue)
            {
                // Head office accessing specific location - get that distributor
                selectedDistributor = await _context.Distributors
                    .FirstOrDefaultAsync(d => d.Id == locationId.Value && 
                                           d.CompanyId == headOfficeDistributor.CompanyId && 
                                           d.IsActive);
                
                if (selectedDistributor == null)
                {
                    return NotFound();
                }

                // Get all distributors in the company for the selector
                allDistributors = await _context.Distributors
                    .Where(d => d.CompanyId == headOfficeDistributor.CompanyId && d.IsActive)
                    .ToListAsync();
            }
            else
            {
                // Normal processing for user's own distributors
                if (locationId.HasValue)
                {
                    selectedDistributor = userDistributors.FirstOrDefault(d => d.Id == locationId.Value);
                    if (selectedDistributor == null)
                    {
                        return NotFound();
                    }
                }
                else
                {
                    selectedDistributor = userDistributors.First();
                }
                allDistributors = userDistributors;
            }

            // Get sales for the selected distributor/location only
            var sales = await _context.Sales
                .Include(s => s.Order)
                .ThenInclude(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(s => s.DistributorId == selectedDistributor.Id)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            ViewBag.SelectedDistributor = selectedDistributor;
            ViewBag.AllDistributors = allDistributors;
            return View(sales);
        }

        public async Task<IActionResult> QRCodes(int? locationId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Distributor)
            {
                return Forbid();
            }

            // Get all distributors for the user
            var userDistributors = await _context.Distributors
                .Include(d => d.Company)
                .Where(d => d.UserId == user.Id && d.IsActive)
                .ToListAsync();

            if (!userDistributors.Any())
            {
                return NotFound();
            }

            // Check if user has head office role and accessing specific location
            var headOfficeDistributor = userDistributors.FirstOrDefault(d => 
                d.DistributorType == DistributorType.HeadOffice && 
                d.Company?.CompanyType == CompanyType.Chain);

            Distributor selectedDistributor;
            List<Distributor> allDistributors;

            if (headOfficeDistributor != null && locationId.HasValue)
            {
                // Head office accessing specific location - get that distributor
                selectedDistributor = await _context.Distributors
                    .FirstOrDefaultAsync(d => d.Id == locationId.Value && 
                                           d.CompanyId == headOfficeDistributor.CompanyId && 
                                           d.IsActive);
                
                if (selectedDistributor == null)
                {
                    return NotFound();
                }

                // Get all distributors in the company for the selector
                allDistributors = await _context.Distributors
                    .Where(d => d.CompanyId == headOfficeDistributor.CompanyId && d.IsActive)
                    .ToListAsync();
            }
            else
            {
                // Normal processing for user's own distributors
                if (locationId.HasValue)
                {
                    selectedDistributor = userDistributors.FirstOrDefault(d => d.Id == locationId.Value);
                    if (selectedDistributor == null)
                    {
                        return NotFound();
                    }
                }
                else
                {
                    selectedDistributor = userDistributors.First();
                }
                allDistributors = userDistributors;
            }

            var qrCodes = await _context.QRCodes
                .Where(q => q.DistributorId == selectedDistributor.Id)
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();

            ViewBag.DistributorId = selectedDistributor.Id;
            ViewBag.SelectedDistributor = selectedDistributor;
            ViewBag.AllDistributors = allDistributors;
            return View(qrCodes);
        }

        [HttpPost]
        public async Task<IActionResult> GenerateQRCode(string location, int distributorId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Distributor)
            {
                return Forbid();
            }

            // Check if user owns this distributor or has head office access
            var userDistributors = await _context.Distributors
                .Include(d => d.Company)
                .Where(d => d.UserId == user.Id && d.IsActive)
                .ToListAsync();

            var headOfficeDistributor = userDistributors.FirstOrDefault(d => 
                d.DistributorType == DistributorType.HeadOffice && 
                d.Company?.CompanyType == CompanyType.Chain);

            var distributor = await _context.Distributors
                .Include(d => d.QRCodes)
                .FirstOrDefaultAsync(d => d.Id == distributorId);

            if (distributor == null)
            {
                return NotFound();
            }

            // Check access permissions
            bool hasAccess = distributor.UserId == user.Id || 
                           (headOfficeDistributor != null && distributor.CompanyId == headOfficeDistributor.CompanyId);

            if (!hasAccess)
            {
                return Forbid();
            }

            // 1拠点=1QRコードの制限チェック：既存のQRコード（有効・無効問わず）があれば新規生成を禁止
            if (distributor.QRCodes?.Any() == true)
            {
                var activeCount = distributor.QRCodes.Count(q => q.IsActive);
                var inactiveCount = distributor.QRCodes.Count(q => !q.IsActive);
                
                if (activeCount > 0)
                {
                    TempData["Error"] = "この拠点には既に有効なQRコードが存在します。1拠点につき1つのQRコードのみ生成可能です。";
                }
                else
                {
                    TempData["Error"] = $"この拠点には既に{inactiveCount}個のQRコードが存在します。新しいQRコードを生成するには、既存のQRコードを完全に削除してください。";
                }
                return RedirectToAction("QRCodes", new { locationId = distributorId });
            }

            if (string.IsNullOrWhiteSpace(location))
            {
                TempData["Error"] = "設置場所を入力してください。";
                return RedirectToAction("QRCodes", new { locationId = distributorId });
            }

            var qrCodeGuid = Guid.NewGuid().ToString("N")[..8];
            
            // Generate QR code URL
            var qrCodeUrl = $"{Request.Scheme}://{Request.Host}/shop/{qrCodeGuid}";
            
            // Generate and save QR code image
            var imageUrl = _qrCodeService.SaveQRCodeImage(qrCodeGuid, qrCodeUrl);
            
            var qrCode = new QRCode
            {
                DistributorId = distributor.Id,
                Code = qrCodeGuid,
                Location = location,
                QRCodeImageUrl = imageUrl
            };

            _context.QRCodes.Add(qrCode);
            await _context.SaveChangesAsync();

            TempData["Success"] = "QRコードを生成しました。";
            return RedirectToAction("QRCodes", new { locationId = distributorId });
        }

        [HttpPost]
        public async Task<IActionResult> DeactivateQRCode(int qrCodeId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Distributor)
            {
                return Forbid();
            }

            // Check if user has head office access
            var userDistributors = await _context.Distributors
                .Include(d => d.Company)
                .Where(d => d.UserId == user.Id && d.IsActive)
                .ToListAsync();

            var headOfficeDistributor = userDistributors.FirstOrDefault(d => 
                d.DistributorType == DistributorType.HeadOffice && 
                d.Company?.CompanyType == CompanyType.Chain);

            var qrCode = await _context.QRCodes
                .Include(q => q.Distributor)
                .FirstOrDefaultAsync(q => q.Id == qrCodeId);

            if (qrCode == null)
            {
                return NotFound();
            }

            // Check access permissions
            bool hasAccess = qrCode.Distributor.UserId == user.Id || 
                           (headOfficeDistributor != null && qrCode.Distributor.CompanyId == headOfficeDistributor.CompanyId);

            if (!hasAccess)
            {
                return Forbid();
            }

            if (qrCode != null)
            {
                qrCode.IsActive = false;
                qrCode.DeactivatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                TempData["Success"] = "QRコードを無効化しました。";
            }

            return RedirectToAction("QRCodes", new { locationId = qrCode.DistributorId });
        }

        [HttpPost]
        public async Task<IActionResult> ActivateQRCode(int qrCodeId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Distributor)
            {
                return Forbid();
            }

            // Check if user has head office access
            var userDistributors = await _context.Distributors
                .Include(d => d.Company)
                .Where(d => d.UserId == user.Id && d.IsActive)
                .ToListAsync();

            var headOfficeDistributor = userDistributors.FirstOrDefault(d => 
                d.DistributorType == DistributorType.HeadOffice && 
                d.Company?.CompanyType == CompanyType.Chain);

            var qrCode = await _context.QRCodes
                .Include(q => q.Distributor)
                .FirstOrDefaultAsync(q => q.Id == qrCodeId);

            if (qrCode == null)
            {
                return NotFound();
            }

            // Check access permissions
            bool hasAccess = qrCode.Distributor.UserId == user.Id || 
                           (headOfficeDistributor != null && qrCode.Distributor.CompanyId == headOfficeDistributor.CompanyId);

            if (!hasAccess)
            {
                return Forbid();
            }

            var distributor = qrCode.Distributor;

            if (distributor == null)
            {
                return NotFound();
            }

            // 1拠点=1QRコードの制限チェック：既に有効なQRコードがある場合は有効化を拒否
            var activeQRCode = await _context.QRCodes
                .FirstOrDefaultAsync(q => q.DistributorId == distributor.Id && q.IsActive && q.Id != qrCodeId);

            if (activeQRCode != null)
            {
                TempData["Error"] = "既に有効なQRコードが存在します。1拠点につき1つのQRコードのみ有効にできます。";
                return RedirectToAction("QRCodes", new { locationId = distributor.Id });
            }

            qrCode.IsActive = true;
            qrCode.DeactivatedAt = null;
            await _context.SaveChangesAsync();

            TempData["Success"] = "QRコードを有効化しました。";
            return RedirectToAction("QRCodes", new { locationId = distributor.Id });
        }

        public async Task<IActionResult> DownloadQRCode(int qrCodeId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Distributor)
            {
                return Forbid();
            }

            var distributor = await _context.Distributors
                .FirstOrDefaultAsync(d => d.UserId == user.Id);

            if (distributor == null)
            {
                return NotFound();
            }

            var qrCode = await _context.QRCodes
                .FirstOrDefaultAsync(q => q.Id == qrCodeId && q.DistributorId == distributor.Id);

            if (qrCode == null)
            {
                return NotFound();
            }

            // Generate QR code URL
            var qrCodeUrl = $"{Request.Scheme}://{Request.Host}/shop/{qrCode.Code}";
            
            // Generate QR code image
            var imageBytes = _qrCodeService.GenerateQRCodeImage(qrCodeUrl);
            
            return File(imageBytes, "image/png", $"QRCode_{qrCode.Location}_{qrCode.Code}.png");
        }



        public async Task<IActionResult> Settings()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Distributor)
            {
                return Forbid();
            }

            var distributor = await _context.Distributors
                .Include(d => d.Company)
                .ThenInclude(c => c.Distributors)
                .FirstOrDefaultAsync(d => d.UserId == user.Id);

            if (distributor == null)
            {
                return NotFound();
            }

            return View(distributor);
        }

        [HttpPost]
        public async Task<IActionResult> UpgradeToChain()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Distributor)
            {
                return Forbid();
            }

            var distributor = await _context.Distributors
                .Include(d => d.Company)
                .FirstOrDefaultAsync(d => d.UserId == user.Id);

            if (distributor == null)
            {
                return NotFound();
            }

            // 既にチェーン店の場合
            if (distributor.Company != null && distributor.Company.CompanyType == CompanyType.Chain)
            {
                TempData["Info"] = "既にチェーン店として登録されています。";
                return RedirectToAction("Settings");
            }

            // 新しい会社を作成またはアップグレード
            if (distributor.Company == null)
            {
                var company = new Company
                {
                    CompanyName = distributor.CompanyName,
                    CompanyType = CompanyType.Chain,
                    OwnerUserId = user.Id
                };
                _context.Companies.Add(company);
                await _context.SaveChangesAsync();

                distributor.CompanyId = company.Id;
            }
            else
            {
                distributor.Company.CompanyType = CompanyType.Chain;
            }

            distributor.DistributorType = DistributorType.HeadOffice;
            distributor.IsHeadquarters = true;

            await _context.SaveChangesAsync();

            TempData["Success"] = "チェーン店として登録しました。複数拠点の管理が可能になりました。";
            return RedirectToAction("Settings");
        }

        [HttpPost]
        public async Task<IActionResult> DowngradeToIndividual()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Distributor)
            {
                return Forbid();
            }

            var distributor = await _context.Distributors
                .Include(d => d.Company)
                .ThenInclude(c => c!.Distributors)
                .FirstOrDefaultAsync(d => d.UserId == user.Id);

            if (distributor == null)
            {
                return NotFound();
            }

            // 子拠点がある場合はダウングレード不可
            if (distributor.Company?.Distributors?.Count(d => d.DistributorType == DistributorType.Store) > 0)
            {
                TempData["Error"] = "子拠点が存在するため、個人店にダウングレードできません。先に子拠点を削除してください。";
                return RedirectToAction("Settings");
            }

            if (distributor.Company != null)
            {
                distributor.Company.CompanyType = CompanyType.Individual;
            }

            distributor.DistributorType = DistributorType.Individual;
            distributor.IsHeadquarters = true;

            await _context.SaveChangesAsync();

            TempData["Success"] = "個人店にダウングレードしました。";
            return RedirectToAction("Settings");
        }

        public async Task<IActionResult> Settlement()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Distributor)
            {
                return Forbid();
            }

            var distributor = await _context.Distributors
                .Include(d => d.Sales)
                .FirstOrDefaultAsync(d => d.UserId == user.Id);

            if (distributor == null)
            {
                return NotFound();
            }

            // 自分の代理店の売上のみ
            var sales = distributor.Sales?.ToList() ?? new List<Sale>();

            ViewBag.Sales = sales;
            ViewBag.TotalRevenue = sales.Sum(s => s.TotalAmount);
            ViewBag.TotalCommission = sales.Sum(s => s.DistributorCommission);

            return View(distributor);
        }

        public async Task<IActionResult> QRCodeProducts(int qrCodeId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Distributor)
            {
                return Forbid();
            }

            // Check if user has head office access
            var userDistributors = await _context.Distributors
                .Include(d => d.Company)
                .Where(d => d.UserId == user.Id && d.IsActive)
                .ToListAsync();

            if (!userDistributors.Any())
            {
                return NotFound();
            }

            var headOfficeDistributor = userDistributors.FirstOrDefault(d => 
                d.DistributorType == DistributorType.HeadOffice && 
                d.Company?.CompanyType == CompanyType.Chain);

            var qrCode = await _context.QRCodes
                .Include(qr => qr.QRCodeProducts)
                .ThenInclude(qcp => qcp.Product)
                .ThenInclude(p => p.Manufacturer)
                .Include(qr => qr.Distributor)
                .FirstOrDefaultAsync(qr => qr.Id == qrCodeId);

            if (qrCode == null)
            {
                return NotFound();
            }

            // Check access permissions
            bool hasAccess = qrCode.Distributor.UserId == user.Id || 
                           (headOfficeDistributor != null && qrCode.Distributor.CompanyId == headOfficeDistributor.CompanyId);

            if (!hasAccess)
            {
                return Forbid();
            }

            // Get the distributor that owns this QR code
            var distributor = qrCode.Distributor;

            // 代理店が選定した商品のみを取得
            var distributorProductIds = await _context.DistributorProducts
                .Where(dp => dp.DistributorId == distributor.Id && dp.IsActive)
                .Select(dp => dp.ProductId)
                .ToListAsync();

            var availableProducts = await _context.Products
                .Include(p => p.Manufacturer)
                .Where(p => p.IsActive && distributorProductIds.Contains(p.Id))
                .ToListAsync();

            ViewBag.AvailableProducts = availableProducts;
            ViewBag.QRCode = qrCode;

            return View(qrCode.QRCodeProducts?.Where(qcp => qcp.IsActive).ToList() ?? new List<QRCodeProduct>());
        }

        [HttpPost]
        public async Task<IActionResult> AddProductToQRCode(int qrCodeId, int productId, string? notes)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Distributor)
            {
                return Forbid();
            }

            // Check if user has head office access
            var userDistributors = await _context.Distributors
                .Include(d => d.Company)
                .Where(d => d.UserId == user.Id && d.IsActive)
                .ToListAsync();

            if (!userDistributors.Any())
            {
                return NotFound();
            }

            var headOfficeDistributor = userDistributors.FirstOrDefault(d => 
                d.DistributorType == DistributorType.HeadOffice && 
                d.Company?.CompanyType == CompanyType.Chain);

            var qrCode = await _context.QRCodes
                .Include(qr => qr.Distributor)
                .FirstOrDefaultAsync(qr => qr.Id == qrCodeId);

            if (qrCode == null)
            {
                return NotFound();
            }

            // Check access permissions
            bool hasAccess = qrCode.Distributor.UserId == user.Id || 
                           (headOfficeDistributor != null && qrCode.Distributor.CompanyId == headOfficeDistributor.CompanyId);

            if (!hasAccess)
            {
                return Forbid();
            }

            // 既に同じ商品が登録されていないかチェック
            var existingAssignment = await _context.QRCodeProducts
                .FirstOrDefaultAsync(qcp => qcp.QRCodeId == qrCodeId && qcp.ProductId == productId && qcp.IsActive);

            if (existingAssignment != null)
            {
                TempData["Error"] = "この商品は既にこのQRコードに登録されています。";
                return RedirectToAction("QRCodeProducts", new { qrCodeId });
            }

            var qrCodeProduct = new QRCodeProduct
            {
                QRCodeId = qrCodeId,
                ProductId = productId,
                Notes = notes
            };

            _context.QRCodeProducts.Add(qrCodeProduct);
            await _context.SaveChangesAsync();

            TempData["Success"] = "商品をQRコードに登録しました。";
            return RedirectToAction("QRCodeProducts", new { qrCodeId });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveProductFromQRCode(int qrCodeProductId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Distributor)
            {
                return Forbid();
            }

            // Check if user has head office access
            var userDistributors = await _context.Distributors
                .Include(d => d.Company)
                .Where(d => d.UserId == user.Id && d.IsActive)
                .ToListAsync();

            if (!userDistributors.Any())
            {
                return NotFound();
            }

            var headOfficeDistributor = userDistributors.FirstOrDefault(d => 
                d.DistributorType == DistributorType.HeadOffice && 
                d.Company?.CompanyType == CompanyType.Chain);

            var qrCodeProduct = await _context.QRCodeProducts
                .Include(qcp => qcp.QRCode)
                .ThenInclude(qr => qr.Distributor)
                .FirstOrDefaultAsync(qcp => qcp.Id == qrCodeProductId);

            if (qrCodeProduct == null)
            {
                return NotFound();
            }

            // Check access permissions
            bool hasAccess = qrCodeProduct.QRCode.Distributor.UserId == user.Id || 
                           (headOfficeDistributor != null && qrCodeProduct.QRCode.Distributor.CompanyId == headOfficeDistributor.CompanyId);

            if (!hasAccess)
            {
                return Forbid();
            }

            qrCodeProduct.IsActive = false;
            qrCodeProduct.RemovedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = "商品をQRコードから削除しました。";
            return RedirectToAction("QRCodeProducts", new { qrCodeId = qrCodeProduct.QRCodeId });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteQRCode(int qrCodeId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Distributor)
            {
                return Forbid();
            }

            var distributor = await _context.Distributors
                .FirstOrDefaultAsync(d => d.UserId == user.Id);

            if (distributor == null)
            {
                return NotFound();
            }

            var qrCode = await _context.QRCodes
                .Include(q => q.QRCodeProducts)
                .FirstOrDefaultAsync(q => q.Id == qrCodeId && q.DistributorId == distributor.Id);

            if (qrCode == null)
            {
                return NotFound();
            }

            // 関連するQRCodeProductsも削除
            if (qrCode.QRCodeProducts?.Any() == true)
            {
                _context.QRCodeProducts.RemoveRange(qrCode.QRCodeProducts);
            }

            // QRコード本体を削除
            _context.QRCodes.Remove(qrCode);
            await _context.SaveChangesAsync();

            TempData["Success"] = "QRコードを完全に削除しました。";
            return RedirectToAction("QRCodes");
        }

    }
}