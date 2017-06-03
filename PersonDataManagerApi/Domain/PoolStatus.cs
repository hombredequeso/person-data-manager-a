using System;
using System.ComponentModel;

namespace Hdq.PersonDataManager.Api.Domain
{
    public class PoolStatus
    {
        public PoolStatus(
            Pool pool,
            Status status)
        {
            if (pool == null) throw new ArgumentNullException(nameof(pool));
            if (!Enum.IsDefined(typeof(Status), status))
                throw new InvalidEnumArgumentException(nameof(status), (int) status, typeof(Status));

            Pool = pool;
            Status = status;
        }

        public Pool Pool { get; private set; }
        public Status Status { get; private set; }
    }
}