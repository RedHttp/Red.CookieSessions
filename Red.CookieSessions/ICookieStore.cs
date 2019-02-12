using System;
using System.Threading.Tasks;

namespace Red.CookieSessions
{
    public interface ICookieStore<TCookieSession>
    {
        /// <summary>
        /// For attempting to get the session for the given token
        /// </summary>
        /// <param name="id">The session id from the cookie</param>
        /// <returns>Tuple containing bool indicating if session was found, and the session (null if not found)</returns>
        Task<Tuple<bool, TCookieSession>> TryGet(string id);
        
        /// <summary>
        /// For attempting to remove a session from the session store
        /// </summary>
        /// <param name="sessionId">The id of the session to remove</param>
        /// <returns>Whether the session was removed successfully</returns>
        Task<bool> TryRemove(string sessionId);
        
        /// <summary>
        /// Insert, or update, the session for a given token
        /// </summary>
        /// <param name="id">Token to set the session for</param>
        /// <param name="session">The session object</param>
        Task Set(TCookieSession session);
        
        /// <summary>
        /// Remove all sessions that have expired
        /// </summary>
        Task RemoveExpired();
    }
}