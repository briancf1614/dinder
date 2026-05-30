using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace Dinder.Infrastructure.Persistence.Migrations.Profile
{
    /// <inheritdoc />
    public partial class InitialProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "profile");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "matches",
                schema: "profile",
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
                name: "profiles",
                schema: "profile",
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
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "swipes",
                schema: "profile",
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
                schema: "profile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
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
                schema: "profile",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_conversations_matches_MatchId",
                        column: x => x.MatchId,
                        principalSchema: "profile",
                        principalTable: "matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "profile_photos",
                schema: "profile",
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
                        principalSchema: "profile",
                        principalTable: "profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "profile_preferences",
                schema: "profile",
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
                        principalSchema: "profile",
                        principalTable: "profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                schema: "profile",
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
                        principalSchema: "profile",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_users_UserId1",
                        column: x => x.UserId1,
                        principalSchema: "profile",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_external_logins",
                schema: "profile",
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
                        principalSchema: "profile",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_external_logins_users_UserId1",
                        column: x => x.UserId1,
                        principalSchema: "profile",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_conversations_MatchId",
                schema: "profile",
                table: "conversations",
                column: "MatchId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_matches_UserId1",
                schema: "profile",
                table: "matches",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_matches_UserId1_UserId2",
                schema: "profile",
                table: "matches",
                columns: new[] { "UserId1", "UserId2" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_matches_UserId2",
                schema: "profile",
                table: "matches",
                column: "UserId2");

            migrationBuilder.CreateIndex(
                name: "IX_profile_photos_ProfileId_SortOrder",
                schema: "profile",
                table: "profile_photos",
                columns: new[] { "ProfileId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_profile_preferences_ProfileId",
                schema: "profile",
                table: "profile_preferences",
                column: "ProfileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_profiles_Birthday",
                schema: "profile",
                table: "profiles",
                column: "Birthday");

            migrationBuilder.CreateIndex(
                name: "IX_profiles_Gender",
                schema: "profile",
                table: "profiles",
                column: "Gender");

            migrationBuilder.CreateIndex(
                name: "IX_profiles_IsDiscoverable",
                schema: "profile",
                table: "profiles",
                column: "IsDiscoverable");

            migrationBuilder.CreateIndex(
                name: "IX_profiles_Location",
                schema: "profile",
                table: "profiles",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_profiles_UserId",
                schema: "profile",
                table: "profiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_ExpiresAt",
                schema: "profile",
                table: "refresh_tokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_Token",
                schema: "profile",
                table: "refresh_tokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_UserId",
                schema: "profile",
                table: "refresh_tokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_UserId1",
                schema: "profile",
                table: "refresh_tokens",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_swipes_SwipedId_SwiperId_Direction",
                schema: "profile",
                table: "swipes",
                columns: new[] { "SwipedId", "SwiperId", "Direction" });

            migrationBuilder.CreateIndex(
                name: "IX_swipes_SwiperId_CreatedAt",
                schema: "profile",
                table: "swipes",
                columns: new[] { "SwiperId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_swipes_SwiperId_SwipedId",
                schema: "profile",
                table: "swipes",
                columns: new[] { "SwiperId", "SwipedId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_external_logins_Provider_ProviderUserId",
                schema: "profile",
                table: "user_external_logins",
                columns: new[] { "Provider", "ProviderUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_external_logins_UserId",
                schema: "profile",
                table: "user_external_logins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_external_logins_UserId1",
                schema: "profile",
                table: "user_external_logins",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                schema: "profile",
                table: "users",
                column: "Email",
                unique: true,
                filter: "status != 3");

            migrationBuilder.CreateIndex(
                name: "IX_users_Status",
                schema: "profile",
                table: "users",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "conversations",
                schema: "profile");

            migrationBuilder.DropTable(
                name: "profile_photos",
                schema: "profile");

            migrationBuilder.DropTable(
                name: "profile_preferences",
                schema: "profile");

            migrationBuilder.DropTable(
                name: "refresh_tokens",
                schema: "profile");

            migrationBuilder.DropTable(
                name: "swipes",
                schema: "profile");

            migrationBuilder.DropTable(
                name: "user_external_logins",
                schema: "profile");

            migrationBuilder.DropTable(
                name: "matches",
                schema: "profile");

            migrationBuilder.DropTable(
                name: "profiles",
                schema: "profile");

            migrationBuilder.DropTable(
                name: "users",
                schema: "profile");
        }
    }
}
