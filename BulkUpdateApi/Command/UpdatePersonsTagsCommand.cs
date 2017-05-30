using System;
using System.Collections.Generic;
using System.Linq;
using BulkUpdateApi.Api;

namespace BulkUpdateApi.Command
{
    public class UpdatePersonsTagsCommand
    {
        public UpdatePersonsTagsCommand(
            Guid commandId, 
            EntityRef newTag, 
            PersonMatch matching)
        {
            if (newTag == null) throw new ArgumentNullException(nameof(newTag));
            if (matching == null) throw new ArgumentNullException(nameof(matching));
            if (commandId == Guid.Empty)
                throw new ArgumentException("commandId cannot be empty");

            CommandId = commandId;
            NewTag = newTag;
            Matching = matching;
        }

        public Guid CommandId { get; private set; }
        public EntityRef NewTag { get; private set; }
        public PersonMatch Matching { get; private set; }

        public class PersonMatch
        {
            public PersonMatch(
                IEnumerable<int> tagIds, 
                IEnumerable<PoolStatus> poolStatuses)
            {
                PoolStatuses = poolStatuses.ToArray();
                TagIds = tagIds.ToArray();
            }

            public int[] TagIds { get; private set; }
            public PoolStatus[] PoolStatuses { get; private set; }
        }
    }
}
