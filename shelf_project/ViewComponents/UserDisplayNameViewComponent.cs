using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using shelf_project.Data;
using shelf_project.Models;

namespace shelf_project.ViewComponents
{
    public class UserDisplayNameViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserDisplayNameViewComponent(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!User.Identity?.IsAuthenticated == true)
                return View("Default", "");

            var user = await _userManager.GetUserAsync(UserClaimsPrincipal);
            if (user == null)
                return View("Default", "");

            string displayName = "";

            switch (user.Role)
            {
                case UserRole.Distributor:
                    var distributor = await _context.Distributors
                        .Include(d => d.Company)
                        .FirstOrDefaultAsync(d => d.UserId == user.Id && d.IsActive);
                    
                    if (distributor != null)
                    {
                        // 本社に紐づく代理店の場合は店舗名（LocationName）を表示
                        if (distributor.Company?.CompanyType == CompanyType.Chain && 
                            distributor.DistributorType == DistributorType.Store)
                        {
                            displayName = distributor.LocationName ?? distributor.CompanyName;
                        }
                        // 本社または独立代理店の場合は会社名を表示
                        else
                        {
                            displayName = distributor.CompanyName;
                        }
                    }
                    break;

                case UserRole.Manufacturer:
                    var manufacturer = await _context.Manufacturers
                        .FirstOrDefaultAsync(m => m.UserId == user.Id);
                    
                    if (manufacturer != null)
                    {
                        displayName = manufacturer.CompanyName;
                    }
                    break;

                case UserRole.Admin:
                    displayName = "管理者";
                    break;

                default:
                    displayName = user.Email ?? "";
                    break;
            }

            return View("Default", displayName);
        }
    }
}