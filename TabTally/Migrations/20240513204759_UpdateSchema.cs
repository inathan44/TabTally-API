using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Splyt.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSchema : Migration
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

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "GroupMember",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupMember_UserId",
                table: "GroupMember",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_GroupMember_User_UserId",
                table: "GroupMember",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Transaction_User_CreatedById",
                table: "Transaction",
                column: "CreatedById",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Transaction_User_PayerId",
                table: "Transaction",
                column: "PayerId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionDetail_User_PayerId",
                table: "TransactionDetail",
                column: "PayerId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionDetail_User_RecipientId",
                table: "TransactionDetail",
                column: "RecipientId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GroupMember_User_UserId",
                table: "GroupMember");

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

            migrationBuilder.DropIndex(
                name: "IX_GroupMember_UserId",
                table: "GroupMember");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "GroupMember");

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
    }
}
