using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ServiceTokenApi.Migrations
{
    /// <inheritdoc />
    public partial class FixData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    MerchantPaymentId = table.Column<string>(type: "text", nullable: false),
                    PayId = table.Column<string>(type: "text", nullable: true),
                    ServiceTokenId = table.Column<string>(type: "text", nullable: false),
                    InvestorPublicKey = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<byte>(type: "smallint", nullable: false),
                    TbcStatus = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_InvestorPublicKey",
                table: "Payments",
                column: "InvestorPublicKey");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_MerchantPaymentId",
                table: "Payments",
                column: "MerchantPaymentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PayId",
                table: "Payments",
                column: "PayId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ServiceTokenId",
                table: "Payments",
                column: "ServiceTokenId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Payments");
        }
    }
}
