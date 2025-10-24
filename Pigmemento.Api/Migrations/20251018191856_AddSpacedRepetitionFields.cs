using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pigmemento.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSpacedRepetitionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_user_case_stats_next_due_at",
                table: "user_case_stats",
                newName: "ix_user_case_stats_next_due_at");

            migrationBuilder.AlterColumn<DateTime>(
                name: "next_due_at",
                table: "user_case_stats",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "NOW() AT TIME ZONE 'UTC'");

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_attempt_at",
                table: "user_case_stats",
                type: "timestamp with time zone",
                nullable: true,
                defaultValueSql: "now()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "NOW() AT TIME ZONE 'UTC'");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "user_case_stats",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<double>(
                name: "ease_factor",
                table: "user_case_stats",
                type: "double precision",
                nullable: false,
                defaultValue: 2.5);

            migrationBuilder.AddColumn<int>(
                name: "interval_days",
                table: "user_case_stats",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "last_latency_ms",
                table: "user_case_stats",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "last_result",
                table: "user_case_stats",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_seen_at",
                table: "user_case_stats",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "recently_wrong_at",
                table: "user_case_stats",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_case_stats_last_seen_at",
                table: "user_case_stats",
                column: "last_seen_at",
                filter: "\"last_seen_at\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_user_case_stats_recently_wrong_at",
                table: "user_case_stats",
                column: "recently_wrong_at",
                filter: "\"recently_wrong_at\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_user_case_stats_user_due",
                table: "user_case_stats",
                columns: new[] { "UserId", "next_due_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_user_case_stats_last_seen_at",
                table: "user_case_stats");

            migrationBuilder.DropIndex(
                name: "ix_user_case_stats_recently_wrong_at",
                table: "user_case_stats");

            migrationBuilder.DropIndex(
                name: "ix_user_case_stats_user_due",
                table: "user_case_stats");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "user_case_stats");

            migrationBuilder.DropColumn(
                name: "ease_factor",
                table: "user_case_stats");

            migrationBuilder.DropColumn(
                name: "interval_days",
                table: "user_case_stats");

            migrationBuilder.DropColumn(
                name: "last_latency_ms",
                table: "user_case_stats");

            migrationBuilder.DropColumn(
                name: "last_result",
                table: "user_case_stats");

            migrationBuilder.DropColumn(
                name: "last_seen_at",
                table: "user_case_stats");

            migrationBuilder.DropColumn(
                name: "recently_wrong_at",
                table: "user_case_stats");

            migrationBuilder.RenameIndex(
                name: "ix_user_case_stats_next_due_at",
                table: "user_case_stats",
                newName: "IX_user_case_stats_next_due_at");

            migrationBuilder.AlterColumn<DateTime>(
                name: "next_due_at",
                table: "user_case_stats",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW() AT TIME ZONE 'UTC'",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "last_attempt_at",
                table: "user_case_stats",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW() AT TIME ZONE 'UTC'",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldDefaultValueSql: "now()");
        }
    }
}
