using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Assesment.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialSetup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExceptionJournal",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExceptionType = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    StackTrace = table.Column<string>(type: "text", nullable: false),
                    QueryParameters = table.Column<string>(type: "text", nullable: false),
                    BodyParameters = table.Column<string>(type: "text", nullable: false),
                    Endpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExceptionJournal", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TreeNodes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TreeName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ParentId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreeNodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TreeNodes_TreeNodes_ParentId",
                        column: x => x.ParentId,
                        principalTable: "TreeNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExceptionJournal_CreatedAt",
                table: "ExceptionJournal",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ExceptionJournal_EventId",
                table: "ExceptionJournal",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_TreeNodes_ParentId",
                table: "TreeNodes",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_TreeNodes_TreeName",
                table: "TreeNodes",
                column: "TreeName");

            migrationBuilder.CreateIndex(
                name: "IX_TreeNodes_TreeName_ParentId_Name",
                table: "TreeNodes",
                columns: new[] { "TreeName", "ParentId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExceptionJournal");

            migrationBuilder.DropTable(
                name: "TreeNodes");
        }
    }
}
