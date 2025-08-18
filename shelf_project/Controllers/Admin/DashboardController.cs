using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using shelf_project.Data;
using shelf_project.Models;

namespace shelf_project.Controllers.Admin
{
    /// <summary>
    /// 管理者ダッシュボードコントローラー - 統計情報と分析データの表示
    /// </summary>
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Admin)
            {
                return Forbid();
            }

            ViewBag.TotalDistributors = await _context.Distributors.CountAsync(d => d.IsActive);
            ViewBag.TotalManufacturers = await _context.Manufacturers.CountAsync(m => m.IsActive);
            ViewBag.TotalProducts = await _context.Products.CountAsync(p => p.IsActive);
            ViewBag.MonthlyRevenue = await _context.Sales
                .Where(s => s.SaleDate.Month == DateTime.Now.Month && s.SaleDate.Year == DateTime.Now.Year)
                .SumAsync(s => s.PlatformFee);

            var recentOrders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Distributor)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.CreatedAt)
                .Take(10)
                .ToListAsync();

            return View(recentOrders);
        }

        public async Task<IActionResult> Analytics()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Admin)
            {
                return Forbid();
            }

            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            var monthlySales = await _context.Sales
                .Where(s => s.SaleDate.Month == currentMonth && s.SaleDate.Year == currentYear)
                .GroupBy(s => s.SaleDate.Day)
                .Select(g => new
                {
                    Day = g.Key,
                    Revenue = g.Sum(s => s.TotalAmount),
                    PlatformFee = g.Sum(s => s.PlatformFee)
                })
                .OrderBy(x => x.Day)
                .ToListAsync();

            var monthlyStats = await _context.Sales
                .Where(s => s.SaleDate.Month == currentMonth && s.SaleDate.Year == currentYear)
                .GroupBy(s => 1)
                .Select(g => new
                {
                    TotalRevenue = g.Sum(s => s.TotalAmount),
                    TotalPlatformFee = g.Sum(s => s.PlatformFee),
                    TotalDistributorCommission = g.Sum(s => s.DistributorCommission),
                    OrderCount = g.Count()
                })
                .FirstOrDefaultAsync();

            ViewBag.MonthlySales = monthlySales;
            ViewBag.MonthlyStats = monthlyStats ?? new { TotalRevenue = 0m, TotalPlatformFee = 0m, TotalDistributorCommission = 0m, OrderCount = 0 };

            return View();
        }

    }
}