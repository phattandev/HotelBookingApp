using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using HotelBookingApp.Infrastructure.Persistence;

var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
optionsBuilder.UseNpgsql("Host=localhost;Database=LVTN_HotelBooking;Username=postgres;Password=123");

using var context = new ApplicationDbContext(optionsBuilder.Options);
context.Database.ExecuteSqlRaw("DELETE FROM hotel_staff_assignments");
Console.WriteLine("Deleted all records in hotel_staff_assignments");
