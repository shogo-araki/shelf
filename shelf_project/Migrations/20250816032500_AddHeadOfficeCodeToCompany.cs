using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace shelf_project.Migrations
{
    /// <inheritdoc />
    public partial class AddHeadOfficeCodeToCompany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HeadOfficeCode",
                table: "Companies",
                type: "TEXT",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HeadOfficeCode",
                table: "Companies");
        }
    }
}
