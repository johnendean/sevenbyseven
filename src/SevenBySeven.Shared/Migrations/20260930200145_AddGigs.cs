using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SevenBySeven.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddGigs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "gigs");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "parted_with_on",
                schema: "collection",
                table: "copies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "gigs",
                schema: "gigs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    played_on = table.Column<DateOnly>(type: "date", nullable: false),
                    venue = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gigs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "selections",
                schema: "gigs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    gig_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_selections", x => x.id);
                    table.ForeignKey(
                        name: "fk_selections_gigs_gig_id",
                        column: x => x.gig_id,
                        principalSchema: "gigs",
                        principalTable: "gigs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "plays",
                schema: "gigs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    selection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    copy_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_plays", x => x.id);
                    table.ForeignKey(
                        name: "fk_plays_copies_copy_id",
                        column: x => x.copy_id,
                        principalSchema: "collection",
                        principalTable: "copies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_plays_selection_selection_id",
                        column: x => x.selection_id,
                        principalSchema: "gigs",
                        principalTable: "selections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_gigs_played_on",
                schema: "gigs",
                table: "gigs",
                column: "played_on");

            migrationBuilder.CreateIndex(
                name: "ix_plays_copy_id",
                schema: "gigs",
                table: "plays",
                column: "copy_id");

            migrationBuilder.CreateIndex(
                name: "ix_plays_selection_id_sequence",
                schema: "gigs",
                table: "plays",
                columns: new[] { "selection_id", "sequence" });

            migrationBuilder.CreateIndex(
                name: "ix_selections_gig_id_sequence",
                schema: "gigs",
                table: "selections",
                columns: new[] { "gig_id", "sequence" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "plays",
                schema: "gigs");

            migrationBuilder.DropTable(
                name: "selections",
                schema: "gigs");

            migrationBuilder.DropTable(
                name: "gigs",
                schema: "gigs");

            migrationBuilder.DropColumn(
                name: "parted_with_on",
                schema: "collection",
                table: "copies");
        }
    }
}
