using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace shelf_project.Models
{
    /// <summary>
    /// アプリケーションユーザーモデル - ASP.NET Core Identityを拡張したユーザー情報管理
    /// システム内の全ユーザー（消費者、代理店、製造者、管理者）の共通属性を定義
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        /// <summary>
        /// ユーザーの名前（名）
        /// </summary>
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// ユーザーの苗字（姓）
        /// </summary>
        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// ユーザーの役割（消費者、代理店、製造者、管理者）
        /// </summary>
        [Required]
        public UserRole Role { get; set; }

        /// <summary>
        /// 会社名（代理店・製造者の場合に使用）
        /// </summary>
        public string? CompanyName { get; set; }

        /// <summary>
        /// アカウント作成日時
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 最終ログイン日時（ログイン状況の追跡用）
        /// </summary>
        public DateTime? LastLoginAt { get; set; }

        /// <summary>
        /// このユーザーに関連する代理店情報のコレクション
        /// </summary>
        /// <remarks>
        /// 1つのユーザーが複数の代理店を持つ可能性に対応
        /// </remarks>
        public virtual ICollection<Distributor>? Distributors { get; set; }

        /// <summary>
        /// このユーザーに関連する製造者情報のコレクション
        /// </summary>
        /// <remarks>
        /// 1つのユーザーが複数の製造者を持つ可能性に対応
        /// </remarks>
        public virtual ICollection<Manufacturer>? Manufacturers { get; set; }
    }

    /// <summary>
    /// ユーザー役割の種類を定義
    /// </summary>
    public enum UserRole
    {
        /// <summary>
        /// 一般消費者 - 商品の購入のみが可能
        /// </summary>
        Consumer = 0,

        /// <summary>
        /// 代理店 - 商品の販売と棚の管理が可能
        /// </summary>
        Distributor = 1,

        /// <summary>
        /// 製造者 - 商品の製造・提供と販売管理が可能
        /// </summary>
        Manufacturer = 2,

        /// <summary>
        /// システム管理者 - 全ての機能とユーザー管理が可能
        /// </summary>
        Admin = 3
    }
}