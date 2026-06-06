using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceTokenApi.Migrations
{
    /// <inheritdoc />
    public partial class AllowSharedMerchantPaymentId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // One Flitt order (one order_id) now covers several per-token Payment rows, so the
            // MerchantPaymentId can repeat. Replace the unique index with a non-unique one.
            migrationBuilder.DropIndex(
                name: "IX_Payments_MerchantPaymentId",
                table: "Payments");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_MerchantPaymentId",
                table: "Payments",
                column: "MerchantPaymentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_MerchantPaymentId",
                table: "Payments");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_MerchantPaymentId",
                table: "Payments",
                column: "MerchantPaymentId",
                unique: true);
        }
    }
}
