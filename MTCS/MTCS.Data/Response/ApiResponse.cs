namespace MTCS.Data.Response
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
        public string? MessageVN { get; set; }
        public string? Errors { get; set; }

        public ApiResponse(bool success, T? data, string message, string? messageVN, string? errors)
        {
            Success = success;
            Data = data;
            Message = message;
            MessageVN = messageVN;
            Errors = errors;
        }
    }
}
