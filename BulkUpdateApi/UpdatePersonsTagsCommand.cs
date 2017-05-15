using System;
using System.Collections.Generic;
using System.Linq;

namespace BulkUpdateApi
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

        public class Tag
        {
            public Tag(Id<int> id, String50 value)
            {
                if (id == null) throw new ArgumentNullException(nameof(id));
                if (value == null) throw new ArgumentNullException(nameof(value));

                Id = id;
                Value = value;
            }

            public Id<int> Id { get; private set; }
            public String50 Value { get; private set; }
        }

        public class PersonMatch
        {
            public PersonMatch(IEnumerable<Id<int>> tagIds)
            {
                TagIds = tagIds.ToArray();
            }

            public Id<int>[] TagIds { get; private set; }
        }
    }

    public class IntId : Id<int>
    {
        public IntId(int value) : base(value)
        {
            if (value < 1)
                throw new ArgumentException($"int id value cannot be less than 1: {value}");
        }
    }

    public class Id<T>
    {
        public Id(T value)
        {
            Value = value;
        }

        public T Value { get; private set; }
    }

    public class String50
    {
        public String50(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                throw new ArgumentException("string cannot be null/whitespace");
            }
            if (s.Length > 50)
            {
                var errorS = s.Substring(0, 55) + (s.Length > 55 ? "..." : "");
                throw new ArgumentException($"string cannot be longer than 50 characters: {errorS}");
            }
            Value = s;
        }

        public string Value { get; private set; }
    }

}