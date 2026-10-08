using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    normalized_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    last_login_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    family_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    revoked_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoked_by_ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    revocation_reason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    replaced_by_token_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.id);
                    table.CheckConstraint("ck_refresh_tokens_expiration", "expires_at_utc > created_at_utc");
                    table.CheckConstraint("ck_refresh_tokens_hash_length", "octet_length(token_hash) = 32");
                    table.ForeignKey(
                        name: "FK_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "text_snippets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    content = table.Column<string>(type: "text", nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    max_views = table.Column<int>(type: "integer", nullable: true),
                    view_count = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    last_viewed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_text_snippets", x => x.id);
                    table.CheckConstraint("ck_text_snippets_content_length", "octet_length(content) <= 1048576");
                    table.CheckConstraint("ck_text_snippets_expiration", "expires_at_utc IS NULL OR expires_at_utc > created_at_utc");
                    table.CheckConstraint("ck_text_snippets_max_views", "max_views IS NULL OR max_views > 0");
                    table.CheckConstraint("ck_text_snippets_view_count", "view_count >= 0");
                    table.CheckConstraint("ck_text_snippets_view_limit", "max_views IS NULL OR view_count <= max_views");
                    table.ForeignKey(
                        name: "FK_text_snippets_users_owner_user_id",
                        column: x => x.owner_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "share_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    text_snippet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    token_key_version = table.Column<short>(type: "smallint", nullable: false),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    max_uses = table.Column<int>(type: "integer", nullable: true),
                    use_count = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    revoked_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_accessed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_share_links", x => x.id);
                    table.CheckConstraint("ck_share_links_expiration", "expires_at_utc IS NULL OR expires_at_utc > created_at_utc");
                    table.CheckConstraint("ck_share_links_key_version", "token_key_version > 0");
                    table.CheckConstraint("ck_share_links_max_uses", "max_uses IS NULL OR max_uses > 0");
                    table.CheckConstraint("ck_share_links_token_hash_length", "octet_length(token_hash) = 32");
                    table.CheckConstraint("ck_share_links_use_count", "use_count >= 0");
                    table.CheckConstraint("ck_share_links_use_limit", "max_uses IS NULL OR use_count <= max_uses");
                    table.ForeignKey(
                        name: "FK_share_links_text_snippets_text_snippet_id",
                        column: x => x.text_snippet_id,
                        principalTable: "text_snippets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "share_access_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    share_link_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    accessed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    was_successful = table.Column<bool>(type: "boolean", nullable: false),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_share_access_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_share_access_logs_share_links_share_link_id",
                        column: x => x.share_link_id,
                        principalTable: "share_links",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_share_access_logs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_expires_at",
                table: "refresh_tokens",
                column: "expires_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_family_id",
                table: "refresh_tokens",
                column: "family_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_user_revoked",
                table: "refresh_tokens",
                columns: new[] { "user_id", "revoked_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_refresh_tokens_token_hash",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_share_access_logs_accessed_at",
                table: "share_access_logs",
                column: "accessed_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_share_access_logs_link_time",
                table: "share_access_logs",
                columns: new[] { "share_link_id", "accessed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_share_access_logs_user_id",
                table: "share_access_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_share_links_expires_at",
                table: "share_links",
                column: "expires_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_share_links_text_snippet_id",
                table: "share_links",
                column: "text_snippet_id");

            migrationBuilder.CreateIndex(
                name: "ux_share_links_token",
                table: "share_links",
                columns: new[] { "token_key_version", "token_hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_text_snippets_expires_at",
                table: "text_snippets",
                column: "expires_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_text_snippets_owner_created",
                table: "text_snippets",
                columns: new[] { "owner_user_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_users_normalized_email",
                table: "users",
                column: "normalized_email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "share_access_logs");

            migrationBuilder.DropTable(
                name: "share_links");

            migrationBuilder.DropTable(
                name: "text_snippets");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
