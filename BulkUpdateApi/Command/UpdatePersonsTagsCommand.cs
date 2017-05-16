using System;
using System.Collections.Generic;
using System.Linq;
using BulkUpdateApi.Domain;

namespace BulkUpdateApi.Command
{
    public class UpdatePersonsTagsCommand
    {
        public UpdatePersonsTagsCommand(
            Guid commandId, 
            Tag newTag, 
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
        public Tag NewTag { get; private set; }
        public PersonMatch Matching { get; private set; }


        public class PersonMatch
        {
            public PersonMatch(IEnumerable<Id<int>> tagIds)
            {
                TagIds = tagIds.ToArray();
            }

            public Id<int>[] TagIds { get; private set; }
        }
    }
}