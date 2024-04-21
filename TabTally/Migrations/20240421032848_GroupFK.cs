using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Splyt.Migrations
{
    /// <inheritdoc />
    public partial class GroupFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Group",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "Group",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateIndex(
                name: "IX_Group_CreatedBy",
                table: "Group",
                column: "CreatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Group_User_CreatedBy",
                table: "Group",
                column: "CreatedBy",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Group_User_CreatedBy",
                table: "Group");

            migrationBuilder.DropIndex(
                name: "IX_Group_CreatedBy",
                table: "Group");

            migrationBuilder.AlterColumn<int>(
                name: "CreatedBy",
                table: "Group",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.InsertData(
                table: "Group",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "Description", "Name", "UpdatedAt" },
                values: new object[] { 1, new DateTime(2024, 4, 21, 3, 17, 4, 345, DateTimeKind.Utc).AddTicks(9710), 1, null, "Whistler Wankers", new DateTime(2024, 4, 21, 3, 17, 4, 345, DateTimeKind.Utc).AddTicks(9710) });
        }
    }
}
