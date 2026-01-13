using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceTokenApi.Migrations
{
    /// <inheritdoc />
    public partial class ChangeFieldName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TokenCount",
                table: "Requests",
                newName: "ServiceTokenCount");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ServiceTokenCount",
                table: "Requests",
                newName: "TokenCount");
        }
    }
}
