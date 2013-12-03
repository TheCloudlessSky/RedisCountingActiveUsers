using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BookSleeve;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RedisCountingActiveUsers
{
    public class RedisActivityMonitor : IActivityMonitor
    {
        private const int Db = 0;
        private const int WindowWidthSeconds = 30;
        private const string KeyPrefix = "ActivityMonitor";

        private readonly RedisConnection redis;

        public RedisActivityMonitor(RedisConnection redis)
        {
            this.redis = redis;
        }

        public void Beacon(string key, DateTime time, int userId, string userName)
        {
            time = time.ToUniversalTime();

            var window = GetWindowFor(key, time);
            var serialized = JsonConvert.SerializeObject(new ActiveUser(userId, userName));

            using (var transaction = redis.CreateTransaction())
            {
                var addToSet = transaction.SortedSets
                    .Add(db: 0, key: window.Key, value: serialized, score: time.Ticks);

                // Because we always look at the previous + current window, 
                // this window can be discarded when it's no longer needed.
                // Also, normally we'd use an absolute expiry (EXPIREAT) but 
                // this version of BookSleeve (1.3.39) does not support it.
                // https://code.google.com/p/booksleeve/issues/detail?id=50
                var expire = transaction.Keys.Expire(0, window.Key, WindowWidthSeconds * 2);
                
                redis.Wait(transaction.Execute());
            }
        }

        public IEnumerable<ActiveUser> GetAll(string key, DateTime time)
        {
            time = time.ToUniversalTime();

            var result = new HashSet<ActiveUser>();
            
            var startSlidingWindowTime = time.AddSeconds(-WindowWidthSeconds);
            var previousWindow = GetWindowFor(key, startSlidingWindowTime);
            var currentWindow = GetWindowFor(key, time);

            using (var transaction = redis.CreateTransaction())
            {
                var getPrevious = transaction.SortedSets
                    .RangeString(db: 0, key: previousWindow.Key, min: startSlidingWindowTime.Ticks);
                var getCurrent = transaction.SortedSets
                    .RangeString(db: 0, key: currentWindow.Key, start: 0, stop: -1);

                redis.Wait(transaction.Execute());

                AddResults(result, getPrevious.Result);
                AddResults(result, getCurrent.Result);
            }

            return result;
        }

        private void AddResults(HashSet<ActiveUser> result, KeyValuePair<string, double>[] results)
        {
            foreach (var kvp in results)
            {
                // The value of kvp is the score (which is the *time* of the 
                // activity). We don't use the time, but it would be trivial
                // to parse that into a DateTime object.
                var deserialized = JsonConvert.DeserializeObject<ActiveUser>(kvp.Key);
                result.Add(deserialized);
            }
        }

        private Window GetWindowFor(string key, DateTime time)
        {
            int window = time.Second / WindowWidthSeconds;
            var windowTime = new DateTime(time.Year, time.Month, time.Day, 
                                          time.Hour, time.Minute, window * WindowWidthSeconds, DateTimeKind.Utc);
            var activityKey = KeyPrefix + "/" + key + "/" + windowTime.Ticks;

            return new Window(windowTime, activityKey);
        }

        private class Window
        {
            public DateTime StartTime { get; private set; }
            public string Key { get; private set; }

            public Window(DateTime time, string key)
            {
                this.StartTime = time;
                this.Key = key;
            }
        }
    }
}
