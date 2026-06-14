using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dinder.Infrastructure.Persistence.Migrations.Analytics
{
    /// <inheritdoc />
    public partial class InitialAnalytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "analytics");

            migrationBuilder.CreateTable(
                name: "daily_active_users",
                schema: "analytics",
                columns: table => new
                {
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    UserCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_active_users", x => x.Date);
                });

            migrationBuilder.CreateTable(
                name: "subscription_snapshots",
                schema: "analytics",
                columns: table => new
                {
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Tier = table.Column<string>(type: "text", nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription_snapshots", x => new { x.Date, x.Tier });
                });

            migrationBuilder.CreateTable(
                name: "swipe_metrics",
                schema: "analytics",
                columns: table => new
                {
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalSwipes = table.Column<int>(type: "integer", nullable: false),
                    TotalRightSwipes = table.Column<int>(type: "integer", nullable: false),
                    TotalMatches = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_swipe_metrics", x => x.Date);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "daily_active_users",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "subscription_snapshots",
                schema: "analytics");

            migrationBuilder.DropTable(
                name: "swipe_metrics",
                schema: "analytics");
        }
    }
}
