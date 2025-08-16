using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace shelf_project.Migrations
{
    /// <inheritdoc />
    public partial class AddContractManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CancellationRequestDate",
                table: "Distributors",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ContractStatus",
                table: "Distributors",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ShelfReturnDueDate",
                table: "Distributors",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ShelfReturnStatus",
                table: "Distributors",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ShelfReturnedDate",
                table: "Distributors",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancellationRequestDate",
                table: "Distributors");

            migrationBuilder.DropColumn(
                name: "ContractStatus",
                table: "Distributors");

            migrationBuilder.DropColumn(
                name: "ShelfReturnDueDate",
                table: "Distributors");

            migrationBuilder.DropColumn(
                name: "ShelfReturnStatus",
                table: "Distributors");

            migrationBuilder.DropColumn(
                name: "ShelfReturnedDate",
                table: "Distributors");
        }
    }
}
