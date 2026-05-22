using System;

namespace ImageGeneratorApp
{
    public class ImageGeneratorException : Exception
    {
        public int StatusCode { get; }

        public ImageGeneratorException(string message) : base(message)
        {
            StatusCode = 0;
        }

        public ImageGeneratorException(string message, int statusCode) : base(message)
        {
            StatusCode = statusCode;
        }
    }
}