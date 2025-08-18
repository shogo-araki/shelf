using Microsoft.EntityFrameworkCore;
using shelf_project.Data;
using shelf_project.Models;

namespace shelf_project.Services
{
    public class SystemSettingService : ISystemSettingService
    {
        private readonly ApplicationDbContext _context;

        public SystemSettingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string?> GetSettingValueAsync(string category, string key)
        {
            var setting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.Category == category && s.Key == key);
            return setting?.Value;
        }

        public async Task<int> GetSettingValueAsIntAsync(string category, string key, int defaultValue = 0)
        {
            var value = await GetSettingValueAsync(category, key);
            return int.TryParse(value, out var intValue) ? intValue : defaultValue;
        }

        public async Task<decimal> GetSettingValueAsDecimalAsync(string category, string key, decimal defaultValue = 0)
        {
            var value = await GetSettingValueAsync(category, key);
            return decimal.TryParse(value, out var decimalValue) ? decimalValue : defaultValue;
        }

        public async Task<bool> GetSettingValueAsBoolAsync(string category, string key, bool defaultValue = false)
        {
            var value = await GetSettingValueAsync(category, key);
            return bool.TryParse(value, out var boolValue) ? boolValue : defaultValue;
        }

        public async Task<SystemSetting?> GetSettingAsync(string category, string key)
        {
            return await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.Category == category && s.Key == key);
        }

        public async Task<List<SystemSetting>> GetSettingsByCategoryAsync(string category)
        {
            return await _context.SystemSettings
                .Where(s => s.Category == category)
                .OrderBy(s => s.Key)
                .ToListAsync();
        }

        public async Task SetSettingValueAsync(string category, string key, string value, string? description = null)
        {
            var setting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.Category == category && s.Key == key);

            if (setting != null)
            {
                setting.Value = value;
                setting.UpdatedAt = DateTime.Now;
                if (description != null)
                {
                    setting.Description = description;
                }
            }
            else
            {
                setting = new SystemSetting
                {
                    Category = category,
                    Key = key,
                    Value = value,
                    Description = description,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                _context.SystemSettings.Add(setting);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<bool> SettingExistsAsync(string category, string key)
        {
            return await _context.SystemSettings
                .AnyAsync(s => s.Category == category && s.Key == key);
        }
    }
}