using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelBookingApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxCodeToHotel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "tax_code",
                table: "hotels",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "tax_code",
                table: "hotels");
        }
    }
}
