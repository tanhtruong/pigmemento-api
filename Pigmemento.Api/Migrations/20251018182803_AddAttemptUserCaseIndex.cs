using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pigmemento.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAttemptUserCaseIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Attempts_UserId_CaseId",
                table: "attempts",
                columns: new[] { "user_id", "case_id" });

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Attempts_UserId_CaseId",
                table: "attempts");
        }
    }
}
