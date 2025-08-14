using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using shelf_project.Data;
using shelf_project.Models;

namespace shelf_project.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
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

        public async Task<IActionResult> Distributors()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Admin)
            {
                return Forbid();
            }

            var distributors = await _context.Distributors
                .Include(d => d.User)
                .Include(d => d.Subscriptions)
                .Include(d => d.Sales)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            return View(distributors);
        }

        public async Task<IActionResult> Manufacturers()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Admin)
            {
                return Forbid();
            }

            var manufacturers = await _context.Manufacturers
                .Include(m => m.User)
                .Include(m => m.Products)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            return View(manufacturers);
        }

        public async Task<IActionResult> Subscriptions()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Admin)
            {
                return Forbid();
            }

            var subscriptions = await _context.DistributorSubscriptions
                .Include(ds => ds.Distributor)
                .ThenInclude(d => d.User)
                .OrderByDescending(ds => ds.BillingDate)
                .ToListAsync();

            return View(subscriptions);
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
                .ThenInclude(o => o.User)
                .Include(s => s.Distributor)
                .ThenInclude(d => d.User)
                .Include(s => s.Order)
                .ThenInclude(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            ViewBag.TotalSales = sales.Sum(s => s.TotalAmount);
            ViewBag.TotalPlatformFees = sales.Sum(s => s.PlatformFee);
            ViewBag.MonthlySales = sales.Where(s => s.SaleDate.Month == DateTime.Now.Month)
                                       .Sum(s => s.TotalAmount);
            ViewBag.MonthlyPlatformFees = sales.Where(s => s.SaleDate.Month == DateTime.Now.Month)
                                              .Sum(s => s.PlatformFee);

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
                .Include(s => s.Distributor)
                .ThenInclude(d => d.User)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            ViewBag.PendingSettlements = settlements.Where(s => s.Status == SettlementStatus.Pending)
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
            if (settlement != null)
            {
                settlement.Status = SettlementStatus.Processing;
                settlement.ProcessedDate = DateTime.Now;
                await _context.SaveChangesAsync();

                // In a real application, you would integrate with a payment system here
                // For now, we'll just mark it as completed
                settlement.Status = SettlementStatus.Completed;
                await _context.SaveChangesAsync();

                TempData["Success"] = "精算処理を完了しました。";
            }

            return RedirectToAction("Settlements");
        }

        public async Task<IActionResult> Analytics()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Admin)
            {
                return Forbid();
            }

            var now = DateTime.Now;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var startOfYear = new DateTime(now.Year, 1, 1);

            ViewBag.MonthlyStats = new
            {
                Sales = await _context.Sales.Where(s => s.SaleDate >= startOfMonth).SumAsync(s => s.TotalAmount),
                PlatformFees = await _context.Sales.Where(s => s.SaleDate >= startOfMonth).SumAsync(s => s.PlatformFee),
                Orders = await _context.Orders.CountAsync(o => o.CreatedAt >= startOfMonth),
                NewDistributors = await _context.Distributors.CountAsync(d => d.CreatedAt >= startOfMonth),
                NewManufacturers = await _context.Manufacturers.CountAsync(m => m.CreatedAt >= startOfMonth)
            };

            ViewBag.YearlyStats = new
            {
                Sales = await _context.Sales.Where(s => s.SaleDate >= startOfYear).SumAsync(s => s.TotalAmount),
                PlatformFees = await _context.Sales.Where(s => s.SaleDate >= startOfYear).SumAsync(s => s.PlatformFee),
                Orders = await _context.Orders.CountAsync(o => o.CreatedAt >= startOfYear),
                NewDistributors = await _context.Distributors.CountAsync(d => d.CreatedAt >= startOfYear),
                NewManufacturers = await _context.Manufacturers.CountAsync(m => m.CreatedAt >= startOfYear)
            };

            var topProducts = await _context.OrderItems
                .Include(oi => oi.Product)
                .GroupBy(oi => oi.Product)
                .Select(g => new
                {
                    Product = g.Key,
                    TotalQuantity = g.Sum(oi => oi.Quantity),
                    TotalRevenue = g.Sum(oi => oi.TotalPrice)
                })
                .OrderByDescending(x => x.TotalRevenue)
                .Take(10)
                .ToListAsync();

            var topDistributors = await _context.Sales
                .Include(s => s.Distributor)
                .ThenInclude(d => d.User)
                .GroupBy(s => s.Distributor)
                .Select(g => new
                {
                    Distributor = g.Key,
                    TotalSales = g.Sum(s => s.TotalAmount),
                    TotalCommission = g.Sum(s => s.DistributorCommission),
                    OrderCount = g.Count()
                })
                .OrderByDescending(x => x.TotalSales)
                .Take(10)
                .ToListAsync();

            ViewBag.TopProducts = topProducts;
            ViewBag.TopDistributors = topDistributors;

            return View();
        }

        public async Task<IActionResult> QRCodes()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Admin)
            {
                return Forbid();
            }

            var qrCodes = await _context.QRCodes
                .Include(qr => qr.Distributor)
                .ThenInclude(d => d.User)
                .Include(qr => qr.Sales)
                .OrderByDescending(qr => qr.CreatedAt)
                .ToListAsync();

            return View(qrCodes);
        }

        [HttpPost]
        public async Task<IActionResult> DeactivateDistributor(int distributorId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Admin)
            {
                return Forbid();
            }

            var distributor = await _context.Distributors.FindAsync(distributorId);
            if (distributor != null)
            {
                distributor.IsActive = false;
                distributor.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                TempData["Success"] = "代理店を無効化しました。";
            }

            return RedirectToAction("Distributors");
        }

        [HttpPost]
        public async Task<IActionResult> ActivateDistributor(int distributorId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Admin)
            {
                return Forbid();
            }

            var distributor = await _context.Distributors.FindAsync(distributorId);
            if (distributor != null)
            {
                distributor.IsActive = true;
                distributor.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                TempData["Success"] = "代理店を有効化しました。";
            }

            return RedirectToAction("Distributors");
        }

        public async Task<IActionResult> SampleOrders()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Admin)
            {
                return Forbid();
            }

            var sampleOrders = await _context.SampleOrders
                .Include(so => so.Distributor)
                .ThenInclude(d => d.User)
                .Include(so => so.Product)
                .ThenInclude(p => p.Manufacturer)
                .OrderByDescending(so => so.OrderDate)
                .ToListAsync();

            ViewBag.MonthlySampleOrderRevenue = sampleOrders
                .Where(so => so.OrderDate.Month == DateTime.Now.Month && so.Status == SampleOrderStatus.Paid)
                .Sum(so => so.ServiceFee);

            return View(sampleOrders);
        }
    }
}