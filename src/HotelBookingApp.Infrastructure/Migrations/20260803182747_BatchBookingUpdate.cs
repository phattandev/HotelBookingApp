using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelBookingApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BatchBookingUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "booking_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    num_rooms = table.Column<int>(type: "integer", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    sub_total = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_booking_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_booking_items_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_booking_items_room_types_room_type_id",
                        column: x => x.room_type_id,
                        principalTable: "room_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_booking_items_booking_id",
                table: "booking_items",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_items_room_type_id",
                table: "booking_items",
                column: "room_type_id");

            // Data Migration: copy existing booking details to booking_items table
            migrationBuilder.Sql(
                @"INSERT INTO booking_items (id, booking_id, room_type_id, num_rooms, unit_price, sub_total)
                  SELECT gen_random_uuid(), id, room_type_id, num_rooms, total_price, total_price
                  FROM bookings;");

            // Now safely drop old constraints and columns
            migrationBuilder.DropForeignKey(
                name: "FK_bookings_room_types_room_type_id",
                table: "bookings");

            migrationBuilder.DropIndex(
                name: "IX_bookings_room_type_id",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "num_rooms",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "room_type_id",
                table: "bookings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "booking_items");

            migrationBuilder.AddColumn<int>(
                name: "num_rooms",
                table: "bookings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "room_type_id",
                table: "bookings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_bookings_room_type_id",
                table: "bookings",
                column: "room_type_id");

            migrationBuilder.AddForeignKey(
                name: "FK_bookings_room_types_room_type_id",
                table: "bookings",
                column: "room_type_id",
                principalTable: "room_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
