using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using shelf_project.Data;
using shelf_project.Models;
using shelf_project.Services;
using shelf_project.ViewModels;

namespace shelf_project.Controllers.Manufacturer
{
    /// <summary>
    /// 商品管理コントローラー - 商品の作成、編集、在庫管理
    /// </summary>
    [Authorize]
    public class ProductManagementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ManufacturerAccessService _accessService;

        public ProductManagementController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ManufacturerAccessService accessService)
        {
            _context = context;
            _userManager = userManager;
            _accessService = accessService;
        }

        /// <summary>
        /// 商品一覧表示 - メーカーのアクティブ商品を表示
        /// </summary>
        public async Task<IActionResult> Products()
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

            var products = await _accessService.GetActiveProductsAsync(manufacturer.Id);

            return View(products);
        }

        /// <summary>
        /// 新商品作成フォーム表示
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CreateProduct()
        {
            var user = await _userManager.GetUserAsync(User);
            if (!_accessService.HasManufacturerRole(user))
            {
                return Forbid();
            }

            return View();
        }

        /// <summary>
        /// 新商品作成処理
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(ProductViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (!_accessService.HasManufacturerRole(user))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                var manufacturer = await _accessService.GetManufacturerByUserAsync(user!);
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

                TempData["Success"] = "商品を正常に登録しました。";
                return RedirectToAction(nameof(Products));
            }

            return View(model);
        }

        /// <summary>
        /// 商品編集フォーム表示
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> EditProduct(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (!_accessService.HasManufacturerRole(user) || !await _accessService.HasAccessToProductAsync(user!, id))
            {
                return Forbid();
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

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
                MinimumOrderQuantity = product.MinimumOrderQuantity
            };

            return View(model);
        }

        /// <summary>
        /// 商品編集処理
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(ProductViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (!_accessService.HasManufacturerRole(user) || (model.Id > 0 && !await _accessService.HasAccessToProductAsync(user!, model.Id)))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == model.Id && p.IsActive);

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

                await _context.SaveChangesAsync();

                TempData["Success"] = "商品情報を更新しました。";
                return RedirectToAction(nameof(Products));
            }

            return View(model);
        }

        /// <summary>
        /// 商品詳細表示 - QRコード商品選定で使用
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ProductDetail(int id, int? qrCodeId = null)
        {
            var product = await _context.Products
                .Include(p => p.Manufacturer)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (product == null)
            {
                return NotFound();
            }

            ViewBag.QRCodeId = qrCodeId;
            return View(product);
        }

        /// <summary>
        /// 在庫数更新 - AJAXリクエストで在庫を更新
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateStock(int productId, int stockQuantity)
        {
            var user = await _userManager.GetUserAsync(User);
            if (!_accessService.HasManufacturerRole(user))
            {
                return Forbid();
            }

            var success = await _accessService.UpdateProductStockAsync(user!, productId, stockQuantity);
            if (success)
            {
                TempData["Success"] = "在庫数を更新しました。";
            }
            else
            {
                TempData["Error"] = "在庫数の更新に失敗しました。";
            }

            return RedirectToAction(nameof(Products));
        }
    }
}