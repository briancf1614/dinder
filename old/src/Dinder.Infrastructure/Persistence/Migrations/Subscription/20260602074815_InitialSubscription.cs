using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace Dinder.Infrastructure.Persistence.Migrations.Subscription
{
    /// <inheritdoc />
    public partial class InitialSubscription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "subscription");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "audit_log",
                schema: "subscription",
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
                schema: "subscription",
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
                schema: "subscription",
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
                name: "matches",
                schema: "subscription",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId1 = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId2 = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_matches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "media_files",
                schema: "subscription",
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
                schema: "subscription",
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
                schema: "subscription",
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
                schema: "subscription",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MediaFileId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RejectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedByAdminId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_photo_reviews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "profiles",
                schema: "subscription",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Bio = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Gender = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Birthday = table.Column<DateOnly>(type: "date", nullable: false),
                    IsDiscoverable = table.Column<bool>(type: "boolean", nullable: false),
                    Location = table.Column<Point>(type: "geography(Point, 4326)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BoostedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "reports",
                schema: "subscription",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReporterId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportedUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
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
                schema: "subscription",
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

            migrationBuilder.CreateTable(
                name: "swipes",
                schema: "subscription",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SwiperId = table.Column<Guid>(type: "uuid", nullable: false),
                    SwipedId = table.Column<Guid>(type: "uuid", nullable: false),
                    Direction = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_swipes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "subscription",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Tier = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "Free"),
                    StripeCustomerId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Birthday = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SoftDeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BanReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "conversations",
                schema: "subscription",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    UnmatchedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UnmatchedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_conversations_matches_MatchId",
                        column: x => x.MatchId,
                        principalSchema: "subscription",
                        principalTable: "matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "profile_photos",
                schema: "subscription",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    MediaFileId = table.Column<Guid>(type: "uuid", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profile_photos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_profile_photos_profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalSchema: "subscription",
                        principalTable: "profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "profile_preferences",
                schema: "subscription",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    InterestedInGenders = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MinAge = table.Column<int>(type: "integer", nullable: false),
                    MaxAge = table.Column<int>(type: "integer", nullable: false),
                    MaxDistanceKm = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profile_preferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_profile_preferences_profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalSchema: "subscription",
                        principalTable: "profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                schema: "subscription",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReplacedByToken = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    UserId1 = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "subscription",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_users_UserId1",
                        column: x => x.UserId1,
                        principalSchema: "subscription",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_external_logins",
                schema: "subscription",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ProviderUserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    UserId1 = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_external_logins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_external_logins_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "subscription",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_external_logins_users_UserId1",
                        column: x => x.UserId1,
                        principalSchema: "subscription",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_Action_Timestamp",
                schema: "subscription",
                table: "audit_log",
                columns: new[] { "Action", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_AdminId",
                schema: "subscription",
                table: "audit_log",
                column: "AdminId");

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_TargetUserId",
                schema: "subscription",
                table: "audit_log",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_Timestamp",
                schema: "subscription",
                table: "audit_log",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_blocks_BlockedId",
                schema: "subscription",
                table: "blocks",
                column: "BlockedId");

            migrationBuilder.CreateIndex(
                name: "IX_blocks_BlockerId_BlockedId",
                schema: "subscription",
                table: "blocks",
                columns: new[] { "BlockerId", "BlockedId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_conversations_MatchId",
                schema: "subscription",
                table: "conversations",
                column: "MatchId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_conversations_Status",
                schema: "subscription",
                table: "conversations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_device_tokens_Token",
                schema: "subscription",
                table: "device_tokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_device_tokens_UserId_IsExpired",
                schema: "subscription",
                table: "device_tokens",
                columns: new[] { "UserId", "IsExpired" });

            migrationBuilder.CreateIndex(
                name: "IX_matches_UserId1",
                schema: "subscription",
                table: "matches",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_matches_UserId1_UserId2",
                schema: "subscription",
                table: "matches",
                columns: new[] { "UserId1", "UserId2" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_matches_UserId2",
                schema: "subscription",
                table: "matches",
                column: "UserId2");

            migrationBuilder.CreateIndex(
                name: "IX_media_files_BlobKey",
                schema: "subscription",
                table: "media_files",
                column: "BlobKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_media_files_OwnerId",
                schema: "subscription",
                table: "media_files",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_media_files_Status_CreatedAt",
                schema: "subscription",
                table: "media_files",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_messages_ConversationId",
                schema: "subscription",
                table: "messages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_messages_ConversationId_SentAt",
                schema: "subscription",
                table: "messages",
                columns: new[] { "ConversationId", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_messages_SenderId",
                schema: "subscription",
                table: "messages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_UserId_CreatedAt",
                schema: "subscription",
                table: "notifications",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_notifications_UserId_IsRead",
                schema: "subscription",
                table: "notifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_photo_reviews_MediaFileId",
                schema: "subscription",
                table: "photo_reviews",
                column: "MediaFileId");

            migrationBuilder.CreateIndex(
                name: "IX_photo_reviews_Status_CreatedAt",
                schema: "subscription",
                table: "photo_reviews",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_profile_photos_ProfileId_SortOrder",
                schema: "subscription",
                table: "profile_photos",
                columns: new[] { "ProfileId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_profile_preferences_ProfileId",
                schema: "subscription",
                table: "profile_preferences",
                column: "ProfileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_profiles_Birthday",
                schema: "subscription",
                table: "profiles",
                column: "Birthday");

            migrationBuilder.CreateIndex(
                name: "IX_profiles_Gender",
                schema: "subscription",
                table: "profiles",
                column: "Gender");

            migrationBuilder.CreateIndex(
                name: "IX_profiles_IsDiscoverable",
                schema: "subscription",
                table: "profiles",
                column: "IsDiscoverable");

            migrationBuilder.CreateIndex(
                name: "IX_profiles_Location",
                schema: "subscription",
                table: "profiles",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_profiles_UserId",
                schema: "subscription",
                table: "profiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_ExpiresAt",
                schema: "subscription",
                table: "refresh_tokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_Token",
                schema: "subscription",
                table: "refresh_tokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_UserId",
                schema: "subscription",
                table: "refresh_tokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_UserId1",
                schema: "subscription",
                table: "refresh_tokens",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_reports_ReporterId_ReportedUserId",
                schema: "subscription",
                table: "reports",
                columns: new[] { "ReporterId", "ReportedUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_reports_Status_CreatedAt",
                schema: "subscription",
                table: "reports",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_StripeSubscriptionId",
                schema: "subscription",
                table: "subscriptions",
                column: "StripeSubscriptionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscriptions_UserId",
                schema: "subscription",
                table: "subscriptions",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_swipes_SwipedId_SwiperId_Direction",
                schema: "subscription",
                table: "swipes",
                columns: new[] { "SwipedId", "SwiperId", "Direction" });

            migrationBuilder.CreateIndex(
                name: "IX_swipes_SwiperId_CreatedAt",
                schema: "subscription",
                table: "swipes",
                columns: new[] { "SwiperId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_swipes_SwiperId_SwipedId",
                schema: "subscription",
                table: "swipes",
                columns: new[] { "SwiperId", "SwipedId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_external_logins_Provider_ProviderUserId",
                schema: "subscription",
                table: "user_external_logins",
                columns: new[] { "Provider", "ProviderUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_external_logins_UserId",
                schema: "subscription",
                table: "user_external_logins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_external_logins_UserId1",
                schema: "subscription",
                table: "user_external_logins",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                schema: "subscription",
                table: "users",
                column: "Email",
                unique: true,
                filter: "status != 3");

            migrationBuilder.CreateIndex(
                name: "IX_users_Status",
                schema: "subscription",
                table: "users",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_log",
                schema: "subscription");

            migrationBuilder.DropTable(
                name: "blocks",
                schema: "subscription");

            migrationBuilder.DropTable(
                name: "conversations",
                schema: "subscription");

            migrationBuilder.DropTable(
                name: "device_tokens",
                schema: "subscription");

            migrationBuilder.DropTable(
                name: "media_files",
                schema: "subscription");

            migrationBuilder.DropTable(
                name: "messages",
                schema: "subscription");

            migrationBuilder.DropTable(
                name: "notifications",
                schema: "subscription");

            migrationBuilder.DropTable(
                name: "photo_reviews",
                schema: "subscription");

            migrationBuilder.DropTable(
                name: "profile_photos",
                schema: "subscription");

            migrationBuilder.DropTable(
                name: "profile_preferences",
                schema: "subscription");

            migrationBuilder.DropTable(
                name: "refresh_tokens",
                schema: "subscription");

            migrationBuilder.DropTable(
                name: "reports",
                schema: "subscription");

            migrationBuilder.DropTable(
                name: "subscriptions",
                schema: "subscription");

            migrationBuilder.DropTable(
                name: "swipes",
                schema: "subscription");

            migrationBuilder.DropTable(
                name: "user_external_logins",
                schema: "subscription");

            migrationBuilder.DropTable(
                name: "matches",
                schema: "subscription");

            migrationBuilder.DropTable(
                name: "profiles",
                schema: "subscription");

            migrationBuilder.DropTable(
                name: "users",
                schema: "subscription");
        }
    }
}
