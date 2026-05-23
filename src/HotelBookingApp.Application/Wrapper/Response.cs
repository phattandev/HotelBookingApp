using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelBookingApp.Application.Wrapper
{
    public class Response<T>
    {
        public bool Succeeded { get; set; }
        public string? Message { get; set; }
        public string[]? Errors { get; set; }
        public T? Data { get; set; }

        public Response() { }

        public Response(T data, string? message = null)
        {
            Succeeded = true;
            Message = message;
            Data = data;
            Errors = null;
        }

        public Response(string message)
        {
            Succeeded = false;
            Message = message;
            Errors = new[] { message };
        }

        public Response(string message, string[] errors)
        {
            Succeeded = false;
            Message = message;
            Errors = errors;
        }
    }
}
