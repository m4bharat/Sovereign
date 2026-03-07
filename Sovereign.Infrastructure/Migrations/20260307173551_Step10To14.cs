using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sovereign.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Step10To14 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "conversation_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ThreadId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Content = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversation_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "conversation_summaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ThreadId = table.Column<Guid>(type: "uuid", nullable: false),
                    SummaryText = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    GeneratedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversation_summaries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "conversation_threads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ContactId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversation_threads", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "influence_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    AggregateInfluenceScore = table.Column<double>(type: "double precision", nullable: false),
                    CapturedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_influence_snapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "relationship_decay_alerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RelationshipId = table.Column<Guid>(type: "uuid", nullable: false),
                    Message = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    TriggeredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_relationship_decay_alerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "social_edges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceUserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    TargetContactId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    StrengthScore = table.Column<double>(type: "double precision", nullable: false),
                    InfluenceScore = table.Column<double>(type: "double precision", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_social_edges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user_accounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_accounts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_conversation_messages_ThreadId_SentAtUtc",
                table: "conversation_messages",
                columns: new[] { "ThreadId", "SentAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_conversation_summaries_ThreadId_GeneratedAtUtc",
                table: "conversation_summaries",
                columns: new[] { "ThreadId", "GeneratedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_conversation_threads_UserId_ContactId",
                table: "conversation_threads",
                columns: new[] { "UserId", "ContactId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_influence_snapshots_UserId_CapturedAtUtc",
                table: "influence_snapshots",
                columns: new[] { "UserId", "CapturedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_relationship_decay_alerts_RelationshipId_TriggeredAtUtc",
                table: "relationship_decay_alerts",
                columns: new[] { "RelationshipId", "TriggeredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_social_edges_SourceUserId_TargetContactId",
                table: "social_edges",
                columns: new[] { "SourceUserId", "TargetContactId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_accounts_Email_TenantId",
                table: "user_accounts",
                columns: new[] { "Email", "TenantId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "conversation_messages");

            migrationBuilder.DropTable(
                name: "conversation_summaries");

            migrationBuilder.DropTable(
                name: "conversation_threads");

            migrationBuilder.DropTable(
                name: "influence_snapshots");

            migrationBuilder.DropTable(
                name: "relationship_decay_alerts");

            migrationBuilder.DropTable(
                name: "social_edges");

            migrationBuilder.DropTable(
                name: "user_accounts");
        }
    }
}
