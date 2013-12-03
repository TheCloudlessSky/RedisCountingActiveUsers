using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RedisCountingActiveUsers
{
    public class ActiveUser : IEquatable<ActiveUser>
    {
        public int Id { get; private set; }
        public string Name { get; private set; }

        public ActiveUser(int id, string name)
        {
            this.Id = id;
            this.Name = name;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ActiveUser);
        }

        public bool Equals(ActiveUser other)
        {
            if (other == null) return false;

            return Id == other.Id && Name == other.Name;
        }
    }
}
