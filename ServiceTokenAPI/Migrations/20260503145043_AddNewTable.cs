using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceTokenApi.Migrations
{
    /// <inheritdoc />
    public partial class AddNewTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServiceTokensInCart",
                columns: table => new
                {
                    ServiceTokenId = table.Column<string>(type: "text", nullable: false),
                    OwnerPublicKey = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceTokensInCart", x => x.ServiceTokenId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceTokensInCart_OwnerPublicKey",
                table: "ServiceTokensInCart",
                column: "OwnerPublicKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceTokensInCart");
        }
    }
}
