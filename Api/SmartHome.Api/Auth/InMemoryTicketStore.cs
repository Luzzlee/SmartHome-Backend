using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Memory;

namespace SmartHome.Api.Auth {

    /// <summary>
    /// Server-side session store for cookie authentication tickets, backed by an in-memory cache.
    /// Without this, ASP.NET Core's cookie auth is fully self-contained/stateless: <c>SignOutAsync</c>
    /// only tells the browser (via a Set-Cookie clear header) to forget the cookie, it can't revoke a
    /// ticket that's already been issued - so a copied/replayed cookie value stays valid until its own
    /// expiry regardless of logout. Wiring this in as <see cref="CookieAuthenticationOptions.SessionStore"/>
    /// makes sign-in store the real ticket here (keyed by a server-generated session id, with only that
    /// id round-tripping in the actual cookie) and makes sign-out/the per-request ticket lookup go through
    /// <see cref="RemoveAsync"/>/<see cref="RetrieveAsync"/> here instead of trusting the cookie's own
    /// (still cryptographically valid) contents - so a removed session is rejected immediately.
    /// In-memory only, no persistence across restarts - a restart already invalidates every session
    /// anyway on this single-user system (see repo CLAUDE.md), so that's fine.
    /// </summary>
    public class InMemoryTicketStore : ITicketStore {
        private const string KeyPrefix = "SmartHome.Auth.Session-";
        private readonly IMemoryCache _cache;

        public InMemoryTicketStore(IMemoryCache cache) {
            _cache = cache;
        }

        public Task<string> StoreAsync(AuthenticationTicket ticket) {
            var key = Guid.NewGuid().ToString("N");
            Store(key, ticket);
            return Task.FromResult(key);
        }

        public Task RenewAsync(string key, AuthenticationTicket ticket) {
            Store(key, ticket);
            return Task.CompletedTask;
        }

        public Task<AuthenticationTicket?> RetrieveAsync(string key) {
            _cache.TryGetValue(CacheKey(key), out AuthenticationTicket? ticket);
            return Task.FromResult(ticket);
        }

        public Task RemoveAsync(string key) {
            _cache.Remove(CacheKey(key));
            return Task.CompletedTask;
        }

        private void Store(string key, AuthenticationTicket ticket) {
            var options = new MemoryCacheEntryOptions();

            // Mirror the cookie's own expiry on the cache entry so a session that was never explicitly
            // logged out still gets cleaned up instead of living in memory forever.
            if (ticket.Properties.ExpiresUtc.HasValue) {
                options.SetAbsoluteExpiration(ticket.Properties.ExpiresUtc.Value);
            }

            _cache.Set(CacheKey(key), ticket, options);
        }

        private static string CacheKey(string key) => $"{KeyPrefix}{key}";
    }
}
