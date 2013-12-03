using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BookSleeve;

namespace RedisCountingActiveUsers
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var time = new DateTime(2013, 11, 11, 10, 30, 0);
            var redis = new RedisConnection("localhost");
            redis.Wait(redis.Open());

            var activity = new RedisActivityMonitor(redis);

            // Normal activity.
            activity.Beacon("documents:1", time.AddSeconds(-15), 1, "John");
            activity.Beacon("documents:1", time, 2, "Sue");
            activity.Beacon("documents:1", time.AddSeconds(5), 3, "Mary");
            PrintAll(activity, "documents:1", time.AddSeconds(23));

            // Ignore duplicates.
            activity.Beacon("documents:2", time, 1, "John");
            activity.Beacon("documents:2", time.AddSeconds(5), 1, "John");
            PrintAll(activity, "documents:2", time.AddSeconds(7));

            redis.Close(abort: true);

            Console.Read();
        }

        private static void PrintAll(IActivityMonitor monitor, string key, DateTime time)
        {
            Console.WriteLine("Users for {0}", key);

            foreach (var user in monitor.GetAll(key, time))
            {
                Console.WriteLine("* {0}-{1}", user.Id, user.Name);
            }

            Console.WriteLine();
        }
    }
}