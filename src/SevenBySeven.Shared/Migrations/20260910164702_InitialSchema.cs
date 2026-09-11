using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SevenBySeven.Shared.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "collection");

            migrationBuilder.EnsureSchema(
                name: "catalogue");

            migrationBuilder.CreateTable(
                name: "releases",
                schema: "catalogue",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    discogs_release_id = table.Column<int>(type: "integer", nullable: false),
                    discogs_master_id = table.Column<int>(type: "integer", nullable: true),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    artist_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    label_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    catalogue_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    released_year = table.Column<int>(type: "integer", nullable: true),
                    released_month = table.Column<int>(type: "integer", nullable: true),
                    released_day = table.Column<int>(type: "integer", nullable: true),
                    format_description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    barcode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    genres = table.Column<List<string>>(type: "text[]", nullable: false),
                    styles = table.Column<List<string>>(type: "text[]", nullable: false),
                    thumbnail_url = table.Column<string>(type: "text", nullable: true),
                    cover_image_url = table.Column<string>(type: "text", nullable: true),
                    cached_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_releases", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "copies",
                schema: "collection",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    release_id = table.Column<Guid>(type: "uuid", nullable: false),
                    added_on = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    media_condition = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    sleeve_condition = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    price_paid = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    price_paid_currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: true),
                    purchased_from = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_copies", x => x.id);
                    table.ForeignKey(
                        name: "fk_copies_releases_release_id",
                        column: x => x.release_id,
                        principalSchema: "catalogue",
                        principalTable: "releases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tracks",
                schema: "catalogue",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    release_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    duration = table.Column<TimeSpan>(type: "interval", nullable: true),
                    bpm = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    bpm_source = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tracks", x => x.id);
                    table.ForeignKey(
                        name: "fk_tracks_releases_release_id",
                        column: x => x.release_id,
                        principalSchema: "catalogue",
                        principalTable: "releases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_copies_release_id",
                schema: "collection",
                table: "copies",
                column: "release_id");

            migrationBuilder.CreateIndex(
                name: "ix_releases_barcode",
                schema: "catalogue",
                table: "releases",
                column: "barcode");

            migrationBuilder.CreateIndex(
                name: "ix_releases_catalogue_number",
                schema: "catalogue",
                table: "releases",
                column: "catalogue_number");

            migrationBuilder.CreateIndex(
                name: "ix_releases_discogs_release_id",
                schema: "catalogue",
                table: "releases",
                column: "discogs_release_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tracks_bpm",
                schema: "catalogue",
                table: "tracks",
                column: "bpm");

            migrationBuilder.CreateIndex(
                name: "ix_tracks_release_id_position",
                schema: "catalogue",
                table: "tracks",
                columns: new[] { "release_id", "position" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "copies",
                schema: "collection");

            migrationBuilder.DropTable(
                name: "tracks",
                schema: "catalogue");

            migrationBuilder.DropTable(
                name: "releases",
                schema: "catalogue");
        }
    }
}
