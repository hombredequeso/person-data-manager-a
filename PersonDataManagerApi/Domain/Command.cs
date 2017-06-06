using System;

namespace Hdq.PersonDataManager.Api.Domain
{
    public class Command<T>
    {
        public Command(Guid id, T cmd)
        {
            Cmd = cmd;
            Id = id;
        }

        public T Cmd { get; }
        public Guid Id { get; }
    }
}