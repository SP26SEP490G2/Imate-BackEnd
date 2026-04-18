namespace Imate.AI.Module.Exceptions
{
    /// <summary>
    /// Exception khi JD hoặc CV không thuộc lĩnh vực IT.
    /// </summary>
    public class UnsupportedDomainException : Exception
    {
        public UnsupportedDomainException(string message) : base(message) { }
        public UnsupportedDomainException(string message, Exception innerException) : base(message, innerException) { }
    }
}
