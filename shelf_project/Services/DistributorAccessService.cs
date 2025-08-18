using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using shelf_project.Data;
using shelf_project.Models;

namespace shelf_project.Services
{
    public class DistributorAccessService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DistributorAccessService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<List<Distributor>> GetUserDistributorsAsync(ApplicationUser user)
        {
            return await _context.Distributors
                .Include(d => d.Company)
                .Where(d => d.UserId == user.Id && d.IsActive)
                .ToListAsync();
        }

        public Task<Distributor?> GetHeadOfficeDistributorAsync(List<Distributor> userDistributors)
        {
            return Task.FromResult(userDistributors.FirstOrDefault(d => 
                d.DistributorType == DistributorType.HeadOffice && 
                d.Company?.CompanyType == CompanyType.Chain));
        }

        public async Task<Distributor?> GetTargetDistributorAsync(ApplicationUser user, int? locationId)
        {
            var userDistributors = await GetUserDistributorsAsync(user);
            
            if (!userDistributors.Any())
                return null;

            var headOfficeDistributor = await GetHeadOfficeDistributorAsync(userDistributors);

            if (headOfficeDistributor != null && locationId.HasValue)
            {
                // Head office user accessing specific location
                return await _context.Distributors
                    .Include(d => d.Company)
                    .FirstOrDefaultAsync(d => d.Id == locationId.Value && 
                                           d.CompanyId == headOfficeDistributor.CompanyId && 
                                           d.IsActive);
            }

            // Normal processing for user's own distributors
            if (locationId.HasValue)
            {
                return userDistributors.FirstOrDefault(d => d.Id == locationId.Value);
            }

            return userDistributors.First();
        }

        public async Task<bool> HasAccessToDistributorAsync(ApplicationUser user, int distributorId)
        {
            var userDistributors = await GetUserDistributorsAsync(user);
            var headOfficeDistributor = await GetHeadOfficeDistributorAsync(userDistributors);

            var distributor = await _context.Distributors
                .FirstOrDefaultAsync(d => d.Id == distributorId);

            if (distributor == null)
                return false;

            return distributor.UserId == user.Id || 
                   (headOfficeDistributor != null && distributor.CompanyId == headOfficeDistributor.CompanyId);
        }

        public async Task<bool> HasAccessToQRCodeAsync(ApplicationUser user, int qrCodeId)
        {
            var userDistributors = await GetUserDistributorsAsync(user);
            var headOfficeDistributor = await GetHeadOfficeDistributorAsync(userDistributors);

            var qrCode = await _context.QRCodes
                .Include(q => q.Distributor)
                .FirstOrDefaultAsync(q => q.Id == qrCodeId);

            if (qrCode == null)
                return false;

            return qrCode.Distributor.UserId == user.Id || 
                   (headOfficeDistributor != null && qrCode.Distributor.CompanyId == headOfficeDistributor.CompanyId);
        }

        public string GenerateUniqueCompanyCode()
        {
            string code;
            do
            {
                code = new Random().Next(10000000, 99999999).ToString();
            }
            while (_context.Companies.Any(c => c.HeadOfficeCode == code));

            return code;
        }
    }
}