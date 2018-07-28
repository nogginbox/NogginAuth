using System;

namespace Noggin.NetCoreAuth.Model
{
    /// <summary>
    /// Information about why athenticating with the provider failed
    /// </summary>
    public class AuthenticationFailInformation
    {
        public AuthenticationFailInformation(Exception ex, UserInformation user = null)
        {
            Reason = ex.Message;
            User = user;
        }

        public AuthenticationFailInformation(string reason, UserInformation user = null)
        {
            Reason = reason;
            User = user;
        }

        /// <summary>
        /// If authenticating failed because an exception was thrown then this will be here to help with debugging. 
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// This will probably be blank, but if the authentication failed late in the process it may have some information about the user.
        /// </summary>
        public UserInformation User { get; }

        /// <summary>
        /// A description of why the authentication failed
        /// </summary>
        public string Reason { get; }
    }
}