using System;

namespace Noggin.NetCoreAuth.Model
{
    /// <summary>
    /// Information about why athenticating with the provider failed
    /// </summary>
    public class AuthenticationFailInformation
    {
        public AuthenticationFailInformation(Exception ex)
        {
            Reason = ex.Message;
        }

        public AuthenticationFailInformation(string reason)
        {
            Reason = reason;
        }

        /// <summary>
        /// If authenticating failed because an exception was thrown then this will be here to help with debugging. 
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// A description of why the authentication failed
        /// </summary>
        public string Reason { get; }
    }
}