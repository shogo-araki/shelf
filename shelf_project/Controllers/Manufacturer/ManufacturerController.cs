using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using shelf_project.Models;
using shelf_project.Services;

namespace shelf_project.Controllers.Manufacturer
{
    /// <summary>
    /// メーカーメインコントローラー - ダッシュボード、統計情報、分析データの表示
    /// </summary>
    [Authorize]
    public class ManufacturerController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ManufacturerAccessService _accessService;

        public ManufacturerController(UserManager<ApplicationUser> userManager, ManufacturerAccessService accessService)
        {
            _userManager = userManager;
            _accessService = accessService;
        }

        /// <summary>
        /// メーカーダッシュボード - 商品数、注文数、売上統計を表示
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (!_accessService.HasManufacturerRole(user))
            {
                return Forbid();
            }

            var manufacturer = await _accessService.GetManufacturerByUserAsync(user!);
            if (manufacturer == null)
            {
                return NotFound();
            }

            var (totalProducts, totalOrders, monthlyRevenue) = await _accessService.GetManufacturerStatsAsync(manufacturer.Id);
            
            ViewBag.TotalProducts = totalProducts;
            ViewBag.TotalOrders = totalOrders;
            ViewBag.MonthlyRevenue = monthlyRevenue;

            return View(manufacturer);
        }

        /// <summary>
        /// メーカー分析画面 - 売上分析、商品パフォーマンス等を表示
        /// </summary>
        public async Task<IActionResult> Analytics()
        {
            var user = await _userManager.GetUserAsync(User);
            if (!_accessService.HasManufacturerRole(user))
            {
                return Forbid();
            }

            var manufacturer = await _accessService.GetManufacturerByUserAsync(user!);
            if (manufacturer == null)
            {
                return NotFound();
            }

            // 分析データは将来実装予定
            ViewBag.MonthlySales = new List<object>();

            return View(manufacturer);
        }
    }
}