using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using shelf_project.Models;
using shelf_project.ViewModels;

namespace shelf_project.Controllers
{
    public class DistributorAuthController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public DistributorAuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

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
                else if (user?.Role == UserRole.Consumer)
                {
                    return RedirectToAction("Index", "Home");
                }
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(DistributorRegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    Role = UserRole.Distributor,
                    CompanyName = model.CompanyName
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Distributor");
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
        public async Task<IActionResult> Login(DistributorLoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user?.Role != UserRole.Distributor)
                {
                    ModelState.AddModelError(string.Empty, "代理店アカウントが見つかりません。");
                    return View(model);
                }

                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    user.LastLoginAt = DateTime.Now;
                    await _userManager.UpdateAsync(user);

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

                    return RedirectToAction("Index", "Distributor");
                }

                ModelState.AddModelError(string.Empty, "ログイン情報が正しくありません。");
            }

            return View(model);
        }
    }
}