using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using shelf_project.Data;
using shelf_project.Models;

namespace shelf_project.Controllers.Admin
{
    [Authorize]
    public class QRManagementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public QRManagementController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> QRCodes()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Admin)
            {
                return Forbid();
            }

            var qrCodes = await _context.QRCodes
                .Include(q => q.Distributor)
                .ThenInclude(d => d.User)
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();

            ViewBag.ActiveQRCodes = qrCodes.Count(q => q.IsActive);
            ViewBag.InactiveQRCodes = qrCodes.Count(q => !q.IsActive);

            return View(qrCodes);
        }
    }
}