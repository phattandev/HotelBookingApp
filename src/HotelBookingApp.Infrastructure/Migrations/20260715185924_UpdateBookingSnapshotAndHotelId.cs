using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelBookingApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBookingSnapshotAndHotelId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_staff_assignments_user_id",
                table: "hotel_staff_assignments");

            migrationBuilder.AddColumn<decimal>(
                name: "deposit_percentage_snapshot",
                table: "bookings",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deposit_policy_id",
                table: "bookings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "hotel_id",
                table: "bookings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "num_adults",
                table: "bookings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "num_children",
                table: "bookings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "penalty_percentage_snapshot",
                table: "bookings",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "idx_staff_assignments_user_id",
                table: "hotel_staff_assignments",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_hotel_id",
                table: "bookings",
                column: "hotel_id");

            migrationBuilder.Sql("UPDATE bookings SET hotel_id = (SELECT hotel_id FROM room_types WHERE room_types.id = bookings.room_type_id);");

            migrationBuilder.AddForeignKey(
                name: "FK_bookings_hotels_hotel_id",
                table: "bookings",
                column: "hotel_id",
                principalTable: "hotels",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_bookings_hotels_hotel_id",
                table: "bookings");

            migrationBuilder.DropIndex(
                name: "idx_staff_assignments_user_id",
                table: "hotel_staff_assignments");

            migrationBuilder.DropIndex(
                name: "IX_bookings_hotel_id",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "deposit_percentage_snapshot",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "deposit_policy_id",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "hotel_id",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "num_adults",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "num_children",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "penalty_percentage_snapshot",
                table: "bookings");

            migrationBuilder.CreateIndex(
                name: "idx_staff_assignments_user_id",
                table: "hotel_staff_assignments",
                column: "user_id",
                unique: true);
        }
    }
}
