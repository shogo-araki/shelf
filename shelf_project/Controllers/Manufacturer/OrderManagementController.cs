using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using shelf_project.Models;
using shelf_project.Services;

namespace shelf_project.Controllers.Manufacturer
{
    /// <summary>
    /// 注文管理コントローラー - メーカーの注文一覧、ステータス更新、配送管理
    /// </summary>
    [Authorize]
    public class OrderManagementController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ManufacturerAccessService _accessService;

        public OrderManagementController(UserManager<ApplicationUser> userManager, ManufacturerAccessService accessService)
        {
            _userManager = userManager;
            _accessService = accessService;
        }

        /// <summary>
        /// 注文一覧表示 - メーカーの商品を含む注文のみ表示
        /// </summary>
        public async Task<IActionResult> Orders()
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

            var orders = await _accessService.GetManufacturerOrdersAsync(manufacturer.Id);

            return View(orders);
        }

        /// <summary>
        /// 注文ステータス更新 - 注文確認、出荷、配送中等のステータス変更
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, OrderStatus status, string? trackingNumber = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (!_accessService.HasManufacturerRole(user))
            {
                return Forbid();
            }

            var success = await _accessService.UpdateOrderStatusAsync(user!, orderId, status, trackingNumber);
            if (success)
            {
                TempData["Success"] = "注文ステータスを更新しました。";
            }
            else
            {
                TempData["Error"] = "注文ステータスの更新に失敗しました。";
            }

            return RedirectToAction(nameof(Orders));
        }
    }
}