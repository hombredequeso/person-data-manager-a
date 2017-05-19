using System.Collections.Generic;
using System.Linq;
using BulkUpdateApi.Domain;

namespace BulkUpdateApi.Command
{
    public class CreatePersonCommand
    {
        public CreatePersonCommand(
            String50 id, 
            String50 name, 
            IEnumerable<Tag> tags, 
            IEnumerable<PoolStatus> poolStatuses)
        {
            Id = id;
            Name = name;
            Tags = tags.ToList();
            PoolStatuses = poolStatuses.ToList();
        }

        public String50 Id { get; private set; }
        public String50 Name { get; private set; }
        public List<Tag> Tags { get; private set; }
        public List<PoolStatus> PoolStatuses { get; private set; }
    }
}