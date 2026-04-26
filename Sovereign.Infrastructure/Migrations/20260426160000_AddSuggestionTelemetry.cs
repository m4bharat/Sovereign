using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sovereign.Infrastructure.Migrations
{
    public partial class AddSuggestionTelemetry : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "suggestion_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SessionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    EventType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EventTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Platform = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Surface = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CurrentUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    SuggestionId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    SituationType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Move = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Strategy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Tone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Confidence = table.Column<double>(type: "double precision", nullable: true),
                    SourceAuthor = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    SourceTitle = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    SourceTextHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    InputMessageHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ReplyHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ReplyLength = table.Column<int>(type: "integer", nullable: true),
                    EditedReplyLength = table.Column<int>(type: "integer", nullable: true),
                    EditDistance = table.Column<int>(type: "integer", nullable: true),
                    EditRatio = table.Column<double>(type: "double precision", nullable: true),
                    LatencyMs = table.Column<int>(type: "integer", nullable: true),
                    ModelProvider = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ModelName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Accepted = table.Column<bool>(type: "boolean", nullable: true),
                    Posted = table.Column<bool>(type: "boolean", nullable: true),
                    Regenerated = table.Column<bool>(type: "boolean", nullable: true),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_suggestion_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "suggestion_feedback",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SuggestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: true),
                    FeedbackType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    FeedbackText = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    WasUseful = table.Column<bool>(type: "boolean", nullable: true),
                    WasGeneric = table.Column<bool>(type: "boolean", nullable: true),
                    WasWrongContext = table.Column<bool>(type: "boolean", nullable: true),
                    WasWrongTone = table.Column<bool>(type: "boolean", nullable: true),
                    WasTooLong = table.Column<bool>(type: "boolean", nullable: true),
                    WasTooShort = table.Column<bool>(type: "boolean", nullable: true),
                    Hallucinated = table.Column<bool>(type: "boolean", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_suggestion_feedback", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "suggestion_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SuggestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RequestPayloadJson = table.Column<string>(type: "jsonb", nullable: true),
                    ResponsePayloadJson = table.Column<string>(type: "jsonb", nullable: true),
                    SourceText = table.Column<string>(type: "character varying(12000)", maxLength: 12000, nullable: true),
                    InputMessage = table.Column<string>(type: "character varying(12000)", maxLength: 12000, nullable: true),
                    GeneratedReply = table.Column<string>(type: "character varying(12000)", maxLength: 12000, nullable: true),
                    EditedReply = table.Column<string>(type: "character varying(12000)", maxLength: 12000, nullable: true),
                    IsDebugSample = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_suggestion_snapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_suggestion_events_EventType",
                table: "suggestion_events",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_suggestion_events_SuggestionId",
                table: "suggestion_events",
                column: "SuggestionId");

            migrationBuilder.CreateIndex(
                name: "IX_suggestion_events_Surface_EventTime",
                table: "suggestion_events",
                columns: new[] { "Surface", "EventTime" });

            migrationBuilder.CreateIndex(
                name: "IX_suggestion_events_UserId_EventTime",
                table: "suggestion_events",
                columns: new[] { "UserId", "EventTime" });

            migrationBuilder.CreateIndex(
                name: "IX_suggestion_feedback_SuggestionId",
                table: "suggestion_feedback",
                column: "SuggestionId");

            migrationBuilder.CreateIndex(
                name: "IX_suggestion_feedback_UserId_CreatedAt",
                table: "suggestion_feedback",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_suggestion_snapshots_SuggestionId",
                table: "suggestion_snapshots",
                column: "SuggestionId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "suggestion_events");
            migrationBuilder.DropTable(name: "suggestion_feedback");
            migrationBuilder.DropTable(name: "suggestion_snapshots");
        }
    }
}
