using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using shelf_project.Data;
using shelf_project.Models;
using shelf_project.ViewModels;

namespace shelf_project.Controllers
{
    public class ShopController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public ShopController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<IActionResult> Index(string? qr = null)
        {
            var products = await _context.Products
                .Include(p => p.Manufacturer)
                .Where(p => p.IsActive && p.StockQuantity > 0)
                .ToListAsync();

            ViewBag.QRCode = qr;
            
            if (!string.IsNullOrEmpty(qr))
            {
                var qrCode = await _context.QRCodes
                    .Include(q => q.Distributor)
                    .FirstOrDefaultAsync(q => q.Code == qr && q.IsActive);
                
                ViewBag.Distributor = qrCode?.Distributor;
                
                if (qrCode?.Distributor != null)
                {
                    // QRコード経由の場合、その代理店が取り扱っている商品のみ表示
                    var distributorProducts = await _context.DistributorProducts
                        .Include(dp => dp.Product)
                        .ThenInclude(p => p.Manufacturer)
                        .Where(dp => dp.DistributorId == qrCode.Distributor.Id && dp.IsActive)
                        .Select(dp => dp.Product)
                        .Where(p => p.IsActive && p.StockQuantity > 0)
                        .ToListAsync();
                    
                    products = distributorProducts;
                }
            }

            return View(products);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(ConsumerRegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Role = UserRole.Consumer
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(ConsumerLoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    var user = await _userManager.FindByEmailAsync(model.Email);
                    if (user != null)
                    {
                        user.LastLoginAt = DateTime.Now;
                        await _userManager.UpdateAsync(user);
                    }

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

                    return RedirectToAction("Index");
                }

                ModelState.AddModelError(string.Empty, "ログイン情報が正しくありません。");
            }

            return View(model);
        }

        public async Task<IActionResult> Product(int id, string? qr = null)
        {
            var product = await _context.Products
                .Include(p => p.Manufacturer)
                .Include(p => p.Reviews)
                .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (product == null)
            {
                return NotFound();
            }

            ViewBag.QRCode = qr;
            
            if (!string.IsNullOrEmpty(qr))
            {
                var qrCode = await _context.QRCodes
                    .Include(q => q.Distributor)
                    .FirstOrDefaultAsync(q => q.Code == qr && q.IsActive);
                
                ViewBag.Distributor = qrCode?.Distributor;
            }

            ViewBag.AverageRating = product.Reviews?.Any() == true 
                ? product.Reviews.Where(r => r.IsApproved).Average(r => r.Rating) 
                : 0;

            return View(product);
        }
    }
}