using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelBookingApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorDB_Policies_Staff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM hotel_staff_assignments;");
            migrationBuilder.Sql("DELETE FROM reviews;");

            migrationBuilder.DropForeignKey(
                name: "FK_users_businesses_business_id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_business_id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_hotel_staff_assignments_user_id",
                table: "hotel_staff_assignments");

            migrationBuilder.DropColumn(
                name: "business_id",
                table: "users");

            migrationBuilder.CreateTable(
                name: "business_staff",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    business_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_business_staff", x => x.id);
                    table.ForeignKey(
                        name: "FK_business_staff_businesses_business_id",
                        column: x => x.business_id,
                        principalTable: "businesses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_business_staff_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "hotel_deposit_policies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    hotel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hours_before_check_in = table.Column<int>(type: "integer", nullable: false),
                    deposit_percentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hotel_deposit_policies", x => x.id);
                    table.ForeignKey(
                        name: "FK_hotel_deposit_policies_hotels_hotel_id",
                        column: x => x.hotel_id,
                        principalTable: "hotels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_staff_assignments_user_id",
                table: "hotel_staff_assignments",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_business_staff_business_id",
                table: "business_staff",
                column: "business_id");

            migrationBuilder.CreateIndex(
                name: "IX_business_staff_user_id",
                table: "business_staff",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_deposit_policies_hotel_id",
                table: "hotel_deposit_policies",
                column: "hotel_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "business_staff");

            migrationBuilder.DropTable(
                name: "hotel_deposit_policies");

            migrationBuilder.DropIndex(
                name: "idx_staff_assignments_user_id",
                table: "hotel_staff_assignments");

            migrationBuilder.AddColumn<Guid>(
                name: "business_id",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_business_id",
                table: "users",
                column: "business_id");

            migrationBuilder.CreateIndex(
                name: "IX_hotel_staff_assignments_user_id",
                table: "hotel_staff_assignments",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_users_businesses_business_id",
                table: "users",
                column: "business_id",
                principalTable: "businesses",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
