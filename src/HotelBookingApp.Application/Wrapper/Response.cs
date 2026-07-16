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

    public class PaginatedResponse<T>
    {
        public bool Succeeded { get; set; } = true;
        public IEnumerable<T> Data { get; set; } = Enumerable.Empty<T>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

        public PaginatedResponse() { }

        public PaginatedResponse(IEnumerable<T> data, int totalCount, int page, int pageSize)
        {
            Data = data;
            TotalCount = totalCount;
            Page = page;
            PageSize = pageSize;
        }
    }
}
