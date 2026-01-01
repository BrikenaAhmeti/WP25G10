using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WP25G10.Data.Migrations
{
    public partial class AddCheckInDeskFieldsToFlights : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Flights_GateId_DepartureTime",
                table: "Flights");

            migrationBuilder.AlterColumn<int>(
                name: "DelayMinutes",
                table: "Flights",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CheckInDeskFrom",
                table: "Flights",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CheckInDeskId1",
                table: "Flights",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CheckInDeskTo",
                table: "Flights",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Flights_CheckInDeskId1",
                table: "Flights",
                column: "CheckInDeskId1");

            migrationBuilder.CreateIndex(
                name: "IX_Flights_GateId",
                table: "Flights",
                column: "GateId");

            migrationBuilder.AddForeignKey(
                name: "FK_Flights_CheckInDesks_CheckInDeskId1",
                table: "Flights",
                column: "CheckInDeskId1",
                principalTable: "CheckInDesks",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Flights_CheckInDesks_CheckInDeskId1",
                table: "Flights");

            migrationBuilder.DropIndex(
                name: "IX_Flights_CheckInDeskId1",
                table: "Flights");

            migrationBuilder.DropIndex(
                name: "IX_Flights_GateId",
                table: "Flights");

            migrationBuilder.DropColumn(
                name: "CheckInDeskFrom",
                table: "Flights");

            migrationBuilder.DropColumn(
                name: "CheckInDeskId1",
                table: "Flights");

            migrationBuilder.DropColumn(
                name: "CheckInDeskTo",
                table: "Flights");

            migrationBuilder.AlterColumn<int>(
                name: "DelayMinutes",
                table: "Flights",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateIndex(
                name: "IX_Flights_GateId_DepartureTime",
                table: "Flights",
                columns: new[] { "GateId", "DepartureTime" });
        }
    }
}
