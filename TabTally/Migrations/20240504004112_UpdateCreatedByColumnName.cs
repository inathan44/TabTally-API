using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Splyt.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCreatedByColumnName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Group_User_CreatedBy",
                table: "Group");

            migrationBuilder.DropForeignKey(
                name: "FK_GroupMember_User_InvitedById",
                table: "GroupMember");

            migrationBuilder.DropForeignKey(
                name: "FK_GroupMember_User_MemberId",
                table: "GroupMember");

            migrationBuilder.DropForeignKey(
                name: "FK_Transaction_User_CreatedBy",
                table: "Transaction");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "Transaction",
                newName: "CreatedById");

            migrationBuilder.RenameIndex(
                name: "IX_Transaction_CreatedBy",
                table: "Transaction",
                newName: "IX_Transaction_CreatedById");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "Group",
                newName: "CreatedById");

            migrationBuilder.RenameIndex(
                name: "IX_Group_CreatedBy",
                table: "Group",
                newName: "IX_Group_CreatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Group_User_CreatedById",
                table: "Group",
                column: "CreatedById",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GroupMember_User_InvitedById",
                table: "GroupMember",
                column: "InvitedById",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GroupMember_User_MemberId",
                table: "GroupMember",
                column: "MemberId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Transaction_User_CreatedById",
                table: "Transaction",
                column: "CreatedById",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Group_User_CreatedById",
                table: "Group");

            migrationBuilder.DropForeignKey(
                name: "FK_GroupMember_User_InvitedById",
                table: "GroupMember");

            migrationBuilder.DropForeignKey(
                name: "FK_GroupMember_User_MemberId",
                table: "GroupMember");

            migrationBuilder.DropForeignKey(
                name: "FK_Transaction_User_CreatedById",
                table: "Transaction");

            migrationBuilder.RenameColumn(
                name: "CreatedById",
                table: "Transaction",
                newName: "CreatedBy");

            migrationBuilder.RenameIndex(
                name: "IX_Transaction_CreatedById",
                table: "Transaction",
                newName: "IX_Transaction_CreatedBy");

            migrationBuilder.RenameColumn(
                name: "CreatedById",
                table: "Group",
                newName: "CreatedBy");

            migrationBuilder.RenameIndex(
                name: "IX_Group_CreatedById",
                table: "Group",
                newName: "IX_Group_CreatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Group_User_CreatedBy",
                table: "Group",
                column: "CreatedBy",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_GroupMember_User_InvitedById",
                table: "GroupMember",
                column: "InvitedById",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GroupMember_User_MemberId",
                table: "GroupMember",
                column: "MemberId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transaction_User_CreatedBy",
                table: "Transaction",
                column: "CreatedBy",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
