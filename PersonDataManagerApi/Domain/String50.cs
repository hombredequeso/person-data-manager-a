using System;

namespace Hdq.PersonDataManager.Api.Domain
{
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