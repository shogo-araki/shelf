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

        public IActionResult Index()
        {
            // 直接アクセスは禁止し、QRコード経由のみアクセス可能
            return NotFound("このページは代理店QRコード経由でのみアクセス可能です。");
        }

        [Route("shop/{qrCode}")]
        public async Task<IActionResult> DistributorShop(string qrCode)
        {
            var qrCodeEntity = await _context.QRCodes
                .Include(q => q.Distributor)
                .FirstOrDefaultAsync(q => q.Code == qrCode && q.IsActive);

            if (qrCodeEntity?.Distributor == null)
            {
                return NotFound("QRコードが見つからないか、無効です。");
            }

            // QRコードに登録されている商品のみを取得
            var qrCodeProducts = await _context.QRCodeProducts
                .Include(qcp => qcp.Product)
                .ThenInclude(p => p.Manufacturer)
                .Where(qcp => qcp.QRCodeId == qrCodeEntity.Id && qcp.IsActive)
                .Select(qcp => qcp.Product)
                .Where(p => p.IsActive && p.StockQuantity > 0)
                .ToListAsync();

            ViewBag.Distributor = qrCodeEntity.Distributor;
            ViewBag.QRCode = qrCode;
            ViewBag.IsDistributorSite = true;

            return View("DistributorShop", qrCodeProducts);
        }

        [Route("shop/{qrCode}/product/{id}")]
        public async Task<IActionResult> DistributorProduct(string qrCode, int id)
        {
            var qrCodeEntity = await _context.QRCodes
                .Include(q => q.Distributor)
                .FirstOrDefaultAsync(q => q.Code == qrCode && q.IsActive);

            if (qrCodeEntity?.Distributor == null)
            {
                return NotFound("QRコードが見つからないか、無効です。");
            }

            // 代理店が取り扱っている商品かチェック
            var distributorProduct = await _context.DistributorProducts
                .FirstOrDefaultAsync(dp => dp.DistributorId == qrCodeEntity.Distributor.Id && 
                                         dp.ProductId == id && dp.IsActive);

            if (distributorProduct == null)
            {
                return NotFound("この商品は取り扱っていません。");
            }

            var product = await _context.Products
                .Include(p => p.Manufacturer)
                .Include(p => p.Reviews)
                .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (product == null)
            {
                return NotFound();
            }

            ViewBag.QRCode = qrCode;
            ViewBag.Distributor = qrCodeEntity.Distributor;
            ViewBag.IsDistributorSite = true;
            ViewBag.AverageRating = product.Reviews?.Any() == true 
                ? product.Reviews.Where(r => r.IsApproved).Average(r => r.Rating) 
                : 0;

            return View("DistributorProduct", product);
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

        public IActionResult Product(int id, string? qr = null)
        {
            // QRコードパラメータがない場合は直接アクセスを禁止
            if (string.IsNullOrEmpty(qr))
            {
                return NotFound("このページは代理店QRコード経由でのみアクセス可能です。");
            }
            
            // QRコード経由の場合は、DistributorProductアクションにリダイレクト
            return RedirectToAction("DistributorProduct", new { qrCode = qr, id = id });
        }
    }
}