using System.Text;
using HotelBookingApp.Application;
using HotelBookingApp.Application.Middlewares;
using HotelBookingApp.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
                options.AddPolicy("AllowReactApp",
                    builder => builder
                        .AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
            });

            services.AddInfrastructureServices(Configuration);

            services.AddApplicationServices();

            services.AddControllers();


            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "HotelBookingApp.API", Version = "v1" });
            });

            // Authentication with Bearee Header
            IConfigurationSection? jwtSettings = Configuration.GetSection("JwtSettings");
            byte[]? secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] ?? "sGraw5@|K1aQFW+?fo.T*/fBI)4Jy8P60:wdRtncyO@KFme/2J&toDLz!U#/x$4kb6hIkq16Boo.wx(elXB>EySOik!^Vz%!%!L2URXr&8Ksmj*oWt&7As(b:jut9+|VUBM9OcJtfco[1Hzq;TsBY+kasYrzvu?Tm4FUcLvm9$EWW#A:Iv3fD{CE$f>uI4WKlA7zDrJJehF.f[|4CbA%k#e^v5A.[$J]vyo[wu%C=p1G[Q#%G{rrxJxCaD?c5}o}slmG1L1>&)xaRgGHUzU-)t,JtLzx?eMo=eqptS&{@OkQ=Z)PSorxKzaP=@I:w<0=U*d3lC+)plY,;$<pss)uvE1>jb8m?!$czGc]52sC,C{tmmRgd@)bQqybG&%GY).[e}8kGWk5-@86GA[WOy|7KmA}%Udbcv.X5)_3.-7xiq6,{=,4WVCrc#-:[8:/2&)Y;inTJDuqjgy@UNRN5/1zh;rA{$JGVPvOG7E<{nb*Gl%w,2K)ws7;Rp00:lNd-xC[");

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

                // Cấu hình đăng ký cho SignalR
                //options.Events = new JwtBearerEvents
                //{
                //    OnMessageReceived = context =>
                //    {
                //        string? accessToken = context.Request.Query["access_token"];
                //        PathString path = context.HttpContext.Request.Path;

                //        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/monitor"))
                //        {
                //            context.Token = accessToken;
                //        }
                //        return Task.CompletedTask;
                //    }
                //};
            });

            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder.SetIsOriginAllowed(_ => true)
                           .AllowAnyMethod()
                           .AllowAnyHeader()
                           .AllowCredentials();
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "HotelBookingApp.API v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseCors("AllowReactApp"); //Cho phép FE truy cập

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseMiddleware<ErrorHandlerMiddleware>();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}