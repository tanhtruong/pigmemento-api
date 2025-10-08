using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pigmemento.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "patient_fitzpatrick",
                table: "cases",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "patient_sex",
                table: "cases",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "patient_fitzpatrick",
                table: "cases");

            migrationBuilder.DropColumn(
                name: "patient_sex",
                table: "cases");
        }
    }
}
