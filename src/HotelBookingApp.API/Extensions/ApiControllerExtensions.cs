using HotelBookingApp.Application.Wrapper;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingApp.API.Extensions
{
    public static class ApiControllerExtensions
    {
        public static IActionResult OkOrBadRequest<T>(this ControllerBase controller, Response<T> response)
        {
            if (response.Succeeded)
            {
                return controller.Ok(response);
            }
            return controller.BadRequest(response);
        }

        public static IActionResult OkOrBadRequest<T>(this ControllerBase controller, PaginatedResponse<T> response)
        {
            if (response.Succeeded)
            {
                return controller.Ok(response);
            }
            return controller.BadRequest(response);
        }
    }
}
