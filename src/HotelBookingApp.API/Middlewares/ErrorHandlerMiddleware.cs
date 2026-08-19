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
                    case HotelBookingApp.Application.Common.Exceptions.ValidationException e:
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                        responseModel.Message = "Dữ liệu đầu vào không hợp lệ.";
                        responseModel.Errors = e.Errors.SelectMany(x => x.Value).ToArray();
                        break;

                    case FluentValidation.ValidationException e:
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                        responseModel.Message = "Dữ liệu đầu vào không hợp lệ.";
                        responseModel.Errors = e.Errors.Select(x => x.ErrorMessage).ToArray();
                        break;

                    case KeyNotFoundException e:
                        response.StatusCode = (int)HttpStatusCode.NotFound;
                        responseModel.Message = e.Message;
                        responseModel.Errors = new[] { e.Message };
                        break;

                    case ApiException e:
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                        responseModel.Message = e.Message;
                        responseModel.Errors = new[] { e.Message };
                        break;

                    case UnauthorizedAccessException e:
                        response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        responseModel.Message = "Phiên làm việc hết hạn hoặc bạn không có quyền truy cập.";
                        responseModel.Errors = new[] { e.Message };
                        break;

                    default:
                        response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        responseModel.Message = "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.";
                        break;
                }

                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                string result = JsonSerializer.Serialize(responseModel, options);
                await response.WriteAsync(result);
            }
        }
    }
}
