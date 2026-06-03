using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokeCollection.Migrations
{
    /// <inheritdoc />
    public partial class AddDuplicateCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DuplicateCount",
                table: "Cards",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DuplicateCount",
                table: "Cards");
        }
    }
}
