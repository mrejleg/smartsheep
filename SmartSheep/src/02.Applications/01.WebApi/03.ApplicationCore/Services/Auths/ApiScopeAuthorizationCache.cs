namespace Api.ApplicationCore.Services.Auths
{
    public static class ApiScopeAuthorizationCache
    {
        private static Dictionary<string, HashSet<string>> _snapshot;
        private static long _expiresAtUtcTicks;

        public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(15);
        public static readonly SemaphoreSlim RefreshLock = new(1, 1);

        public static bool TryAuthorize(string clientId, string scope, out bool isAuthorized)
        {
            var snapshot = Volatile.Read(ref _snapshot);
            if (snapshot == null || DateTime.UtcNow.Ticks >= Interlocked.Read(ref _expiresAtUtcTicks))
            {
                isAuthorized = false;
                return false;
            }

            isAuthorized = snapshot.TryGetValue(clientId, out var scopes)
                && scopes.Contains(scope);
            return true;
        }

        public static void Set(Dictionary<string, HashSet<string>> snapshot)
        {
            Volatile.Write(ref _snapshot, snapshot);
            Interlocked.Exchange(ref _expiresAtUtcTicks, DateTime.UtcNow.Add(Lifetime).Ticks);
        }

        public static void Invalidate()
        {
            Volatile.Write(ref _snapshot, null);
            Interlocked.Exchange(ref _expiresAtUtcTicks, 0);
        }
    }
}
