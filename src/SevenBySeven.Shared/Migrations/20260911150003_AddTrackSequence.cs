using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SevenBySeven.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddTrackSequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "sequence",
                schema: "catalogue",
                table: "tracks",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "sequence",
                schema: "catalogue",
                table: "tracks");
        }
    }
}
