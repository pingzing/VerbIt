using System.Runtime.Serialization;

namespace VerbIt.Backend
{
    /// <summary>
    /// An exception that, when thrown, and handled by the backend's base error handler,
    /// will immediately return the given <see cref="StatusCode"/> and message.
    /// </summary>
    public class StatusCodeException : Exception
    {
        public int StatusCode { get; set; }

        public StatusCodeException(int httpStatusCode)
        {
            StatusCode = httpStatusCode;
        }

        public StatusCodeException(int statusCode, string? message) : base(message)
        {
            StatusCode = statusCode;
        }

        public StatusCodeException(int statusCode, string? message, Exception? innerException)
            : base(message, innerException)
        {
            StatusCode = statusCode;
        }

        protected StatusCodeException(int statusCode, SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            StatusCode = statusCode;
        }
    }
}
