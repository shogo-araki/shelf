using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using shelf_project.Data;
using shelf_project.Models;
using shelf_project.Services;

namespace shelf_project.Controllers.DistributorManagement
{
    /// <summary>
    /// 代理店メインコントローラー - ダッシュボード、売上表示、多拠点管理
    /// </summary>
    [Authorize]
    public class DistributorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly DistributorAccessService _accessService;
        private readonly ISystemSettingService _systemSettingService;

        public DistributorController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, DistributorAccessService accessService, ISystemSettingService systemSettingService)
        {
            _context = context;
            _userManager = userManager;
            _accessService = accessService;
            _systemSettingService = systemSettingService;
        }

        /// <summary>
        /// 代理店メインダッシュボード - 拠点情報、QRコード、契約状態を表示
        /// </summary>
        /// <param name="locationId">表示対象の拠点ID（チェーン店の場合）</param>
        public async Task<IActionResult> Index(int? locationId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Distributor)
            {
                return Forbid();
            }

            var userDistributors = await _accessService.GetUserDistributorsAsync(user);

            if (!userDistributors.Any())
            {
                return RedirectToAction("NewContract", "Settings");
            }

            var headOfficeDistributor = await _accessService.GetHeadOfficeDistributorAsync(userDistributors);

            if (headOfficeDistributor != null && locationId.HasValue)
            {
                var targetDistributor = await _context.Distributors
                    .Include(d => d.Company)
                    .Include(d => d.QRCode)
                    .FirstOrDefaultAsync(d => d.Id == locationId.Value && 
                                           d.CompanyId == headOfficeDistributor.CompanyId && 
                                           d.IsActive);
                
                if (targetDistributor != null)
                {
                    return View(targetDistributor);
                }
            }

            var distributors = await _context.Distributors
                .Include(d => d.Company)
                .Include(d => d.QRCode)
                .Where(d => d.UserId == user.Id && d.IsActive)
                .ToListAsync();

            if (distributors.Count > 1 && locationId.HasValue)
            {
                var selectedDistributor = distributors.FirstOrDefault(d => d.Id == locationId.Value);
                if (selectedDistributor != null)
                {
                    return View(selectedDistributor);
                }
            }
            
            if (distributors.Count > 1)
            {
                var headOffice = distributors.FirstOrDefault(d => d.DistributorType == DistributorType.HeadOffice);
                if (headOffice?.Company?.CompanyType == CompanyType.Chain)
                {
                    return RedirectToAction("Dashboard", "Company");
                }
            }

            var distributor = distributors.First();
            
            // システム設定から月額料金を取得
            decimal monthlyFee;
            if (distributor.DistributorType == DistributorType.Individual)
            {
                monthlyFee = await _systemSettingService.GetSettingValueAsDecimalAsync(
                    SystemSettingCategories.Pricing, SystemSettingKeys.MonthlyFeeIndividual, 5000);
            }
            else if (distributor.DistributorType == DistributorType.Store)
            {
                monthlyFee = await _systemSettingService.GetSettingValueAsDecimalAsync(
                    SystemSettingCategories.Pricing, SystemSettingKeys.MonthlyFeeChainStore, 5000);
            }
            else
            {
                monthlyFee = await _systemSettingService.GetSettingValueAsDecimalAsync(
                    SystemSettingCategories.Pricing, SystemSettingKeys.MonthlyFeeHeadOffice, 5000);
            }
            ViewBag.SystemMonthlyFee = monthlyFee;
            
            return View(distributor);
        }

        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Distributor)
            {
                return Forbid();
            }

            var distributor = await _context.Distributors
                .Include(d => d.Sales!)
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

        public async Task<IActionResult> Sales(int? locationId)
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
    }
}