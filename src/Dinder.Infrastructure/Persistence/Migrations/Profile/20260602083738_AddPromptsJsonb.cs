using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dinder.Infrastructure.Persistence.Migrations.Profile
{
    /// <inheritdoc />
    public partial class AddPromptsJsonb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StripeCustomerId",
                schema: "profile",
                table: "users",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tier",
                schema: "profile",
                table: "users",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Free");

            migrationBuilder.AddColumn<DateTime>(
                name: "BoostedAt",
                schema: "profile",
                table: "profiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "prompts",
                schema: "profile",
                table: "profiles",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IcebreakerCategory",
                schema: "profile",
                table: "conversations",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IcebreakerQuestion",
                schema: "profile",
                table: "conversations",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "profile",
                table: "conversations",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UnmatchedAt",
                schema: "profile",
                table: "conversations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UnmatchedByUserId",
                schema: "profile",
                table: "conversations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "audit_log",
                schema: "profile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdminId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TargetUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_log", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "blocks",
                schema: "profile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BlockerId = table.Column<Guid>(type: "uuid", nullable: false),
                    BlockedId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "device_tokens",
                schema: "profile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Platform = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    IsExpired = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_device_tokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "icebreaker_library",
                schema: "profile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_icebreaker_library", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "media_files",
                schema: "profile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    BlobKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedByAdminId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_media_files", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "messages",
                schema: "profile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                schema: "profile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Body = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    DeepLinkPayload = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "photo_reviews",
                schema: "profile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MediaFileId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RejectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AdultScore = table.Column<float>(type: "real", nullable: true),
                    RacyScore = table.Column<float>(type: "real", nullable: true),
                    ViolenceScore = table.Column<float>(type: "real", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedByAdminId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_photo_reviews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "prompt_catalog",
                schema: "profile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prompt_catalog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "reports",
                schema: "profile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReporterId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportedUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SubCategory = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ResolutionNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "subscriptions",
                schema: "profile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    StripeSubscriptionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Tier = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CurrentPeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscriptions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_conversations_Status",
                schema: "profile",
                table: "conversations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_Action_Timestamp",
                schema: "profile",
                table: "audit_log",
                columns: new[] { "Action", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_AdminId",
                schema: "profile",
                table: "audit_log",
                column: "AdminId");

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_TargetUserId",
                schema: "profile",
                table: "audit_log",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_Timestamp",
                schema: "profile",
                table: "audit_log",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_blocks_BlockedId",
                schema: "profile",
                table: "blocks",
                column: "BlockedId");

            migrationBuilder.CreateIndex(
                name: "IX_blocks_BlockerId_BlockedId",
                schema: "profile",
                table: "blocks",
                columns: new[] { "BlockerId", "BlockedId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_device_tokens_Token",
                schema: "profile",
                table: "device_tokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_device_tokens_UserId_IsExpired",
                schema: "profile",
                table: "device_tokens",
                columns: new[] { "UserId", "IsExpired" });

            migrationBuilder.CreateIndex(
                name: "IX_icebreaker_library_Category",
                schema: "profile",
                table: "icebreaker_library",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_icebreaker_library_IsEnabled",
                schema: "profile",
                table: "icebreaker_library",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_media_files_BlobKey",
                schema: "profile",
                table: "media_files",
                column: "BlobKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_media_files_OwnerId",
                schema: "profile",
                table: "media_files",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_media_files_Status_CreatedAt",
                schema: "profile",
                table: "media_files",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_messages_ConversationId",
                schema: "profile",
                table: "messages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_messages_ConversationId_SentAt",
                schema: "profile",
                table: "messages",
                columns: new[] { "ConversationId", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_messages_SenderId",
                schema: "profile",
                table: "messages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_UserId_CreatedAt",
                schema: "profile",
                table: "notifications",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_notifications_UserId_IsRead",
                schema: "profile",
                table: "notifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_photo_reviews_MediaFileId",
                schema: "profile",
                table: "photo_reviews",
                column: "MediaFileId");

            migrationBuilder.CreateIndex(
                name: "IX_photo_reviews_Status_CreatedAt",
                schema: "profile",
                table: "photo_reviews",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_prompt_catalog_Category",
                schema: "profile",
                table: "prompt_catalog",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_prompt_catalog_IsEnabled",
                schema: "profile",
                table: "prompt_catalog",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_reports_ReporterId_ReportedUserId",
                schema: "profile",
                table: "reports",
                columns: new[] { "ReporterId", "ReportedUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_reports_Status_CreatedAt",
                schema: "profile",
                table: "reports",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_StripeSubscriptionId",
                schema: "profile",
                table: "subscriptions",
                column: "StripeSubscriptionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_UserId",
                schema: "profile",
                table: "subscriptions",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_log",
                schema: "profile");

            migrationBuilder.DropTable(
                name: "blocks",
                schema: "profile");

            migrationBuilder.DropTable(
                name: "device_tokens",
                schema: "profile");

            migrationBuilder.DropTable(
                name: "icebreaker_library",
                schema: "profile");

            migrationBuilder.DropTable(
                name: "media_files",
                schema: "profile");

            migrationBuilder.DropTable(
                name: "messages",
                schema: "profile");

            migrationBuilder.DropTable(
                name: "notifications",
                schema: "profile");

            migrationBuilder.DropTable(
                name: "photo_reviews",
                schema: "profile");

            migrationBuilder.DropTable(
                name: "prompt_catalog",
                schema: "profile");

            migrationBuilder.DropTable(
                name: "reports",
                schema: "profile");

            migrationBuilder.DropTable(
                name: "subscriptions",
                schema: "profile");

            migrationBuilder.DropIndex(
                name: "IX_conversations_Status",
                schema: "profile",
                table: "conversations");

            migrationBuilder.DropColumn(
                name: "StripeCustomerId",
                schema: "profile",
                table: "users");

            migrationBuilder.DropColumn(
                name: "Tier",
                schema: "profile",
                table: "users");

            migrationBuilder.DropColumn(
                name: "BoostedAt",
                schema: "profile",
                table: "profiles");

            migrationBuilder.DropColumn(
                name: "prompts",
                schema: "profile",
                table: "profiles");

            migrationBuilder.DropColumn(
                name: "IcebreakerCategory",
                schema: "profile",
                table: "conversations");

            migrationBuilder.DropColumn(
                name: "IcebreakerQuestion",
                schema: "profile",
                table: "conversations");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "profile",
                table: "conversations");

            migrationBuilder.DropColumn(
                name: "UnmatchedAt",
                schema: "profile",
                table: "conversations");

            migrationBuilder.DropColumn(
                name: "UnmatchedByUserId",
                schema: "profile",
                table: "conversations");
        }
    }
}
