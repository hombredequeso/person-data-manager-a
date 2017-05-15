using System;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;

namespace BulkUpdateApi
{
    // Request classes:
    public class Tag
    {
        public string Id { get; set; }
        public string Value { get; set; }
    }

    public class Person
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public Tag[] Tags { get; set; }
    }

    public class PersonMatch
    {
        public string Name { get; set; }
        public int[] Tags { get; set; }
    }

    public class BulkTagAdd
    {
        public PersonMatch Match { get; set; }
        public Tag AddTag { get; set; }
    }

    public static class RequestToCommandTransform
    {
        public static UpdatePersonsTagsCommand GetCommand(BulkTagAdd requestBody)
        {
            return new UpdatePersonsTagsCommand(
                Guid.NewGuid(),
                new UpdatePersonsTagsCommand.Tag(
                    new Id<int>(int.Parse(requestBody.AddTag.Id)),
                    new String50(requestBody.AddTag.Value)),
                new UpdatePersonsTagsCommand.PersonMatch(
                    requestBody.Match.Tags.Select(x => new Id<int>(x)))
                );
        }
    }

    public class PersonModule : NancyModule
    {
        public PersonModule()
        {
            Get["/api/person/{id}"] = parameters =>
            {
                string id = parameters.id;
                var personFromElastic = ElasticsearchQueries.GetPerson(id);
                return personFromElastic != null
                    ? Response.AsJson(personFromElastic)
                    : HttpStatusCode.NotFound;
            };

            Post["api/person"] = parameters =>
            {
                var person = this.Bind<Person>();
                var success = ElasticsearchQueries.CreatePerson(person);
                return success ? HttpStatusCode.Created : HttpStatusCode.InternalServerError;
            };

            Post["/api/person/tag/"] = parameters =>
            {
                var requestBody = this.Bind<BulkTagAdd>();
                var cmd = RequestToCommandTransform.GetCommand(requestBody);
                var success = ElasticsearchQueries.UpdateMatchingPersonTags(cmd);
                return success ? HttpStatusCode.OK : HttpStatusCode.InternalServerError;
            };
        }
    }
}