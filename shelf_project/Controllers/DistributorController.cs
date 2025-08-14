using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using shelf_project.Data;
using shelf_project.Models;

namespace shelf_project.Controllers
{
    [Authorize]
    public class DistributorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DistributorController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Distributor)
            {
                return Forbid();
            }

            var distributor = await _context.Distributors
                .Include(d => d.Subscriptions)
                .Include(d => d.DistributorProducts)
                .ThenInclude(dp => dp.Product)
                .FirstOrDefaultAsync(d => d.UserId == user.Id);

            if (distributor == null)
            {
                // Create new distributor profile if not exists
                distributor = new Distributor
                {
                    UserId = user.Id,
                    CompanyName = user.CompanyName ?? "未設定",
                    ContractStartDate = DateTime.Now
                };
                _context.Distributors.Add(distributor);
                await _context.SaveChangesAsync();
            }

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

        public async Task<IActionResult> Products()
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

            var availableProducts = await _context.Products
                .Include(p => p.Manufacturer)
                .Where(p => p.IsActive && !assignedProducts.Select(ap => ap.ProductId).Contains(p.Id))
                .ToListAsync();

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

        public async Task<IActionResult> Sales()
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

            var sales = await _context.Sales
                .Include(s => s.Order)
                .ThenInclude(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(s => s.DistributorId == distributor.Id)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            return View(sales);
        }
    }
}