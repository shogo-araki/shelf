using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using shelf_project.Data;
using shelf_project.Models;

namespace shelf_project.Controllers.Admin
{
    [Authorize]
    public class FinancialController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public FinancialController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Subscriptions()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Admin)
            {
                return Forbid();
            }

            // Subscriptionテーブルが存在しない場合は、代理店情報で代替
            var distributors = await _context.Distributors
                .Include(d => d.User)
                .Where(d => d.IsActive)
                .OrderByDescending(d => d.ContractStartDate)
                .ToListAsync();

            return View(distributors);
        }

        public async Task<IActionResult> Sales()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Admin)
            {
                return Forbid();
            }

            var sales = await _context.Sales
                .Include(s => s.Order)
                .ThenInclude(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(s => s.Distributor)
                .ThenInclude(d => d.User)
                .OrderByDescending(s => s.SaleDate)
                .Take(100)
                .ToListAsync();

            ViewBag.TotalRevenue = sales.Sum(s => s.TotalAmount);
            ViewBag.TotalPlatformFee = sales.Sum(s => s.PlatformFee);

            return View(sales);
        }

        public async Task<IActionResult> Settlements()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Admin)
            {
                return Forbid();
            }

            var settlements = await _context.Settlements
                .Include(s => s.Manufacturer)
                .ThenInclude(m => m.User)
                .Include(s => s.Distributor!)
                .ThenInclude(d => d.User)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            ViewBag.PendingSettlements = settlements.Where(s => s.Status == SettlementStatus.Pending)
                                                   .Sum(s => s.Amount);

            ViewBag.ProcessedSettlements = settlements.Where(s => s.Status == SettlementStatus.Completed)
                                                     .Sum(s => s.Amount);

            return View(settlements);
        }

        [HttpPost]
        public async Task<IActionResult> ProcessSettlement(int settlementId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Admin)
            {
                return Forbid();
            }

            var settlement = await _context.Settlements.FindAsync(settlementId);
            if (settlement != null && settlement.Status == SettlementStatus.Pending)
            {
                settlement.Status = SettlementStatus.Completed;
                await _context.SaveChangesAsync();

                TempData["Success"] = "決済を処理しました。";
            }
            else
            {
                TempData["Error"] = "決済の処理に失敗しました。";
            }

            return RedirectToAction(nameof(Settlements));
        }

        public async Task<IActionResult> SampleOrders()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Admin)
            {
                return Forbid();
            }

            var sampleOrders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Distributor)
                .ThenInclude(d => d.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .ThenInclude(p => p.Manufacturer)
                // サンプル注文の判定ロジックをここに実装（例：特定の条件で判定）
                .OrderByDescending(o => o.CreatedAt)
                .Take(50)
                .ToListAsync();

            ViewBag.PendingSampleOrders = sampleOrders.Count(o => o.Status == OrderStatus.Pending);
            ViewBag.ProcessedSampleOrders = sampleOrders.Count(o => o.Status == OrderStatus.Shipped);

            return View(sampleOrders);
        }
    }
}