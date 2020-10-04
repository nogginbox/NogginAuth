using System;
using System.Runtime.Serialization;

namespace Noggin.NetCoreAuth.Exceptions
{
    [Serializable]
    public class NogginNetCoreAuthException : Exception
    {
        public NogginNetCoreAuthException()
        {
        }

        public NogginNetCoreAuthException(string message) : base(message)
        {
        }

        public NogginNetCoreAuthException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected NogginNetCoreAuthException(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext)
        {
        }
    }
}