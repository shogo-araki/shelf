using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using shelf_project.Data;
using shelf_project.Models;

namespace shelf_project.Controllers.Admin
{
    [Authorize]
    public class UserManagementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserManagementController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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
                .Include(d => d.Company)
                .OrderBy(d => d.CompanyName)
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
                .OrderBy(m => m.CompanyName)
                .ToListAsync();

            return View(manufacturers);
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
                await _context.SaveChangesAsync();
                TempData["Success"] = "代理店を無効化しました。";
            }

            return RedirectToAction(nameof(Distributors));
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
                await _context.SaveChangesAsync();
                TempData["Success"] = "代理店を有効化しました。";
            }

            return RedirectToAction(nameof(Distributors));
        }
    }
}