using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Splyt.Migrations
{
    /// <inheritdoc />
    public partial class AddInvitedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GroupMembers_User_UserId",
                table: "GroupMembers");

            migrationBuilder.AddColumn<string>(
                name: "InvitedById",
                table: "GroupMembers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_InvitedById",
                table: "GroupMembers",
                column: "InvitedById");

            migrationBuilder.AddForeignKey(
                name: "FK_GroupMembers_User_InvitedById",
                table: "GroupMembers",
                column: "InvitedById",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GroupMembers_User_UserId",
                table: "GroupMembers",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GroupMembers_User_InvitedById",
                table: "GroupMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_GroupMembers_User_UserId",
                table: "GroupMembers");

            migrationBuilder.DropIndex(
                name: "IX_GroupMembers_InvitedById",
                table: "GroupMembers");

            migrationBuilder.DropColumn(
                name: "InvitedById",
                table: "GroupMembers");

            migrationBuilder.AddForeignKey(
                name: "FK_GroupMembers_User_UserId",
                table: "GroupMembers",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
