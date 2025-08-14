using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using shelf_project.Data;
using shelf_project.Models;
using shelf_project.ViewModels;

namespace shelf_project.Controllers
{
    [Authorize]
    public class ManufacturerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ManufacturerController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Manufacturer)
            {
                return Forbid();
            }

            var manufacturer = await _context.Manufacturers
                .Include(m => m.Products)
                .FirstOrDefaultAsync(m => m.UserId == user.Id);

            if (manufacturer == null)
            {
                manufacturer = new Manufacturer
                {
                    UserId = user.Id,
                    CompanyName = user.CompanyName ?? "未設定"
                };
                _context.Manufacturers.Add(manufacturer);
                await _context.SaveChangesAsync();
            }

            return View(manufacturer);
        }

        public async Task<IActionResult> Products()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Manufacturer)
            {
                return Forbid();
            }

            var manufacturer = await _context.Manufacturers
                .FirstOrDefaultAsync(m => m.UserId == user.Id);

            if (manufacturer == null)
            {
                return NotFound();
            }

            var products = await _context.Products
                .Where(p => p.ManufacturerId == manufacturer.Id)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(products);
        }

        [HttpGet]
        public async Task<IActionResult> CreateProduct()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Manufacturer)
            {
                return Forbid();
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(ProductViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Manufacturer)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                var manufacturer = await _context.Manufacturers
                    .FirstOrDefaultAsync(m => m.UserId == user.Id);

                if (manufacturer == null)
                {
                    return NotFound();
                }

                var product = new Product
                {
                    ManufacturerId = manufacturer.Id,
                    Name = model.Name,
                    Description = model.Description,
                    WholesalePrice = model.WholesalePrice,
                    RetailPrice = model.RetailPrice,
                    Category = model.Category,
                    ImageUrl = model.ImageUrl,
                    RequiresRefrigeration = model.RequiresRefrigeration,
                    RequiresFreezing = model.RequiresFreezing,
                    ShippingFee = model.ShippingFee,
                    FreeShippingThreshold = model.FreeShippingThreshold,
                    StockQuantity = model.StockQuantity,
                    MinimumOrderQuantity = model.MinimumOrderQuantity
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                TempData["Success"] = "商品を登録しました。";
                return RedirectToAction("Products");
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditProduct(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Manufacturer)
            {
                return Forbid();
            }

            var manufacturer = await _context.Manufacturers
                .FirstOrDefaultAsync(m => m.UserId == user.Id);

            if (manufacturer == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.ManufacturerId == manufacturer.Id);

            if (product == null)
            {
                return NotFound();
            }

            var model = new ProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                WholesalePrice = product.WholesalePrice,
                RetailPrice = product.RetailPrice,
                Category = product.Category,
                ImageUrl = product.ImageUrl,
                RequiresRefrigeration = product.RequiresRefrigeration,
                RequiresFreezing = product.RequiresFreezing,
                ShippingFee = product.ShippingFee,
                FreeShippingThreshold = product.FreeShippingThreshold,
                StockQuantity = product.StockQuantity,
                MinimumOrderQuantity = product.MinimumOrderQuantity,
                IsActive = product.IsActive
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(ProductViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Manufacturer)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                var manufacturer = await _context.Manufacturers
                    .FirstOrDefaultAsync(m => m.UserId == user.Id);

                if (manufacturer == null)
                {
                    return NotFound();
                }

                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == model.Id && p.ManufacturerId == manufacturer.Id);

                if (product == null)
                {
                    return NotFound();
                }

                product.Name = model.Name;
                product.Description = model.Description;
                product.WholesalePrice = model.WholesalePrice;
                product.RetailPrice = model.RetailPrice;
                product.Category = model.Category;
                product.ImageUrl = model.ImageUrl;
                product.RequiresRefrigeration = model.RequiresRefrigeration;
                product.RequiresFreezing = model.RequiresFreezing;
                product.ShippingFee = model.ShippingFee;
                product.FreeShippingThreshold = model.FreeShippingThreshold;
                product.StockQuantity = model.StockQuantity;
                product.MinimumOrderQuantity = model.MinimumOrderQuantity;
                product.IsActive = model.IsActive;
                product.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["Success"] = "商品を更新しました。";
                return RedirectToAction("Products");
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStock(int productId, int stockQuantity)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Manufacturer)
            {
                return Forbid();
            }

            var manufacturer = await _context.Manufacturers
                .FirstOrDefaultAsync(m => m.UserId == user.Id);

            if (manufacturer == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == productId && p.ManufacturerId == manufacturer.Id);

            if (product != null)
            {
                product.StockQuantity = stockQuantity;
                product.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }

            return Json(new { success = false });
        }

        public async Task<IActionResult> Orders()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Manufacturer)
            {
                return Forbid();
            }

            var manufacturer = await _context.Manufacturers
                .FirstOrDefaultAsync(m => m.UserId == user.Id);

            if (manufacturer == null)
            {
                return NotFound();
            }

            var orders = await _context.OrderItems
                .Include(oi => oi.Order)
                .ThenInclude(o => o.User)
                .Include(oi => oi.Product)
                .Where(oi => oi.Product.ManufacturerId == manufacturer.Id)
                .OrderByDescending(oi => oi.Order.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, OrderStatus status, string? trackingNumber = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Manufacturer)
            {
                return Forbid();
            }

            var manufacturer = await _context.Manufacturers
                .FirstOrDefaultAsync(m => m.UserId == user.Id);

            if (manufacturer == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId && 
                    o.OrderItems.Any(oi => oi.Product.ManufacturerId == manufacturer.Id));

            if (order != null)
            {
                order.Status = status;
                if (!string.IsNullOrEmpty(trackingNumber))
                {
                    order.TrackingNumber = trackingNumber;
                }

                if (status == OrderStatus.Shipped)
                {
                    order.ShippedAt = DateTime.Now;
                }
                else if (status == OrderStatus.Delivered)
                {
                    order.DeliveredAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }

            return Json(new { success = false });
        }

        public async Task<IActionResult> Analytics()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Manufacturer)
            {
                return Forbid();
            }

            var manufacturer = await _context.Manufacturers
                .FirstOrDefaultAsync(m => m.UserId == user.Id);

            if (manufacturer == null)
            {
                return NotFound();
            }

            var sales = await _context.OrderItems
                .Include(oi => oi.Product)
                .Include(oi => oi.Order)
                .Where(oi => oi.Product.ManufacturerId == manufacturer.Id && 
                             oi.Order.Status == OrderStatus.Delivered)
                .ToListAsync();

            ViewBag.TotalSales = sales.Sum(oi => oi.TotalPrice);
            ViewBag.TotalQuantity = sales.Sum(oi => oi.Quantity);
            ViewBag.MonthlySales = sales.Where(oi => oi.Order.CreatedAt.Month == DateTime.Now.Month)
                                        .Sum(oi => oi.TotalPrice);
            ViewBag.MonthlyQuantity = sales.Where(oi => oi.Order.CreatedAt.Month == DateTime.Now.Month)
                                           .Sum(oi => oi.Quantity);

            return View(sales);
        }
    }
}