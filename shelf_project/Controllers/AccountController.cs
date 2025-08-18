using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using shelf_project.Models;
using shelf_project.ViewModels;

namespace shelf_project.Controllers
{
    /// <summary>
    /// アカウント管理コントローラー - ユーザー登録、ログイン、管理者作成を処理
    /// </summary>
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        /// <summary>
        /// コンストラクタ - ASP.NET Core Identityのサービスを注入
        /// </summary>
        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        /// <summary>
        /// ユーザー登録画面表示 - 既ログイン時は適切なダッシュボードにリダイレクト
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user?.Role == UserRole.Distributor)
                {
                    return RedirectToAction("Index", "Distributor");
                }
                else if (user?.Role == UserRole.Manufacturer)
                {
                    return RedirectToAction("Index", "Manufacturer");
                }
                else if (user?.Role == UserRole.Admin)
                {
                    return RedirectToAction("Index", "Dashboard");
                }
                else if (user?.Role == UserRole.Consumer)
                {
                    return RedirectToAction("Index", "Home");
                }
            }
            return View();
        }

        /// <summary>
        /// ユーザー登録処理 - 登録後はロール別にリダイレクト
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Role = model.Role,
                    CompanyName = model.CompanyName
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    
                    // Redirect based on user role
                    return model.Role switch
                    {
                        UserRole.Distributor => RedirectToAction("Index", "Distributor"),
                        UserRole.Manufacturer => RedirectToAction("Index", "Manufacturer"),
                        UserRole.Admin => RedirectToAction("Index", "Dashboard", new { area = "" }),
                        _ => RedirectToAction("Index", "Home")
                    };
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user?.Role == UserRole.Distributor)
                {
                    return RedirectToAction("Index", "Distributor");
                }
                else if (user?.Role == UserRole.Manufacturer)
                {
                    return RedirectToAction("Index", "Manufacturer");
                }
                else if (user?.Role == UserRole.Admin)
                {
                    return RedirectToAction("Index", "Dashboard");
                }
                else if (user?.Role == UserRole.Consumer)
                {
                    return RedirectToAction("Index", "Home");
                }
            }
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
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

                    // Redirect based on user role
                    return user?.Role switch
                    {
                        UserRole.Distributor => RedirectToAction("Index", "Distributor"),
                        UserRole.Manufacturer => RedirectToAction("Index", "Manufacturer"),
                        UserRole.Admin => RedirectToAction("Index", "Dashboard", new { area = "" }),
                        _ => RedirectToAction("Index", "Home")
                    };
                }

                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> CreateAdmin()
        {
            var adminExists = await _userManager.Users.AnyAsync(u => u.Role == UserRole.Admin);
            
            if (adminExists)
            {
                // 管理者が存在する場合は認証チェック
                if (!User.Identity?.IsAuthenticated == true)
                {
                    return RedirectToAction("Login");
                }
                
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser?.Role != UserRole.Admin)
                {
                    return Forbid();
                }
            }
            
            ViewBag.IsFirstAdmin = !adminExists;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAdmin(RegisterViewModel model)
        {
            var adminExists = await _userManager.Users.AnyAsync(u => u.Role == UserRole.Admin);
            
            if (adminExists)
            {
                // 管理者が存在する場合は認証チェック
                if (!User.Identity?.IsAuthenticated == true)
                {
                    return RedirectToAction("Login");
                }
                
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser?.Role != UserRole.Admin)
                {
                    return Forbid();
                }
            }

            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Role = UserRole.Admin,
                    CompanyName = "Shelf Up 運営"
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    var otherAdminExists = await _userManager.Users.AnyAsync(u => u.Role == UserRole.Admin && u.Id != user.Id);
                    if (otherAdminExists)
                    {
                        TempData["Success"] = "管理者アカウントが正常に作成されました。";
                        return RedirectToAction("Index", "Dashboard");
                    }
                    else
                    {
                        TempData["Success"] = "初期管理者アカウントが正常に作成されました。ログインしてください。";
                        return RedirectToAction("Login");
                    }
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }
    }
}