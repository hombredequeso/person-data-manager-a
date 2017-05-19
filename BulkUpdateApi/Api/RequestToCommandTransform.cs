using System;
using System.Linq;
using BulkUpdateApi.Command;
using BulkUpdateApi.Domain;

namespace BulkUpdateApi.Api
{
    public static class RequestToCommandTransform
    {
        public static CreatePersonCommand ToCreateCommand(Person apiPerson)
        {
            return new CreatePersonCommand(
                new String50(apiPerson.Id),
                new String50(apiPerson.Name),
                apiPerson.Tags.Select(ToDomainTag),
                apiPerson.PoolStatuses.Select(ToDomainPoolStatus));
        }

        public static Tag ToDomainTag(EntityRef apiTag)
        {
            return new Tag(
                new Id<int>(Int32.Parse(apiTag.Id)), 
                new String50(apiTag.Value));
        }

        public static UpdatePersonsTagsCommand GetCommand(BulkTagAdd requestBody)
        {
            return new UpdatePersonsTagsCommand(
                Guid.NewGuid(),
                ToDomainTag(requestBody.AddTag),
                ToCmdPersonMatch(requestBody.Match));
        }

        private static UpdatePersonsTagsCommand.PersonMatch ToCmdPersonMatch(
            PersonMatch match)
        {
            return
                new UpdatePersonsTagsCommand.PersonMatch(
                    match.Tags.Select(x => new Id<int>(x)),
                    match.PoolStatuses.Select(ToDomainPoolStatus)
                    );
        }

        public static Domain.PoolStatus ToDomainPoolStatus(PoolStatus apiPoolStatus)
        {
            return new Domain.PoolStatus(
                new Pool(
                    new Id<int>(Int32.Parse(apiPoolStatus.Pool.Id)),
                    new String50(apiPoolStatus.Pool.Value)),
                ToDomainStatus(apiPoolStatus.Status));
        }

        private static bool shouldIgnoreCase = true;

        public static Status ToDomainStatus(string apiStatus)
        {
            return (Status)Enum.Parse(typeof(Status), apiStatus, shouldIgnoreCase);
        }
    }
}