using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace shelf_project.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyAndChainSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DistributorId1",
                table: "QRCodes",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Distributors",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DistributorType",
                table: "Distributors",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CompanyName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    HeadquartersAddress = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PhoneNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CompanyType = table.Column<int>(type: "INTEGER", nullable: false),
                    OwnerUserId = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Companies_AspNetUsers_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QRCodes_DistributorId1",
                table: "QRCodes",
                column: "DistributorId1",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Distributors_CompanyId",
                table: "Distributors",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_OwnerUserId",
                table: "Companies",
                column: "OwnerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Distributors_Companies_CompanyId",
                table: "Distributors",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_QRCodes_Distributors_DistributorId1",
                table: "QRCodes",
                column: "DistributorId1",
                principalTable: "Distributors",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Distributors_Companies_CompanyId",
                table: "Distributors");

            migrationBuilder.DropForeignKey(
                name: "FK_QRCodes_Distributors_DistributorId1",
                table: "QRCodes");

            migrationBuilder.DropTable(
                name: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_QRCodes_DistributorId1",
                table: "QRCodes");

            migrationBuilder.DropIndex(
                name: "IX_Distributors_CompanyId",
                table: "Distributors");

            migrationBuilder.DropColumn(
                name: "DistributorId1",
                table: "QRCodes");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Distributors");

            migrationBuilder.DropColumn(
                name: "DistributorType",
                table: "Distributors");
        }
    }
}
