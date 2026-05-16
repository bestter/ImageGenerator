using System;

namespace GrokImagineApp
{
    public class GrokImagineException : Exception
    {
        public int StatusCode { get; }

        public GrokImagineException(string message) : base(message)
        {
            StatusCode = 0;
        }

        public GrokImagineException(string message, int statusCode) : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
