using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RedisCountingActiveUsers
{
    public interface IActivityMonitor
    {
        void Beacon(string key, DateTime time, int userId, string userName);
        IEnumerable<ActiveUser> GetAll(string key, DateTime time);
    }
}
