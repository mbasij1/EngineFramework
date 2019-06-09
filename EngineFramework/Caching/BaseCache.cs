using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EngineFramework.Caching
{
    public class BaseCache<T>
    {
        protected byte TTL { get; set; }
        protected object _lockObject = new object();
        private Dictionary<byte[], T> CachedData;
        private Dictionary<byte[], int> TTLs;
        private Dictionary<byte[], DateTime> ExpiredTimes;

        protected TimeSpan? ExpiredTimeSpan { get; set; }

        public BaseCache()
        {
            TTL = 50;
            CachedData = new Dictionary<byte[], T>(new ByteArrayComparer());
            TTLs = new Dictionary<byte[], int>(new ByteArrayComparer());
            ExpiredTimes = new Dictionary<byte[], DateTime>(new ByteArrayComparer());
        }

        protected T GetDataFromCache(byte[] key, Func<T> func)
        {
            T result;
            int ttl;

            lock (_lockObject)
            {
                if (TTLs.TryGetValue(key, out ttl))
                {
                    TTLs[key] = TTL;
                    result = CachedData[key];
                    if(ExpiredTimeSpan.HasValue)
                        ExpiredTimes[key] = DateTime.UtcNow.Add(ExpiredTimeSpan.Value);
                }
                else
                {
                    result = func();
                    CachedData[key] = func();
                    TTLs[key] = TTL;
                }

                TTLs.Keys.ToList().ForEach(t =>
                {
                    TTLs[t]--;
                });

                var toRemove = TTLs.Where(pair => pair.Value < 0)
                         .Select(pair => pair.Key)
                         .ToList();

                foreach (var tkey in toRemove)
                {
                    TTLs.Remove(tkey);
                    CachedData.Remove(tkey);
                    ExpiredTimes.Remove(tkey);
                }

                if (ExpiredTimeSpan.HasValue)
                {
                    toRemove = ExpiredTimes.Where(pair => pair.Value <= DateTime.UtcNow)
                         .Select(pair => pair.Key)
                         .ToList();

                    foreach (var tkey in toRemove)
                    {
                        TTLs.Remove(tkey);
                        CachedData.Remove(tkey);
                        ExpiredTimes.Remove(tkey);
                    }
                }
            }

            return result;
        }
    }
}
