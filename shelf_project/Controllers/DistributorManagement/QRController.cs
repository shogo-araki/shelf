using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using shelf_project.Data;
using shelf_project.Models;
using shelf_project.Services;

namespace shelf_project.Controllers.DistributorManagement
{
    [Authorize]
    public class QRController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IQRCodeService _qrCodeService;
        private readonly DistributorAccessService _accessService;

        public QRController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager, 
            IQRCodeService qrCodeService,
            DistributorAccessService accessService)
        {
            _context = context;
            _userManager = userManager;
            _qrCodeService = qrCodeService;
            _accessService = accessService;
        }

        public async Task<IActionResult> QRCodes(int? locationId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Distributor)
            {
                return Forbid();
            }

            var userDistributors = await _accessService.GetUserDistributorsAsync(user);
            if (!userDistributors.Any())
            {
                return NotFound();
            }

            var headOfficeDistributor = await _accessService.GetHeadOfficeDistributorAsync(userDistributors);

            Distributor? selectedDistributor;
            List<Distributor> allDistributors;

            if (headOfficeDistributor != null && locationId.HasValue)
            {
                selectedDistributor = await _context.Distributors
                    .FirstOrDefaultAsync(d => d.Id == locationId.Value && 
                                           d.CompanyId == headOfficeDistributor.CompanyId && 
                                           d.IsActive);
                
                if (selectedDistributor == null)
                {
                    return NotFound();
                }

                allDistributors = await _context.Distributors
                    .Where(d => d.CompanyId == headOfficeDistributor.CompanyId && d.IsActive)
                    .ToListAsync();
            }
            else
            {
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

            var distributor = await _context.Distributors
                .Include(d => d.QRCode)
                .FirstOrDefaultAsync(d => d.Id == distributorId);

            if (distributor == null)
            {
                return NotFound();
            }

            if (!await _accessService.HasAccessToDistributorAsync(user, distributorId))
            {
                return Forbid();
            }

            // 1拠点1QRコード制限チェック
            if (distributor.QRCode != null)
            {
                if (distributor.QRCode.IsActive)
                {
                    TempData["Error"] = "この拠点には既に有効なQRコードが存在します。1拠点につき1つのQRコードのみ生成可能です。";
                }
                else
                {
                    TempData["Error"] = "この拠点には既に無効なQRコードが存在します。新しいQRコードを生成するには、既存のQRコードを完全に削除してください。";
                }
                return RedirectToAction("QRCodes", new { locationId = distributorId });
            }

            if (string.IsNullOrWhiteSpace(location))
            {
                TempData["Error"] = "設置場所を入力してください。";
                return RedirectToAction("QRCodes", new { locationId = distributorId });
            }

            var qrCodeGuid = Guid.NewGuid().ToString("N")[..8];
            var qrCodeUrl = $"{Request.Scheme}://{Request.Host}/shop/{qrCodeGuid}";
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

            if (!await _accessService.HasAccessToQRCodeAsync(user, qrCodeId))
            {
                return Forbid();
            }

            var qrCode = await _context.QRCodes
                .Include(q => q.Distributor)
                .FirstOrDefaultAsync(q => q.Id == qrCodeId);

            if (qrCode != null)
            {
                qrCode.IsActive = false;
                qrCode.DeactivatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                TempData["Success"] = "QRコードを無効化しました。";
            }

            return RedirectToAction("QRCodes", new { locationId = qrCode!.DistributorId });
        }

        [HttpPost]
        public async Task<IActionResult> ActivateQRCode(int qrCodeId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Distributor)
            {
                return Forbid();
            }

            if (!await _accessService.HasAccessToQRCodeAsync(user, qrCodeId))
            {
                return Forbid();
            }

            var qrCode = await _context.QRCodes
                .Include(q => q.Distributor)
                .FirstOrDefaultAsync(q => q.Id == qrCodeId);

            if (qrCode == null)
            {
                return NotFound();
            }

            var activeQRCode = await _context.QRCodes
                .FirstOrDefaultAsync(q => q.DistributorId == qrCode.DistributorId && q.IsActive && q.Id != qrCodeId);

            if (activeQRCode != null)
            {
                TempData["Error"] = "既に有効なQRコードが存在します。1拠点につき1つのQRコードのみ有効にできます。";
                return RedirectToAction("QRCodes", new { locationId = qrCode.DistributorId });
            }

            qrCode.IsActive = true;
            qrCode.DeactivatedAt = null;
            await _context.SaveChangesAsync();

            TempData["Success"] = "QRコードを有効化しました。";
            return RedirectToAction("QRCodes", new { locationId = qrCode.DistributorId });
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

            if (qrCode.QRCodeProducts?.Any() == true)
            {
                _context.QRCodeProducts.RemoveRange(qrCode.QRCodeProducts);
            }

            _context.QRCodes.Remove(qrCode);
            await _context.SaveChangesAsync();

            TempData["Success"] = "QRコードを完全に削除しました。";
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

            var qrCodeUrl = $"{Request.Scheme}://{Request.Host}/shop/{qrCode.Code}";
            var imageBytes = _qrCodeService.GenerateQRCodeImage(qrCodeUrl);
            
            return File(imageBytes, "image/png", $"QRCode_{qrCode.Location}_{qrCode.Code}.png");
        }

        public async Task<IActionResult> QRCodeProducts(int qrCodeId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Distributor)
            {
                return Forbid();
            }

            if (!await _accessService.HasAccessToQRCodeAsync(user, qrCodeId))
            {
                return Forbid();
            }

            var qrCode = await _context.QRCodes
                .Include(qr => qr.QRCodeProducts!)
                .ThenInclude(qcp => qcp.Product)
                .ThenInclude(p => p.Manufacturer)
                .Include(qr => qr.Distributor)
                .FirstOrDefaultAsync(qr => qr.Id == qrCodeId);

            if (qrCode == null)
            {
                return NotFound();
            }

            // データベースから商品を取得
            var availableProducts = await _context.Products
                .Include(p => p.Manufacturer)
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();

            // JavaScript用にデータを整形（キャメルケースで）
            var productsForJs = availableProducts.Select(p => new {
                id = p.Id,
                name = p.Name ?? "",
                description = p.Description ?? "",
                category = p.Category ?? "",
                retailPrice = p.RetailPrice,
                wholesalePrice = p.WholesalePrice,
                imageUrl = p.ImageUrl ?? "",
                stockQuantity = p.StockQuantity,
                requiresRefrigeration = p.RequiresRefrigeration,
                requiresFreezing = p.RequiresFreezing,
                manufacturerId = p.ManufacturerId,
                manufacturerName = p.Manufacturer?.CompanyName ?? "",
                profitMargin = p.RetailPrice > 0 ? Math.Round(((p.RetailPrice - p.WholesalePrice) / p.RetailPrice) * 100, 1) : 0
            }).ToList();

            ViewBag.AvailableProducts = availableProducts;
            ViewBag.AvailableProductsJs = productsForJs;
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

            if (!await _accessService.HasAccessToQRCodeAsync(user, qrCodeId))
            {
                return Forbid();
            }

            var qrCode = await _context.QRCodes
                .Include(qr => qr.Distributor)
                .FirstOrDefaultAsync(qr => qr.Id == qrCodeId);

            if (qrCode == null)
            {
                return NotFound();
            }

            // 商品の存在確認
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId && p.IsActive);
            if (product == null)
            {
                TempData["Error"] = $"指定された商品（ID: {productId}）が見つからないか、無効になっています。";
                return RedirectToAction("QRCodeProducts", new { qrCodeId });
            }

            var currentProductCount = await _context.QRCodeProducts
                .CountAsync(qcp => qcp.QRCodeId == qrCodeId && qcp.IsActive);
            
            if (currentProductCount >= qrCode.Distributor.ProductSelectionCount)
            {
                TempData["Error"] = $"商品選定数の上限（{qrCode.Distributor.ProductSelectionCount}商品）に達しています。";
                return RedirectToAction("QRCodeProducts", new { qrCodeId });
            }

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

            var qrCodeProduct = await _context.QRCodeProducts
                .Include(qcp => qcp.QRCode)
                .ThenInclude(qr => qr.Distributor)
                .FirstOrDefaultAsync(qcp => qcp.Id == qrCodeProductId);

            if (qrCodeProduct == null)
            {
                return NotFound();
            }

            if (!await _accessService.HasAccessToQRCodeAsync(user, qrCodeProduct.QRCodeId))
            {
                return Forbid();
            }

            qrCodeProduct.IsActive = false;
            qrCodeProduct.RemovedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = "商品をQRコードから削除しました。";
            return RedirectToAction("QRCodeProducts", new { qrCodeId = qrCodeProduct.QRCodeId });
        }
    }
}