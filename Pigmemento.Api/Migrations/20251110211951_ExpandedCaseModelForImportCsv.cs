using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pigmemento.Api.Migrations
{
    /// <inheritdoc />
    public partial class ExpandedCaseModelForImportCsv : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "AdditionalDiagnoses",
                table: "Cases",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryDiagnosis",
                table: "Cases",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Cases",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SourceId",
                table: "Cases",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdditionalDiagnoses",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "PrimaryDiagnosis",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "SourceId",
                table: "Cases");
        }
    }
}
