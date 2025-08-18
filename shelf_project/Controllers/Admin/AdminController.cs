using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using shelf_project.Data;
using shelf_project.Models;
using shelf_project.ViewModels;

namespace shelf_project.Controllers.Admin
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

        public IActionResult Index()
        {
            // メインダッシュボードは Dashboard/Index にリダイレクト
            return RedirectToAction("Index", "Dashboard");
        }

        [AllowAnonymous]
        public async Task<IActionResult> RegisterAdmin()
        {
            // 管理者が存在しない場合のみ許可
            var adminCount = await _context.Users.CountAsync(u => u.Role == UserRole.Admin);
            if (adminCount > 0 && User.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction("Login", "Account");
            }
            
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user?.Role != UserRole.Admin)
                {
                    return Forbid();
                }
            }

            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterAdmin(RegisterViewModel model)
        {
            // 管理者が存在しない場合のみ許可
            var adminCount = await _context.Users.CountAsync(u => u.Role == UserRole.Admin);
            if (adminCount > 0 && User.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction("Login", "Account");
            }
            
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user?.Role != UserRole.Admin)
                {
                    return Forbid();
                }
            }

            if (ModelState.IsValid)
            {
                var newUser = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Role = UserRole.Admin
                };

                var result = await _userManager.CreateAsync(newUser, model.Password);

                if (result.Succeeded)
                {
                    TempData["Success"] = "管理者アカウントが正常に作成されました。";
                    return RedirectToAction("Index", "Dashboard");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            return View(model);
        }
    }
}