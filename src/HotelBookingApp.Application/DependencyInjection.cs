using System.Reflection;
using FluentValidation;
using HotelBookingApp.Application.Behaviours;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBookingApp.Application
{
    public static class DependencyInjection
    {
        // Hàm mở rộng (Extension method) cho IServiceCollection
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            // Chỉ đăng ký MediatR cơ bản, chưa có ValidationBehaviour
            services.AddMediatR(config =>
            {
                config.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
                config.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
            });

            services.AddAutoMapper(config => { }, Assembly.GetExecutingAssembly());

            return services;
        }
    }
}
