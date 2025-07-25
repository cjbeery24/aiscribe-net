using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SermonTranscription.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTranscriptionHoursToMinutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TranscriptionHoursUsed",
                table: "Subscriptions",
                newName: "TranscriptionMinutesUsed");

            migrationBuilder.RenameColumn(
                name: "MaxTranscriptionHours",
                table: "Subscriptions",
                newName: "MaxTranscriptionMinutes");

            migrationBuilder.RenameColumn(
                name: "MaxTranscriptionHours",
                table: "Organizations",
                newName: "MaxTranscriptionMinutes");

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "MaxTranscriptionMinutes",
                value: 600);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TranscriptionMinutesUsed",
                table: "Subscriptions",
                newName: "TranscriptionHoursUsed");

            migrationBuilder.RenameColumn(
                name: "MaxTranscriptionMinutes",
                table: "Subscriptions",
                newName: "MaxTranscriptionHours");

            migrationBuilder.RenameColumn(
                name: "MaxTranscriptionMinutes",
                table: "Organizations",
                newName: "MaxTranscriptionHours");

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "MaxTranscriptionHours",
                value: 10);
        }
    }
}
