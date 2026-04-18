namespace Imate.AI.Module.Exceptions
{
    /// <summary>
    /// Exception khi dữ liệu đầu vào không hợp lệ.
    /// </summary>
    public class BadRequestException : Exception
    {
        public BadRequestException(string message) : base(message) { }
        public BadRequestException(string message, Exception innerException) : base(message, innerException) { }
    }
}
