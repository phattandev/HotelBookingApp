using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBookingApp.Application
{
    public static class DependencyInjection
    {
        // Hàm mở rộng (Extension method) cho IServiceCollection
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Chỉ đăng ký MediatR cơ bản, chưa có ValidationBehaviour
            services.AddMediatR(config =>
            {
                config.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            });

            services.AddAutoMapper(config => { }, Assembly.GetExecutingAssembly());

            return services;
        }
    }
}
