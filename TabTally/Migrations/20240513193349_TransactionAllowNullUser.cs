using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Splyt.Migrations
{
    /// <inheritdoc />
    public partial class TransactionAllowNullUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transaction_User_CreatedById",
                table: "Transaction");

            migrationBuilder.DropForeignKey(
                name: "FK_Transaction_User_PayerId",
                table: "Transaction");

            migrationBuilder.DropForeignKey(
                name: "FK_TransactionDetail_User_PayerId",
                table: "TransactionDetail");

            migrationBuilder.DropForeignKey(
                name: "FK_TransactionDetail_User_RecipientId",
                table: "TransactionDetail");

            migrationBuilder.AlterColumn<string>(
                name: "RecipientId",
                table: "TransactionDetail",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "PayerId",
                table: "TransactionDetail",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "PayerId",
                table: "Transaction",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedById",
                table: "Transaction",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddForeignKey(
                name: "FK_Transaction_User_CreatedById",
                table: "Transaction",
                column: "CreatedById",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Transaction_User_PayerId",
                table: "Transaction",
                column: "PayerId",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionDetail_User_PayerId",
                table: "TransactionDetail",
                column: "PayerId",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionDetail_User_RecipientId",
                table: "TransactionDetail",
                column: "RecipientId",
                principalTable: "User",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transaction_User_CreatedById",
                table: "Transaction");

            migrationBuilder.DropForeignKey(
                name: "FK_Transaction_User_PayerId",
                table: "Transaction");

            migrationBuilder.DropForeignKey(
                name: "FK_TransactionDetail_User_PayerId",
                table: "TransactionDetail");

            migrationBuilder.DropForeignKey(
                name: "FK_TransactionDetail_User_RecipientId",
                table: "TransactionDetail");

            migrationBuilder.AlterColumn<string>(
                name: "RecipientId",
                table: "TransactionDetail",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PayerId",
                table: "TransactionDetail",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PayerId",
                table: "Transaction",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedById",
                table: "Transaction",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Transaction_User_CreatedById",
                table: "Transaction",
                column: "CreatedById",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Transaction_User_PayerId",
                table: "Transaction",
                column: "PayerId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionDetail_User_PayerId",
                table: "TransactionDetail",
                column: "PayerId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionDetail_User_RecipientId",
                table: "TransactionDetail",
                column: "RecipientId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
