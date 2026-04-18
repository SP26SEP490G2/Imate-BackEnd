namespace Imate.AI.Module.Exceptions
{
    /// <summary>
    /// Exception khi không tìm thấy tài nguyên (session, response, v.v.).
    /// </summary>
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }
        public NotFoundException(string message, Exception innerException) : base(message, innerException) { }
    }
}
