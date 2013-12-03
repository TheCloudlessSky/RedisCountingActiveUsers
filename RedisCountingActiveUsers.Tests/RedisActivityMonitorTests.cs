using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BookSleeve;
using Xunit;
using Should;

namespace RedisCountingActiveUsers.Tests
{
    public class RedisActivityMonitorTests : IDisposable
    {
        private const int Db = 0;
        private readonly RedisConnection redis;
        private readonly RedisActivityMonitor sut;

        public RedisActivityMonitorTests()
        {
            redis = new RedisConnection("localhost", allowAdmin: true);
            sut = new RedisActivityMonitor(redis);
            redis.Wait(redis.Open());

            redis.Wait(redis.Server.FlushDb(0));
        }

        [Fact]
        void sets_the_user_for_current_time()
        {
            var now = DateTime.Now;
            sut.Beacon("posts/1", now, 1, "John");

            var result = sut.GetAll("posts/1", now);

            result.ShouldEqual(new[] { new ActiveUser(1, "John") });
        }

        [Fact]
        void does_not_include_results_from_a_different_key()
        {
            var now = DateTime.Now;
            sut.Beacon("posts/1", now, 1, "John");
            sut.Beacon("posts/2", now, 2, "Mary");

            var result = sut.GetAll("posts/1", now);

            result.ShouldEqual(new[] { new ActiveUser(1, "John") });
        }

        [Fact]
        void does_not_get_users_outside_of_current_window_and_sliding_window()
        {
            var now = DateTime.Now;
            sut.Beacon("posts/1", now, 1, "John");

            var result = sut.GetAll("posts/1", now.AddSeconds(60));

            result.ShouldBeEmpty();
        }

        [Fact]
        void gets_the_user_from_previous_window_and_current_window_when_within_sliding_window()
        {
            var previousWindow = new DateTime(2013, 11, 11, 5, 30, 25);
            var now = new DateTime(2013, 11, 11, 5, 30, 45);
            sut.Beacon("posts/1", previousWindow, 1, "John");
            sut.Beacon("posts/1", now.AddSeconds(-2), 2, "Mary");

            var result = sut.GetAll("posts/1", now);

            result.ShouldEqual(new[]
            {
                new ActiveUser(1, "John"),
                new ActiveUser(2, "Mary")
            });
        }

        [Fact]
        void does_not_get_users_from_previous_window_that_are_not_within_the_sliding_window()
        {
            var previousWindow = new DateTime(2013, 11, 11, 5, 30, 25);
            var now = new DateTime(2013, 11, 11, 5, 30, 45);
            sut.Beacon("posts/1", previousWindow.AddSeconds(-25), 2, "Mary");
            sut.Beacon("posts/1", previousWindow, 1, "John");
            sut.Beacon("posts/1", now.AddSeconds(-2), 3, "Sue");

            var result = sut.GetAll("posts/1", now);

            result.ShouldEqual(new[]
            {
                new ActiveUser(1, "John"),
                new ActiveUser(3, "Sue")
            });
        }

        [Fact]
        void does_not_count_duplciate_entries_from_previous_and_current_window()
        {
            var previousWindow = new DateTime(2013, 11, 11, 5, 30, 25);
            var now = new DateTime(2013, 11, 11, 5, 30, 45);
            sut.Beacon("posts/1", previousWindow, 1, "John");
            sut.Beacon("posts/1", now.AddSeconds(-2), 1, "John");

            var result = sut.GetAll("posts/1", now);

            result.ShouldEqual(new[] { new ActiveUser(1, "John") });
        }

        [Fact]
        void does_not_count_duplciate_entries_from_current_window()
        {
            var now = new DateTime(2013, 11, 11, 5, 30, 45);
            sut.Beacon("posts/1", now.AddSeconds(-2), 1, "John");
            sut.Beacon("posts/1", now, 1, "John");
            sut.Beacon("posts/1", now, 2, "Mary");

            var result = sut.GetAll("posts/1", now);

            result.ShouldEqual(new[]
            {
                new ActiveUser(1, "John"),
                new ActiveUser(2, "Mary")
            });
        }

        public void Dispose()
        {
            redis.Dispose();
        }
    }
}