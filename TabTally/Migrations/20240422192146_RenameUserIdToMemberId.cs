using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Splyt.Migrations
{
    /// <inheritdoc />
    public partial class RenameUserIdToMemberId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GroupMembers_User_UserId",
                table: "GroupMembers");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "GroupMembers",
                newName: "MemberId");

            migrationBuilder.RenameIndex(
                name: "IX_GroupMembers_UserId",
                table: "GroupMembers",
                newName: "IX_GroupMembers_MemberId");

            migrationBuilder.AddForeignKey(
                name: "FK_GroupMembers_User_MemberId",
                table: "GroupMembers",
                column: "MemberId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GroupMembers_User_MemberId",
                table: "GroupMembers");

            migrationBuilder.RenameColumn(
                name: "MemberId",
                table: "GroupMembers",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_GroupMembers_MemberId",
                table: "GroupMembers",
                newName: "IX_GroupMembers_UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_GroupMembers_User_UserId",
                table: "GroupMembers",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
