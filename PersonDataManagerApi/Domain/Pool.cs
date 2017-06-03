using System;

namespace Hdq.PersonDataManager.Api.Domain
{
    public class Pool
    {
        public Pool(Id<int> id, String50 value)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (value == null) throw new ArgumentNullException(nameof(value));

            Id = id;
            Value = value;
        }

        public Id<int> Id { get; private set; }
        public String50 Value { get; private set; }
    }
}