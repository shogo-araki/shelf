using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using shelf_project.Data;
using shelf_project.Models;

namespace shelf_project.Services
{
    /// <summary>
    /// メーカーアクセス制御サービス - 認証、認可、権限チェック、データアクセス共通処理
    /// </summary>
    public class ManufacturerAccessService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ManufacturerAccessService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// メーカーロールの権限チェック
        /// </summary>
        /// <param name="user">認証済みユーザー</param>
        /// <returns>メーカー権限があるかどうか</returns>
        public bool HasManufacturerRole(ApplicationUser? user)
        {
            return user?.Role == UserRole.Manufacturer;
        }

        /// <summary>
        /// ユーザーに関連するメーカー情報を取得
        /// </summary>
        /// <param name="user">認証済みユーザー</param>
        /// <returns>メーカー情報、存在しない場合はnull</returns>
        public async Task<Models.Manufacturer?> GetManufacturerByUserAsync(ApplicationUser user)
        {
            return await _context.Manufacturers
                .FirstOrDefaultAsync(m => m.UserId == user.Id);
        }

        /// <summary>
        /// メーカーの商品一覧を取得（アクティブのみ）
        /// </summary>
        /// <param name="manufacturerId">メーカーID</param>
        /// <returns>商品リスト</returns>
        public async Task<List<Product>> GetActiveProductsAsync(int manufacturerId)
        {
            return await _context.Products
                .Where(p => p.ManufacturerId == manufacturerId && p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        /// <summary>
        /// メーカーの特定商品にアクセス権限があるかチェック
        /// </summary>
        /// <param name="user">認証済みユーザー</param>
        /// <param name="productId">商品ID</param>
        /// <returns>アクセス権限があるかどうか</returns>
        public async Task<bool> HasAccessToProductAsync(ApplicationUser user, int productId)
        {
            var manufacturer = await GetManufacturerByUserAsync(user);
            if (manufacturer == null) return false;

            return await _context.Products
                .AnyAsync(p => p.Id == productId && p.ManufacturerId == manufacturer.Id && p.IsActive);
        }

        /// <summary>
        /// メーカーの注文一覧を取得（自社商品を含む注文のみ）
        /// </summary>
        /// <param name="manufacturerId">メーカーID</param>
        /// <returns>注文リスト</returns>
        public async Task<List<Order>> GetManufacturerOrdersAsync(int manufacturerId)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(o => o.User)
                .Include(o => o.Distributor)
                .ThenInclude(d => d.User)
                .Where(o => o.OrderItems.Any(oi => oi.Product.ManufacturerId == manufacturerId))
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// メーカーが特定注文にアクセス権限があるかチェック
        /// </summary>
        /// <param name="user">認証済みユーザー</param>
        /// <param name="orderId">注文ID</param>
        /// <returns>アクセス権限があるかどうか</returns>
        public async Task<bool> HasAccessToOrderAsync(ApplicationUser user, int orderId)
        {
            var manufacturer = await GetManufacturerByUserAsync(user);
            if (manufacturer == null) return false;

            return await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .AnyAsync(o => o.Id == orderId && 
                         o.OrderItems.Any(oi => oi.Product.ManufacturerId == manufacturer.Id));
        }

        /// <summary>
        /// メーカーの統計情報を取得
        /// </summary>
        /// <param name="manufacturerId">メーカーID</param>
        /// <returns>統計情報（商品数、注文数、月次売上）</returns>
        public async Task<(int TotalProducts, int TotalOrders, decimal MonthlyRevenue)> GetManufacturerStatsAsync(int manufacturerId)
        {
            var totalProducts = await _context.Products
                .CountAsync(p => p.ManufacturerId == manufacturerId && p.IsActive);

            var totalOrders = await _context.Orders
                .CountAsync(o => o.OrderItems.Any(oi => oi.Product.ManufacturerId == manufacturerId));

            // 月次売上（Settlement テーブルがある場合の処理）
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;
            var monthlyRevenue = await _context.Settlements
                .Where(s => s.ManufacturerId == manufacturerId && 
                           s.CreatedAt.Month == currentMonth && 
                           s.CreatedAt.Year == currentYear &&
                           s.Status == SettlementStatus.Completed)
                .SumAsync(s => s.Amount);

            return (totalProducts, totalOrders, monthlyRevenue);
        }

        /// <summary>
        /// 商品の在庫更新
        /// </summary>
        /// <param name="user">認証済みユーザー</param>
        /// <param name="productId">商品ID</param>
        /// <param name="stockQuantity">新しい在庫数</param>
        /// <returns>更新成功したかどうか</returns>
        public async Task<bool> UpdateProductStockAsync(ApplicationUser user, int productId, int stockQuantity)
        {
            if (!await HasAccessToProductAsync(user, productId))
            {
                return false;
            }

            var product = await _context.Products.FindAsync(productId);
            if (product == null) return false;

            product.StockQuantity = stockQuantity;
            product.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            
            return true;
        }

        /// <summary>
        /// 注文ステータス更新
        /// </summary>
        /// <param name="user">認証済みユーザー</param>
        /// <param name="orderId">注文ID</param>
        /// <param name="status">新しいステータス</param>
        /// <param name="trackingNumber">追跡番号（オプション）</param>
        /// <returns>更新成功したかどうか</returns>
        public async Task<bool> UpdateOrderStatusAsync(ApplicationUser user, int orderId, OrderStatus status, string? trackingNumber = null)
        {
            if (!await HasAccessToOrderAsync(user, orderId))
            {
                return false;
            }

            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return false;

            order.Status = status;
            if (!string.IsNullOrEmpty(trackingNumber))
            {
                order.TrackingNumber = trackingNumber;
            }
            if (status == OrderStatus.Shipped)
            {
                order.ShippedAt = DateTime.Now;
            }
            // UpdatedAt プロパティが存在しないためコメントアウト
            // order.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}