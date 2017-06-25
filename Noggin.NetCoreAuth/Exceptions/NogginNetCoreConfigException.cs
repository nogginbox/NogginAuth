using System;

namespace Noggin.NetCoreAuth.Exceptions
{
    [Serializable]
    public class NogginNetCoreConfigException : Exception
    {
        public NogginNetCoreConfigException()
        {
        }

        public NogginNetCoreConfigException(string message) : base(message)
        {
        }

        public NogginNetCoreConfigException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}