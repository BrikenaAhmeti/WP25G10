using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WP25G10.Data.Migrations
{
    public partial class AddCheckInDeskAndDelayToFlight : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Airlines_AspNetUsers_CreatedByUserId",
                table: "Airlines");

            migrationBuilder.DropIndex(
                name: "IX_Flights_GateId",
                table: "Flights");

            migrationBuilder.RenameColumn(
                name: "CheckInDeskTo",
                table: "Flights",
                newName: "DelayMinutes");

            migrationBuilder.RenameColumn(
                name: "CheckInDeskFrom",
                table: "Flights",
                newName: "CheckInDeskId");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedByUserId",
                table: "Airlines",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_Flights_ArrivalTime",
                table: "Flights",
                column: "ArrivalTime");

            migrationBuilder.CreateIndex(
                name: "IX_Flights_CheckInDeskId",
                table: "Flights",
                column: "CheckInDeskId");

            migrationBuilder.CreateIndex(
                name: "IX_Flights_DepartureTime",
                table: "Flights",
                column: "DepartureTime");

            migrationBuilder.CreateIndex(
                name: "IX_Flights_FlightNumber",
                table: "Flights",
                column: "FlightNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Flights_GateId_DepartureTime",
                table: "Flights",
                columns: new[] { "GateId", "DepartureTime" });

            migrationBuilder.AddForeignKey(
                name: "FK_Airlines_AspNetUsers_CreatedByUserId",
                table: "Airlines",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Flights_CheckInDesks_CheckInDeskId",
                table: "Flights",
                column: "CheckInDeskId",
                principalTable: "CheckInDesks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Airlines_AspNetUsers_CreatedByUserId",
                table: "Airlines");

            migrationBuilder.DropForeignKey(
                name: "FK_Flights_CheckInDesks_CheckInDeskId",
                table: "Flights");

            migrationBuilder.DropIndex(
                name: "IX_Flights_ArrivalTime",
                table: "Flights");

            migrationBuilder.DropIndex(
                name: "IX_Flights_CheckInDeskId",
                table: "Flights");

            migrationBuilder.DropIndex(
                name: "IX_Flights_DepartureTime",
                table: "Flights");

            migrationBuilder.DropIndex(
                name: "IX_Flights_FlightNumber",
                table: "Flights");

            migrationBuilder.DropIndex(
                name: "IX_Flights_GateId_DepartureTime",
                table: "Flights");

            migrationBuilder.RenameColumn(
                name: "DelayMinutes",
                table: "Flights",
                newName: "CheckInDeskTo");

            migrationBuilder.RenameColumn(
                name: "CheckInDeskId",
                table: "Flights",
                newName: "CheckInDeskFrom");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedByUserId",
                table: "Airlines",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Flights_GateId",
                table: "Flights",
                column: "GateId");

            migrationBuilder.AddForeignKey(
                name: "FK_Airlines_AspNetUsers_CreatedByUserId",
                table: "Airlines",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
