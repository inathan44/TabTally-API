using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Splyt.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTransactionFromTransactionDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Group",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 4, 15, 23, 3, 59, 641, DateTimeKind.Utc).AddTicks(3850), new DateTime(2024, 4, 15, 23, 3, 59, 641, DateTimeKind.Utc).AddTicks(3850) });

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 4, 15, 23, 3, 59, 641, DateTimeKind.Utc).AddTicks(3760), new DateTime(2024, 4, 15, 23, 3, 59, 641, DateTimeKind.Utc).AddTicks(3760) });

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 4, 15, 23, 3, 59, 641, DateTimeKind.Utc).AddTicks(3760), new DateTime(2024, 4, 15, 23, 3, 59, 641, DateTimeKind.Utc).AddTicks(3760) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Group",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 4, 15, 22, 47, 39, 429, DateTimeKind.Utc).AddTicks(2360), new DateTime(2024, 4, 15, 22, 47, 39, 429, DateTimeKind.Utc).AddTicks(2360) });

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 4, 15, 22, 47, 39, 429, DateTimeKind.Utc).AddTicks(2260), new DateTime(2024, 4, 15, 22, 47, 39, 429, DateTimeKind.Utc).AddTicks(2260) });

            migrationBuilder.UpdateData(
                table: "User",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2024, 4, 15, 22, 47, 39, 429, DateTimeKind.Utc).AddTicks(2260), new DateTime(2024, 4, 15, 22, 47, 39, 429, DateTimeKind.Utc).AddTicks(2260) });
        }
    }
}
