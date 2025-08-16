using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using shelf_project.Data;
using shelf_project.Models;

namespace shelf_project.Controllers
{
    /// <summary>
    /// チェーン店管理機能を提供するコントローラー
    /// 本社アカウントが拠点管理、売上管理、本社コード管理などの機能を利用するためのエンドポイントを提供
    /// </summary>
    [Authorize]
    public class CompanyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        /// <summary>
        /// CompanyControllerのコンストラクタ
        /// </summary>
        /// <param name="context">データベースコンテキスト</param>
        /// <param name="userManager">ASP.NET Core Identityのユーザー管理</param>
        public CompanyController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        /// <summary>
        /// チェーン店の拠点一覧表示と管理機能
        /// 本社アカウントが所属するチェーン店の全拠点を表示し、
        /// 各拠点の詳細情報、QRコード状況、売上実績などを確認できる
        /// </summary>
        /// <returns>拠点一覧画面</returns>
        public async Task<IActionResult> Locations()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Distributor)
            {
                return Forbid();
            }

            var distributor = await _context.Distributors
                .Include(d => d.Company)
                .FirstOrDefaultAsync(d => d.UserId == user.Id);

            if (distributor?.Company == null || distributor.DistributorType != DistributorType.HeadOffice)
            {
                return Forbid();
            }

            var allLocations = await _context.Distributors
                .Include(d => d.QRCodes)
                .Include(d => d.Sales)
                .ThenInclude(s => s.Order)
                .Where(d => d.CompanyId == distributor.CompanyId)
                .OrderBy(d => d.LocationName)
                .ToListAsync();

            ViewBag.Company = distributor.Company;
            return View(allLocations);
        }

        /// <summary>
        /// 本社コード管理画面
        /// チェーン店の本社コードの表示と再生成機能を提供
        /// 新拠点がチェーンに参加する際に必要なコードの管理
        /// </summary>
        /// <returns>本社コード管理画面</returns>
        public async Task<IActionResult> HeadOfficeCode()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Distributor)
            {
                return Forbid();
            }

            var distributor = await _context.Distributors
                .Include(d => d.Company)
                .ThenInclude(c => c.Distributors)
                .FirstOrDefaultAsync(d => d.UserId == user.Id);

            if (distributor?.Company == null || distributor.DistributorType != DistributorType.HeadOffice)
            {
                return Forbid();
            }

            ViewBag.Company = distributor.Company;
            return View(distributor);
        }

        /// <summary>
        /// 本社コードの再生成
        /// セキュリティ上の理由で本社コードを変更する必要がある場合に使用
        /// 既存の本社コードが漏洩した場合などに新しいコードを生成
        /// </summary>
        /// <returns>本社コード管理画面にリダイレクト</returns>
        [HttpPost]
        public async Task<IActionResult> RegenerateHeadOfficeCode()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Distributor)
            {
                return Forbid();
            }

            var distributor = await _context.Distributors
                .Include(d => d.Company)
                .FirstOrDefaultAsync(d => d.UserId == user.Id);

            if (distributor?.Company == null || distributor.DistributorType != DistributorType.HeadOffice)
            {
                return Forbid();
            }

            // 新しい本社コードを生成（8桁の数字、重複チェック付き）
            string newCode;
            do
            {
                newCode = new Random().Next(10000000, 99999999).ToString();
            }
            while (_context.Companies.Any(c => c.HeadOfficeCode == newCode));

            distributor.Company.HeadOfficeCode = newCode;
            await _context.SaveChangesAsync();

            TempData["Success"] = "本社コードを再生成しました。";
            return RedirectToAction("HeadOfficeCode");
        }

        /// <summary>
        /// チェーン店全体の売上管理画面
        /// 本社が所属する全拠点の売上データを統合して表示
        /// 拠点別、商品別、期間別の売上分析が可能
        /// </summary>
        /// <returns>売上管理画面</returns>
        public async Task<IActionResult> Sales()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Distributor)
            {
                return Forbid();
            }

            var distributor = await _context.Distributors
                .Include(d => d.Company)
                .FirstOrDefaultAsync(d => d.UserId == user.Id);

            if (distributor?.Company == null || distributor.DistributorType != DistributorType.HeadOffice)
            {
                return Forbid();
            }

            var allSales = await _context.Sales
                .Include(s => s.Order)
                .ThenInclude(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(s => s.Distributor)
                .Include(s => s.QRCode)
                .Where(s => s.Distributor.CompanyId == distributor.CompanyId)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            ViewBag.Company = distributor.Company;
            return View(allSales);
        }

        /// <summary>
        /// チェーン店の子拠点削除
        /// 本社が管理する子拠点（店舗）を削除する機能
        /// 売上履歴がある拠点は削除不可、関連するQRコードと商品選定も同時に削除
        /// </summary>
        /// <param name="locationId">削除対象の拠点ID</param>
        /// <returns>拠点一覧画面にリダイレクト</returns>
        [HttpPost]
        public async Task<IActionResult> DeleteLocation(int locationId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Distributor)
            {
                return Forbid();
            }

            var headOffice = await _context.Distributors
                .Include(d => d.Company)
                .FirstOrDefaultAsync(d => d.UserId == user.Id);

            if (headOffice?.Company == null || headOffice.DistributorType != DistributorType.HeadOffice)
            {
                return Forbid();
            }

            var locationToDelete = await _context.Distributors
                .Include(d => d.QRCodes)
                .Include(d => d.Sales)
                .Include(d => d.DistributorProducts)
                .Where(d => d.Id == locationId && d.CompanyId == headOffice.CompanyId && d.DistributorType == DistributorType.Store)
                .FirstOrDefaultAsync();

            if (locationToDelete == null)
            {
                TempData["Error"] = "指定された拠点が見つからないか、削除権限がありません。";
                return RedirectToAction("Locations");
            }

            // 本社は削除不可
            if (locationToDelete.DistributorType == DistributorType.HeadOffice)
            {
                TempData["Error"] = "本社は削除できません。";
                return RedirectToAction("Locations");
            }

            // 関連データの確認（売上履歴がある場合は削除不可）
            var hasSales = locationToDelete.Sales?.Any() == true;
            if (hasSales)
            {
                TempData["Error"] = $"拠点「{locationToDelete.LocationName}」には売上履歴があるため削除できません。";
                return RedirectToAction("Locations");
            }

            // 関連データを削除（QRコード、商品選定）
            if (locationToDelete.QRCodes?.Any() == true)
            {
                _context.QRCodes.RemoveRange(locationToDelete.QRCodes);
            }

            if (locationToDelete.DistributorProducts?.Any() == true)
            {
                _context.DistributorProducts.RemoveRange(locationToDelete.DistributorProducts);
            }

            // 拠点を削除
            _context.Distributors.Remove(locationToDelete);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"拠点「{locationToDelete.LocationName}」を削除しました。";
            return RedirectToAction("Locations");
        }

        /// <summary>
        /// 拠点別商品選定管理
        /// 本社が特定の拠点の商品選定状況を確認・管理する機能
        /// 各拠点の選定商品一覧表示、新規商品追加、選定解除が可能
        /// </summary>
        /// <param name="locationId">管理対象の拠点ID</param>
        /// <returns>拠点商品管理画面</returns>
        public async Task<IActionResult> LocationProducts(int locationId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Distributor)
            {
                return Forbid();
            }

            var headOffice = await _context.Distributors
                .Include(d => d.Company)
                .FirstOrDefaultAsync(d => d.UserId == user.Id);

            if (headOffice?.Company == null || headOffice.DistributorType != DistributorType.HeadOffice)
            {
                return Forbid();
            }

            var location = await _context.Distributors
                .Include(d => d.DistributorProducts)
                .ThenInclude(dp => dp.Product)
                .ThenInclude(p => p.Manufacturer)
                .Where(d => d.Id == locationId && d.CompanyId == headOffice.CompanyId)
                .FirstOrDefaultAsync();

            if (location == null)
            {
                return NotFound();
            }

            // 利用可能な商品を取得（既に選定済みでない商品）
            var selectedProductIds = location.DistributorProducts?.Where(dp => dp.IsActive).Select(dp => dp.ProductId).ToList() ?? new List<int>();
            
            var availableProducts = await _context.Products
                .Include(p => p.Manufacturer)
                .Where(p => p.IsActive && !selectedProductIds.Contains(p.Id))
                .OrderBy(p => p.Name)
                .ToListAsync();

            ViewBag.Location = location;
            ViewBag.HeadOffice = headOffice;
            ViewBag.AvailableProducts = availableProducts;
            ViewBag.MaxProducts = 5; // 各拠点は5商品まで選定可能

            return View(location.DistributorProducts?.Where(dp => dp.IsActive).ToList() ?? new List<DistributorProduct>());
        }

        /// <summary>
        /// 拠点への商品追加
        /// 本社が特定の拠点に商品を追加選定する機能
        /// 選定数制限（5商品）や重複選定のチェックを実行
        /// </summary>
        /// <param name="locationId">対象拠点ID</param>
        /// <param name="productId">追加する商品ID</param>
        /// <returns>拠点商品管理画面にリダイレクト</returns>
        [HttpPost]
        public async Task<IActionResult> AddProductToLocation(int locationId, int productId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Distributor)
            {
                return Forbid();
            }

            var headOffice = await _context.Distributors
                .Include(d => d.Company)
                .FirstOrDefaultAsync(d => d.UserId == user.Id);

            if (headOffice?.Company == null || headOffice.DistributorType != DistributorType.HeadOffice)
            {
                return Forbid();
            }

            var location = await _context.Distributors
                .Include(d => d.DistributorProducts)
                .Where(d => d.Id == locationId && d.CompanyId == headOffice.CompanyId)
                .FirstOrDefaultAsync();

            if (location == null)
            {
                return NotFound();
            }

            // 選定数制限チェック（各拠点最大5商品まで）
            var currentCount = location.DistributorProducts?.Count(dp => dp.IsActive) ?? 0;
            if (currentCount >= 5)
            {
                TempData["Error"] = "各拠点は最大5商品まで選定可能です。";
                return RedirectToAction("LocationProducts", new { locationId });
            }

            // 既に選定済みかチェック（非アクティブな場合は再アクティベート）
            var existingProduct = location.DistributorProducts?.FirstOrDefault(dp => dp.ProductId == productId);
            if (existingProduct != null)
            {
                if (existingProduct.IsActive)
                {
                    TempData["Error"] = "この商品は既に選定済みです。";
                }
                else
                {
                    existingProduct.IsActive = true;
                    existingProduct.AssignedAt = DateTime.Now;
                    TempData["Success"] = "商品を再選定しました。";
                }
            }
            else
            {
                var distributorProduct = new DistributorProduct
                {
                    DistributorId = locationId,
                    ProductId = productId,
                    IsActive = true,
                    AssignedAt = DateTime.Now
                };
                _context.DistributorProducts.Add(distributorProduct);
                TempData["Success"] = "商品を選定しました。";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("LocationProducts", new { locationId });
        }

        /// <summary>
        /// 拠点からの商品選定解除
        /// 本社が特定の拠点から商品選定を解除する機能
        /// 商品は物理削除せず、IsActiveフラグをfalseにして履歴を保持
        /// </summary>
        /// <param name="locationId">対象拠点ID</param>
        /// <param name="productId">解除する商品ID</param>
        /// <returns>拠点商品管理画面にリダイレクト</returns>
        [HttpPost]
        public async Task<IActionResult> RemoveProductFromLocation(int locationId, int productId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Distributor)
            {
                return Forbid();
            }

            var headOffice = await _context.Distributors
                .Include(d => d.Company)
                .FirstOrDefaultAsync(d => d.UserId == user.Id);

            if (headOffice?.Company == null || headOffice.DistributorType != DistributorType.HeadOffice)
            {
                return Forbid();
            }

            var distributorProduct = await _context.DistributorProducts
                .Include(dp => dp.Distributor)
                .Where(dp => dp.DistributorId == locationId && dp.ProductId == productId && dp.Distributor.CompanyId == headOffice.CompanyId)
                .FirstOrDefaultAsync();

            if (distributorProduct == null)
            {
                return NotFound();
            }

            // 論理削除（履歴保持のため物理削除は行わない）
            distributorProduct.IsActive = false;
            distributorProduct.RemovedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "商品の選定を解除しました。";
            return RedirectToAction("LocationProducts", new { locationId });
        }
    }
}