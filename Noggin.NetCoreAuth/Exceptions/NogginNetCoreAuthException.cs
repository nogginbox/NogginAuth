using System;

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
    }
}