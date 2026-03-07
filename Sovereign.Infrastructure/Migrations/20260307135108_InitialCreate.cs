using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sovereign.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "relationships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ContactId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Role = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ReciprocityScore = table.Column<double>(type: "double precision", nullable: false),
                    MomentumScore = table.Column<double>(type: "double precision", nullable: false),
                    PowerDifferential = table.Column<double>(type: "double precision", nullable: false),
                    EmotionalTemperature = table.Column<double>(type: "double precision", nullable: false),
                    LastInteractionAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_relationships", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_relationships_LastInteractionAtUtc",
                table: "relationships",
                column: "LastInteractionAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_relationships_UserId_ContactId",
                table: "relationships",
                columns: new[] { "UserId", "ContactId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "relationships");
        }
    }
}
