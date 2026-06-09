using System.Net;
using System.Text.Json;
using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Wrapper;

namespace HotelBookingApp.Application.Middlewares
{
    public class ErrorHandlerMiddleware
    {
        private readonly RequestDelegate _next;

        public ErrorHandlerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception error)
            {
                HttpResponse? response = context.Response;
                response.ContentType = "application/json";

                Response<string>? responseModel = new Response<string>() { Succeeded = false, Message = error?.Message };

                switch (error)
                {
                    case ValidationException e:
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                        responseModel.Message = "Dữ liệu đầu vào không hợp lệ.";
                        responseModel.Errors = e.Errors.SelectMany(x => x.Value).ToArray();
                        break;

                    case KeyNotFoundException e:
                        response.StatusCode = (int)HttpStatusCode.NotFound;
                        break;

                    case ApiException e:
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                        break;

                    case UnauthorizedAccessException e:
                        response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        break;

                    default:
                        response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        responseModel.Message = $"Internal Server Error: {error.Message} | Stack: {error.InnerException?.Message}";
                        break;
                }

                string result = JsonSerializer.Serialize(responseModel);
                await response.WriteAsync(result);
            }
        }
    }
}
