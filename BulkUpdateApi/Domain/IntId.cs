using System;

namespace BulkUpdateApi.Domain
{
    public class IntId : Id<int>
    {
        public IntId(int value) : base(value)
        {
            if (value < 1)
                throw new ArgumentException($"int id value cannot be less than 1: {value}");
        }
    }
}