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
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly DistributorAccessService _accessService;

        public ProductController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            DistributorAccessService accessService)
        {
            _context = context;
            _userManager = userManager;
            _accessService = accessService;
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
                .Where(p => p.IsActive);

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

            var assignedProducts = await _context.DistributorProducts
                .Include(dp => dp.Product)
                .ThenInclude(p => p.Manufacturer)
                .Where(dp => dp.DistributorId == distributor.Id && dp.IsActive)
                .ToListAsync();

            var availableProducts = manufacturer.Products?
                .Where(p => p.IsActive)
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
    }
}