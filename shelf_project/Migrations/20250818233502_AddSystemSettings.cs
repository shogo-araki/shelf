using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace shelf_project.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QRCodes_Distributors_DistributorId1",
                table: "QRCodes");

            migrationBuilder.DropIndex(
                name: "IX_QRCodes_DistributorId",
                table: "QRCodes");

            migrationBuilder.DropIndex(
                name: "IX_QRCodes_DistributorId1",
                table: "QRCodes");

            migrationBuilder.DropColumn(
                name: "DistributorId1",
                table: "QRCodes");

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QRCodes_DistributorId",
                table: "QRCodes",
                column: "DistributorId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_Category_Key",
                table: "SystemSettings",
                columns: new[] { "Category", "Key" },
                unique: true);

            // Insert default system settings
            var now = DateTime.Now;
            migrationBuilder.InsertData(
                table: "SystemSettings",
                columns: new[] { "Category", "Key", "Value", "Description", "CreatedAt", "UpdatedAt" },
                values: new object[,]
                {
                    { "SHELF", "DEFAULT_SHELF_COUNT", "1", "デフォルトの棚数", now, now },
                    { "SHELF", "DEFAULT_PRODUCT_SELECTION_COUNT", "10", "デフォルトの商品選定数", now, now },
                    { "PRICING", "MONTHLY_FEE_INDIVIDUAL", "5000", "個人店の月額料金", now, now },
                    { "PRICING", "MONTHLY_FEE_CHAIN_STORE", "4000", "チェーン店支店の月額料金", now, now },
                    { "PRICING", "MONTHLY_FEE_HEAD_OFFICE", "6000", "チェーン店本社の月額料金", now, now },
                    { "CONTRACT", "DEFAULT_CONTRACT_DURATION_MONTHS", "12", "デフォルトの契約期間（月）", now, now },
                    { "SYSTEM", "SYSTEM_NAME", "ShelfUp", "システム名", now, now },
                    { "SYSTEM", "SYSTEM_VERSION", "1.0.0", "システムバージョン", now, now }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropIndex(
                name: "IX_QRCodes_DistributorId",
                table: "QRCodes");

            migrationBuilder.AddColumn<int>(
                name: "DistributorId1",
                table: "QRCodes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_QRCodes_DistributorId",
                table: "QRCodes",
                column: "DistributorId");

            migrationBuilder.CreateIndex(
                name: "IX_QRCodes_DistributorId1",
                table: "QRCodes",
                column: "DistributorId1",
                unique: true,
                filter: "[DistributorId1] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_QRCodes_Distributors_DistributorId1",
                table: "QRCodes",
                column: "DistributorId1",
                principalTable: "Distributors",
                principalColumn: "Id");
        }
    }
}
