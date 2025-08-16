using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace shelf_project.Migrations
{
    /// <inheritdoc />
    public partial class AddDistributorLocationSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsHeadquarters",
                table: "Distributors",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LocationName",
                table: "Distributors",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentDistributorId",
                table: "Distributors",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Distributors_ParentDistributorId",
                table: "Distributors",
                column: "ParentDistributorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Distributors_Distributors_ParentDistributorId",
                table: "Distributors",
                column: "ParentDistributorId",
                principalTable: "Distributors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Distributors_Distributors_ParentDistributorId",
                table: "Distributors");

            migrationBuilder.DropIndex(
                name: "IX_Distributors_ParentDistributorId",
                table: "Distributors");

            migrationBuilder.DropColumn(
                name: "IsHeadquarters",
                table: "Distributors");

            migrationBuilder.DropColumn(
                name: "LocationName",
                table: "Distributors");

            migrationBuilder.DropColumn(
                name: "ParentDistributorId",
                table: "Distributors");
        }
    }
}
