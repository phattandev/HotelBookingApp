using System.Security.Claims;
using System.Text;
using Hangfire;
using Hangfire.PostgreSql;
using HotelBookingApp.Application;
using HotelBookingApp.Application.Middlewares;
using HotelBookingApp.Infrastructure;
using HotelBookingApp.Infrastructure.Jobs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using HotelBookingApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowReactApp", policy =>
                {
                    policy.WithOrigins("http://localhost:5173")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            services.AddInfrastructureServices(Configuration);

            services.AddApplicationServices();

            services.AddControllers()
                    .ConfigureApiBehaviorOptions(options =>
                    {
                        options.SuppressModelStateInvalidFilter = true;
                    });


            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Description = "Enter JWT Token here",
                    Name = "Authorization",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT"
                });

                options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
                    {
                        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                        {
                            Reference = new Microsoft.OpenApi.Models.OpenApiReference
                            {
                                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });
            });

            // Authentication với JWT Bearer
            IConfigurationSection? jwtSettings = Configuration.GetSection("JwtSettings");
            string secretKeyString = jwtSettings["SecretKey"] 
                ?? throw new InvalidOperationException("JwtSettings:SecretKey chưa được cấu hình trong appsettings.json.");
            byte[]? secretKey = Encoding.UTF8.GetBytes(secretKeyString);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(secretKey),
                    ClockSkew = TimeSpan.Zero,

                    RoleClaimType = ClaimTypes.Role
                };
            });

            // Hangfire: Background Job Processing
            var connectionString = Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection chưa được cấu hình.");

            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(opts => opts.UseNpgsqlConnection(connectionString)));

            services.AddHangfireServer(opts =>
            {
                opts.WorkerCount = 2;       // Số worker (2 là đủ cho dự án)
                opts.Queues = new[] { "default" };
            });

            // Đăng ký Job class vào DI
            services.AddScoped<BookingCompletionJob>();
            services.AddScoped<BookingDepositJob>();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseMiddleware<ErrorHandlerMiddleware>();

            if (env.IsDevelopment())
            {
                //app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "HotelBookingApp.API v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseCors("AllowReactApp"); //Cho phép FE truy cập

            app.UseAuthentication();

            app.UseAuthorization();

            // Hangfire Dashboard — chỉ admin được xem (production nên thêm Authorization filter)
            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                // TODO: Thêm DashboardAuthorizationFilter cho production
                Authorization = Array.Empty<Hangfire.Dashboard.IDashboardAuthorizationFilter>()
            });

            // Recurring Job: tự động hoàn thành booking sau 12:00 trưa giờ VN (chạy mỗi giờ)
            RecurringJob.AddOrUpdate<BookingCompletionJob>(
                "auto-complete-bookings",
                job => job.ExecuteAsync(),
                "0 * * * *");    // Cron: Mỗi giờ

            // Recurring Job: tự động hủy đơn quá 12h chưa cọc (chạy mỗi 30 phút)
            RecurringJob.AddOrUpdate<BookingDepositJob>(
                "auto-cancel-unpaid-bookings",
                job => job.AutoCancelUnpaidBookingsAsync(),
                "*/30 * * * *");  // Cron: mỗi 30 phút

            // Recurring Job: nhắc nhở khách chưa cọc (chạy mỗi 60 phút)
            RecurringJob.AddOrUpdate<BookingDepositJob>(
                "send-deposit-reminders",
                job => job.SendDepositReminderEmailsAsync(),
                "0 * * * *");     // Cron: đầu mỗi giờ

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHangfireDashboard();
            });

            // Warmup Database & Services để tránh request đầu tiên bị chậm
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    // 1. Warmup EF Core (Database connection & Model compilation)
                    var dbContext = services.GetRequiredService<ApplicationDbContext>();
                    dbContext.Database.CanConnect();
                    var _ = dbContext.Users.Include(u => u.Role).FirstOrDefault();

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warmup failed: {ex.Message}");
                }
            }
        }
    }
}