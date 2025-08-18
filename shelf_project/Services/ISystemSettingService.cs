using shelf_project.Models;

namespace shelf_project.Services
{
    public interface ISystemSettingService
    {
        Task<string?> GetSettingValueAsync(string category, string key);
        Task<int> GetSettingValueAsIntAsync(string category, string key, int defaultValue = 0);
        Task<decimal> GetSettingValueAsDecimalAsync(string category, string key, decimal defaultValue = 0);
        Task<bool> GetSettingValueAsBoolAsync(string category, string key, bool defaultValue = false);
        Task<SystemSetting?> GetSettingAsync(string category, string key);
        Task<List<SystemSetting>> GetSettingsByCategoryAsync(string category);
        Task SetSettingValueAsync(string category, string key, string value, string? description = null);
        Task<bool> SettingExistsAsync(string category, string key);
    }
}