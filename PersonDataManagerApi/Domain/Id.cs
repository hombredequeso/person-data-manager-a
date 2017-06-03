namespace Hdq.PersonDataManager.Api.Domain
{
    public class Id<T>
    {
        public Id(T value)
        {
            Value = value;
        }

        public T Value { get; private set; }
    }
}