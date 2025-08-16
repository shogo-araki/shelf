using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using shelf_project.Data;
using shelf_project.Models;

namespace shelf_project.Controllers
{
    /// <summary>
    /// 契約・解約管理機能を提供するコントローラー
    /// 代理店の契約状況確認、解約申請、棚返却管理などの契約ライフサイクル全体を管理
    /// 1年間の最低契約期間と棚返却要件を含むビジネスルールを実装
    /// </summary>
    [Authorize]
    public class ContractController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        /// <summary>
        /// ContractControllerのコンストラクタ
        /// </summary>
        /// <param name="context">データベースコンテキスト</param>
        /// <param name="userManager">ASP.NET Core Identityのユーザー管理</param>
        public ContractController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// 契約情報表示画面
        /// 現在の契約状況、解約可能日、棚返却状況などの契約詳細を表示
        /// 解約申請ボタンや棚返却状況の確認が可能
        /// locationIdが指定された場合は該当拠点の契約情報を表示
        /// </summary>
        /// <param name="locationId">表示対象の拠点ID（オプション）</param>
        /// <returns>契約情報表示画面</returns>
        public async Task<IActionResult> Index(int? locationId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Distributor)
            {
                return Forbid();
            }

            Distributor? targetDistributor = null;

            if (locationId.HasValue)
            {
                // 特定の拠点が指定された場合のアクセス制御
                var userDistributors = await _context.Distributors
                    .Include(d => d.Company)
                    .Where(d => d.UserId == user.Id)
                    .ToListAsync();

                // 本社アカウントかチェック
                var headOfficeDistributor = userDistributors.FirstOrDefault(d => 
                    d.DistributorType == DistributorType.HeadOffice && 
                    d.Company?.CompanyType == CompanyType.Chain);

                if (headOfficeDistributor != null)
                {
                    // 本社の場合、同じ会社の拠点の契約情報にアクセス可能
                    targetDistributor = await _context.Distributors
                        .Include(d => d.Company)
                        .Where(d => d.Id == locationId.Value && d.CompanyId == headOfficeDistributor.CompanyId)
                        .FirstOrDefaultAsync();
                }
                else
                {
                    // 本社でない場合は自分の拠点のみアクセス可能
                    targetDistributor = await _context.Distributors
                        .Include(d => d.Company)
                        .Where(d => d.Id == locationId.Value && d.UserId == user.Id)
                        .FirstOrDefaultAsync();
                }

                if (targetDistributor == null)
                {
                    return Forbid();
                }
            }
            else
            {
                // 拠点IDが指定されていない場合はデフォルト拠点
                targetDistributor = await _context.Distributors
                    .Include(d => d.Company)
                    .FirstOrDefaultAsync(d => d.UserId == user.Id);
            }

            if (targetDistributor == null)
            {
                return NotFound();
            }

            // 複数拠点がある場合は拠点選択情報も渡す
            var allUserDistributors = await _context.Distributors
                .Include(d => d.Company)
                .Where(d => d.UserId == user.Id)
                .ToListAsync();

            // 本社の場合は管理下の全拠点も取得
            var headOffice = allUserDistributors.FirstOrDefault(d => 
                d.DistributorType == DistributorType.HeadOffice && 
                d.Company?.CompanyType == CompanyType.Chain);

            if (headOffice != null)
            {
                var allManagedDistributors = await _context.Distributors
                    .Include(d => d.Company)
                    .Where(d => d.CompanyId == headOffice.CompanyId)
                    .OrderBy(d => d.LocationName)
                    .ToListAsync();
                ViewBag.AllDistributors = allManagedDistributors;
            }
            else
            {
                ViewBag.AllDistributors = allUserDistributors;
            }

            ViewBag.CurrentDistributorId = targetDistributor.Id;

            return View(targetDistributor);
        }

        /// <summary>
        /// 解約申請処理
        /// 契約開始から1年後からの解約申請を受け付け
        /// 解約申請時に棚返却予定日を自動設定（申請から30日後）
        /// </summary>
        /// <param name="distributorId">解約申請対象の代理店ID</param>
        /// <returns>契約情報画面にリダイレクト</returns>
        [HttpPost]
        public async Task<IActionResult> RequestCancellation(int distributorId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Distributor)
            {
                return Forbid();
            }

            // アクセス権限チェック
            var userDistributors = await _context.Distributors
                .Include(d => d.Company)
                .Where(d => d.UserId == user.Id)
                .ToListAsync();

            var headOfficeDistributor = userDistributors.FirstOrDefault(d => 
                d.DistributorType == DistributorType.HeadOffice && 
                d.Company?.CompanyType == CompanyType.Chain);

            Distributor? targetDistributor = null;

            if (headOfficeDistributor != null)
            {
                // 本社の場合、同じ会社の拠点の解約申請が可能
                targetDistributor = await _context.Distributors
                    .Where(d => d.Id == distributorId && d.CompanyId == headOfficeDistributor.CompanyId)
                    .FirstOrDefaultAsync();
            }
            else
            {
                // 本社でない場合は自分の拠点のみ解約申請可能
                targetDistributor = await _context.Distributors
                    .Where(d => d.Id == distributorId && d.UserId == user.Id)
                    .FirstOrDefaultAsync();
            }

            if (targetDistributor == null)
            {
                return Forbid();
            }

            // 解約可能かチェック
            if (!targetDistributor.CanCancelContract)
            {
                TempData["Error"] = $"契約開始から1年後（{targetDistributor.ContractMaturityDate:yyyy年MM月dd日}）以降に解約申請が可能です。";
                return RedirectToAction("Index", new { locationId = distributorId });
            }

            // 既に解約申請済みかチェック
            if (targetDistributor.ContractStatus != ContractStatus.Active)
            {
                TempData["Error"] = "既に解約申請が処理中です。";
                return RedirectToAction("Index", new { locationId = distributorId });
            }

            // 解約申請処理
            targetDistributor.ContractStatus = ContractStatus.CancellationRequested;
            targetDistributor.CancellationRequestDate = DateTime.Now;
            targetDistributor.ShelfReturnDueDate = DateTime.Now.AddDays(30); // 30日後に返却予定
            targetDistributor.ShelfReturnStatus = ShelfReturnStatus.Scheduled;
            targetDistributor.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "解約申請を受け付けました。30日以内に棚の返却をお願いいたします。";
            return RedirectToAction("Index", new { locationId = distributorId });
        }

        /// <summary>
        /// 棚返却完了報告処理
        /// 代理店が棚を返却した際の完了報告を受け付け
        /// 棚返却完了後、契約を正式に終了状態に変更
        /// </summary>
        /// <param name="distributorId">対象代理店ID</param>
        /// <returns>契約情報画面にリダイレクト</returns>
        [HttpPost]
        public async Task<IActionResult> ConfirmShelfReturn(int distributorId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Distributor)
            {
                return Forbid();
            }

            // アクセス権限チェック
            var userDistributors = await _context.Distributors
                .Include(d => d.Company)
                .Where(d => d.UserId == user.Id)
                .ToListAsync();

            var headOfficeDistributor = userDistributors.FirstOrDefault(d => 
                d.DistributorType == DistributorType.HeadOffice && 
                d.Company?.CompanyType == CompanyType.Chain);

            Distributor? targetDistributor = null;

            if (headOfficeDistributor != null)
            {
                // 本社の場合、同じ会社の拠点の棚返却報告が可能
                targetDistributor = await _context.Distributors
                    .Where(d => d.Id == distributorId && d.CompanyId == headOfficeDistributor.CompanyId)
                    .FirstOrDefaultAsync();
            }
            else
            {
                // 本社でない場合は自分の拠点のみ棚返却報告可能
                targetDistributor = await _context.Distributors
                    .Where(d => d.Id == distributorId && d.UserId == user.Id)
                    .FirstOrDefaultAsync();
            }

            if (targetDistributor == null)
            {
                return Forbid();
            }

            // 解約申請済みかチェック
            if (targetDistributor.ContractStatus != ContractStatus.CancellationRequested)
            {
                TempData["Error"] = "解約申請が行われていません。";
                return RedirectToAction("Index", new { locationId = distributorId });
            }

            // 棚返却完了処理
            targetDistributor.ShelfReturnedDate = DateTime.Now;
            targetDistributor.ShelfReturnStatus = ShelfReturnStatus.Completed;
            targetDistributor.ContractStatus = ContractStatus.PendingShelfReturn;
            targetDistributor.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "棚返却の報告を受け付けました。確認後、契約解約処理を完了いたします。";
            return RedirectToAction("Index", new { locationId = distributorId });
        }

        /// <summary>
        /// 契約延長処理
        /// 解約申請をキャンセルして契約を継続する場合の処理
        /// 棚返却予定もキャンセルされ、通常の契約状態に戻る
        /// </summary>
        /// <param name="distributorId">対象代理店ID</param>
        /// <returns>契約情報画面にリダイレクト</returns>
        [HttpPost]
        public async Task<IActionResult> ExtendContract(int distributorId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.Role != UserRole.Distributor)
            {
                return Forbid();
            }

            // アクセス権限チェック
            var userDistributors = await _context.Distributors
                .Include(d => d.Company)
                .Where(d => d.UserId == user.Id)
                .ToListAsync();

            var headOfficeDistributor = userDistributors.FirstOrDefault(d => 
                d.DistributorType == DistributorType.HeadOffice && 
                d.Company?.CompanyType == CompanyType.Chain);

            Distributor? targetDistributor = null;

            if (headOfficeDistributor != null)
            {
                // 本社の場合、同じ会社の拠点の契約延長が可能
                targetDistributor = await _context.Distributors
                    .Where(d => d.Id == distributorId && d.CompanyId == headOfficeDistributor.CompanyId)
                    .FirstOrDefaultAsync();
            }
            else
            {
                // 本社でない場合は自分の拠点のみ契約延長可能
                targetDistributor = await _context.Distributors
                    .Where(d => d.Id == distributorId && d.UserId == user.Id)
                    .FirstOrDefaultAsync();
            }

            if (targetDistributor == null)
            {
                return Forbid();
            }

            // 解約申請中でないとキャンセルできない
            if (targetDistributor.ContractStatus != ContractStatus.CancellationRequested)
            {
                TempData["Error"] = "解約申請が行われていないため、契約延長はできません。";
                return RedirectToAction("Index", new { locationId = distributorId });
            }

            // 棚返却が完了していたらキャンセル不可
            if (targetDistributor.ShelfReturnStatus == ShelfReturnStatus.Completed)
            {
                TempData["Error"] = "棚返却が完了しているため、契約延長はできません。";
                return RedirectToAction("Index", new { locationId = distributorId });
            }

            // 契約延長処理（解約申請をキャンセル）
            targetDistributor.ContractStatus = ContractStatus.Active;
            targetDistributor.CancellationRequestDate = null;
            targetDistributor.ShelfReturnDueDate = null;
            targetDistributor.ShelfReturnStatus = ShelfReturnStatus.NotRequired;
            targetDistributor.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "契約を延長しました。解約申請がキャンセルされました。";
            return RedirectToAction("Index", new { locationId = distributorId });
        }

        /// <summary>
        /// 管理者用：契約一覧表示
        /// 全代理店の契約状況、解約申請状況、棚返却状況を一覧表示
        /// 管理者が契約状況を監視・管理するための画面
        /// </summary>
        /// <returns>契約一覧管理画面</returns>
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminList()
        {
            var distributors = await _context.Distributors
                .Include(d => d.Company)
                .Include(d => d.User)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            return View(distributors);
        }

        /// <summary>
        /// 管理者用：契約解約完了処理
        /// 棚返却確認後、管理者が契約を正式に解約完了状態に変更
        /// 契約終了日を設定し、拠点を削除（本社の場合は会社全体、店舗の場合は該当拠点のみ）
        /// </summary>
        /// <param name="distributorId">対象代理店ID</param>
        /// <returns>契約一覧画面にリダイレクト</returns>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CompleteCancellation(int distributorId)
        {
            var distributor = await _context.Distributors
                .Include(d => d.Company)
                .Include(d => d.QRCodes)
                .Include(d => d.DistributorProducts)
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == distributorId);

            if (distributor == null)
            {
                return NotFound();
            }

            // 棚返却完了済みかチェック
            if (distributor.ShelfReturnStatus != ShelfReturnStatus.Completed)
            {
                TempData["Error"] = "棚返却が完了していません。";
                return RedirectToAction("AdminList");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                string deletedInfo = "";

                if (distributor.DistributorType == DistributorType.HeadOffice && distributor.Company != null)
                {
                    // 本社の解約：チェーン全体を削除
                    var allChainLocations = await _context.Distributors
                        .Include(d => d.QRCodes)
                        .Include(d => d.DistributorProducts)
                        .Include(d => d.User)
                        .Where(d => d.CompanyId == distributor.CompanyId)
                        .ToListAsync();

                    deletedInfo = $"チェーン店「{distributor.Company.CompanyName}」（{allChainLocations.Count}拠点）";

                    // 各拠点の関連データを削除
                    foreach (var location in allChainLocations)
                    {
                        await DeleteDistributorAndRelatedData(location);
                    }

                    // 会社情報も削除
                    _context.Companies.Remove(distributor.Company);
                }
                else if (distributor.DistributorType == DistributorType.Individual)
                {
                    // 独立代理店の解約：該当拠点のみ削除
                    deletedInfo = $"独立代理店「{distributor.CompanyName}」";
                    await DeleteDistributorAndRelatedData(distributor);
                }
                else if (distributor.DistributorType == DistributorType.Store)
                {
                    // チェーン店の子拠点の解約：該当拠点のみ削除
                    deletedInfo = $"拠点「{distributor.LocationName ?? distributor.CompanyName}」";
                    await DeleteDistributorAndRelatedData(distributor);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = $"{deletedInfo}の契約解約処理が完了し、関連データを削除しました。";
                return RedirectToAction("AdminList");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = $"解約処理中にエラーが発生しました: {ex.Message}";
                return RedirectToAction("AdminList");
            }
        }

        /// <summary>
        /// 代理店と関連データを削除するヘルパーメソッド
        /// QRコード、商品選定、ユーザーアカウントを含む全関連データを安全に削除
        /// </summary>
        /// <param name="distributor">削除対象の代理店</param>
        private async Task DeleteDistributorAndRelatedData(Distributor distributor)
        {
            // QRコードとその関連データを削除
            if (distributor.QRCodes?.Any() == true)
            {
                foreach (var qrCode in distributor.QRCodes)
                {
                    // QRCodeProductsを削除
                    var qrCodeProducts = await _context.QRCodeProducts
                        .Where(qcp => qcp.QRCodeId == qrCode.Id)
                        .ToListAsync();
                    _context.QRCodeProducts.RemoveRange(qrCodeProducts);
                }
                _context.QRCodes.RemoveRange(distributor.QRCodes);
            }

            // 商品選定を削除
            if (distributor.DistributorProducts?.Any() == true)
            {
                _context.DistributorProducts.RemoveRange(distributor.DistributorProducts);
            }

            // 売上データは履歴として保持（Distributorへの参照のみNULLに設定）
            var sales = await _context.Sales
                .Where(s => s.DistributorId == distributor.Id)
                .ToListAsync();
            foreach (var sale in sales)
            {
                sale.DistributorId = null; // 参照を削除するが履歴は保持
            }

            // 代理店を削除
            _context.Distributors.Remove(distributor);

            // ユーザーアカウントを無効化（完全削除ではなく無効化）
            if (distributor.User != null)
            {
                distributor.User.LockoutEnabled = true;
                distributor.User.LockoutEnd = DateTimeOffset.MaxValue; // 永続的にロックアウト
            }
        }

        /// <summary>
        /// 管理者用：棚返却期限超過の代理店にペナルティ設定
        /// 棚返却期限を過ぎた代理店のステータスを期限超過に変更
        /// </summary>
        /// <param name="distributorId">対象代理店ID</param>
        /// <returns>契約一覧画面にリダイレクト</returns>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> MarkOverdue(int distributorId)
        {
            var distributor = await _context.Distributors
                .FirstOrDefaultAsync(d => d.Id == distributorId);

            if (distributor == null)
            {
                return NotFound();
            }

            // 棚返却期限をチェック
            if (distributor.ShelfReturnDueDate == null || distributor.ShelfReturnDueDate > DateTime.Now)
            {
                TempData["Error"] = "棚返却期限が設定されていないか、まだ期限前です。";
                return RedirectToAction("AdminList");
            }

            distributor.ShelfReturnStatus = ShelfReturnStatus.Overdue;
            distributor.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"代理店「{distributor.CompanyName}」を期限超過にマークしました。";
            return RedirectToAction("AdminList");
        }
    }
}