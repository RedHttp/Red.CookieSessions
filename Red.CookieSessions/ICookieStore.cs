using System;
using System.Threading.Tasks;

namespace Red.CookieSessions
{
    public interface ICookieStore<TCookieSession>
    {
        /// <summary>
        /// For attempting to get the session for the given token
        /// </summary>
        /// <param name="token">The token from the cookie</param>
        /// <returns>Tuple containing bool indicating if session was found, and the session (null if not found)</returns>
        Task<Tuple<bool, TCookieSession>> TryGet(string token);
        
        /// <summary>
        /// For attempting to remove a session from the session store
        /// </summary>
        /// <param name="token">The token to remove the session for</param>
        /// <returns>Whether the session was removed successfully</returns>
        Task<bool> TryRemove(string token);
        
        /// <summary>
        /// Insert, or update, the session for a given token
        /// </summary>
        /// <param name="token">Token to set the session for</param>
        /// <param name="session">The session object</param>
        Task Set(string token, TCookieSession session);
        
        /// <summary>
        /// Remove all sessions that have expired
        /// </summary>
        Task RemoveExpired();
    }
}