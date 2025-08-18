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
    public class SettingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly DistributorAccessService _accessService;
        private readonly ISystemSettingService _systemSettingService;

        public SettingsController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            DistributorAccessService accessService,
            ISystemSettingService systemSettingService)
        {
            _context = context;
            _userManager = userManager;
            _accessService = accessService;
            _systemSettingService = systemSettingService;
        }

        public async Task<IActionResult> NewContract()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Distributor)
            {
                return Forbid();
            }

            var existingDistributor = await _context.Distributors
                .FirstOrDefaultAsync(d => d.UserId == user.Id && d.IsActive);

            if (existingDistributor != null)
            {
                return RedirectToAction("Index", "Distributor", new { area = "" });
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

            var existingDistributor = await _context.Distributors
                .FirstOrDefaultAsync(d => d.UserId == user.Id && d.IsActive);

            if (existingDistributor != null)
            {
                TempData["Error"] = "既に契約が存在します。";
                return RedirectToAction("Index", "Distributor", new { area = "" });
            }

            if (string.IsNullOrWhiteSpace(companyName) || string.IsNullOrWhiteSpace(locationName))
            {
                TempData["Error"] = "会社名と拠点名は必須項目です。";
                return View();
            }

            Models.Company? parentCompany = null;
            DistributorType distributorType = DistributorType.Individual;

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
                var companyCode = _accessService.GenerateUniqueCompanyCode();
                parentCompany = new Models.Company
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

            // システム設定から基本値を取得
            var defaultShelfCount = await _systemSettingService.GetSettingValueAsIntAsync(
                SystemSettingCategories.Shelf, SystemSettingKeys.DefaultShelfCount, 1);
            var defaultProductSelectionCount = await _systemSettingService.GetSettingValueAsIntAsync(
                SystemSettingCategories.Shelf, SystemSettingKeys.DefaultProductSelectionCount, 10);
            
            // 月額料金を取得
            decimal monthlyFee;
            if (distributorType == DistributorType.Individual)
            {
                monthlyFee = await _systemSettingService.GetSettingValueAsDecimalAsync(
                    SystemSettingCategories.Pricing, SystemSettingKeys.MonthlyFeeIndividual, 5000);
            }
            else if (distributorType == DistributorType.Store)
            {
                monthlyFee = await _systemSettingService.GetSettingValueAsDecimalAsync(
                    SystemSettingCategories.Pricing, SystemSettingKeys.MonthlyFeeChainStore, 4000);
            }
            else
            {
                monthlyFee = await _systemSettingService.GetSettingValueAsDecimalAsync(
                    SystemSettingCategories.Pricing, SystemSettingKeys.MonthlyFeeHeadOffice, 6000);
            }

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
                ShelfCount = defaultShelfCount,
                ProductSelectionCount = defaultProductSelectionCount,
                MonthlyFee = monthlyFee,
                IsActive = true
            };

            _context.Distributors.Add(newDistributor);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"契約が完了しました。{(distributorType == DistributorType.Store ? "チェーン店" : "独立代理店")}として登録されました。";
            return RedirectToAction("Index", "Distributor");
        }

        public async Task<IActionResult> Settings()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Distributor)
            {
                return Forbid();
            }

            var distributor = await _context.Distributors
                .Include(d => d.Company!)
                .ThenInclude(c => c.Distributors!)
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

            if (distributor.Company != null && distributor.Company.CompanyType == CompanyType.Chain)
            {
                TempData["Info"] = "既にチェーン店として登録されています。";
                return RedirectToAction("Settings");
            }

            if (distributor.Company == null)
            {
                var company = new Models.Company
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

            var sales = distributor.Sales?.ToList() ?? new List<Sale>();

            ViewBag.Sales = sales;
            ViewBag.TotalRevenue = sales.Sum(s => s.TotalAmount);
            ViewBag.TotalCommission = sales.Sum(s => s.DistributorCommission);

            return View(distributor);
        }
    }
}