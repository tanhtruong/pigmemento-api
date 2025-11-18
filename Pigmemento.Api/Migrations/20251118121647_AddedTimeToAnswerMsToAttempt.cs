using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pigmemento.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddedTimeToAnswerMsToAttempt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TimeToAnswerMs",
                table: "Attempts",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeToAnswerMs",
                table: "Attempts");
        }
    }
}
