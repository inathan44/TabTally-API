using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Splyt.Migrations
{
    /// <inheritdoc />
    public partial class RemovePayerFromDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TransactionDetail_User_PayerId",
                table: "TransactionDetail");

            migrationBuilder.DropIndex(
                name: "IX_TransactionDetail_PayerId",
                table: "TransactionDetail");

            migrationBuilder.DropColumn(
                name: "PayerId",
                table: "TransactionDetail");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PayerId",
                table: "TransactionDetail",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransactionDetail_PayerId",
                table: "TransactionDetail",
                column: "PayerId");

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionDetail_User_PayerId",
                table: "TransactionDetail",
                column: "PayerId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
