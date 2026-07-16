using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelBookingApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCancellationPolicyAndModelFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "hotels",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "rejection_reason",
                table: "hotels",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "hotels",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "businesses",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "rejection_reason",
                table: "businesses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "businesses",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "cancellation_policy_id",
                table: "bookings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "cancelled_at",
                table: "bookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "penalty_amount",
                table: "bookings",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "refund_amount",
                table: "bookings",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "hotel_cancellation_policies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    hotel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    policy_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    hours_before_check_in = table.Column<int>(type: "integer", nullable: false),
                    penalty_percentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hotel_cancellation_policies", x => x.id);
                    table.CheckConstraint("chk_policy_hours_before", "\"hours_before_check_in\" > 0");
                    table.CheckConstraint("chk_policy_penalty_percentage", "\"penalty_percentage\" >= 0 AND \"penalty_percentage\" <= 100");
                    table.ForeignKey(
                        name: "FK_hotel_cancellation_policies_hotels_hotel_id",
                        column: x => x.hotel_id,
                        principalTable: "hotels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "chk_roomtype_baseprice",
                table: "room_types",
                sql: "\"base_price\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "chk_roomtype_totalrooms",
                table: "room_types",
                sql: "\"total_rooms\" >= 1");

            migrationBuilder.CreateIndex(
                name: "idx_businesses_tax_code",
                table: "businesses",
                column: "tax_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bookings_cancellation_policy_id",
                table: "bookings",
                column: "cancellation_policy_id");

            migrationBuilder.CreateIndex(
                name: "idx_cancellation_policies_hotel_id",
                table: "hotel_cancellation_policies",
                column: "hotel_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_bookings_hotel_cancellation_policies_cancellation_policy_id",
                table: "bookings",
                column: "cancellation_policy_id",
                principalTable: "hotel_cancellation_policies",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_bookings_hotel_cancellation_policies_cancellation_policy_id",
                table: "bookings");

            migrationBuilder.DropTable(
                name: "hotel_cancellation_policies");

            migrationBuilder.DropCheckConstraint(
                name: "chk_roomtype_baseprice",
                table: "room_types");

            migrationBuilder.DropCheckConstraint(
                name: "chk_roomtype_totalrooms",
                table: "room_types");

            migrationBuilder.DropIndex(
                name: "idx_businesses_tax_code",
                table: "businesses");

            migrationBuilder.DropIndex(
                name: "IX_bookings_cancellation_policy_id",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "hotels");

            migrationBuilder.DropColumn(
                name: "rejection_reason",
                table: "hotels");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "hotels");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "businesses");

            migrationBuilder.DropColumn(
                name: "rejection_reason",
                table: "businesses");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "businesses");

            migrationBuilder.DropColumn(
                name: "cancellation_policy_id",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "cancelled_at",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "penalty_amount",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "refund_amount",
                table: "bookings");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
        }
    }
}
