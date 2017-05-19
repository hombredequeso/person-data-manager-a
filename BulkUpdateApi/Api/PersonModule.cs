using BulkUpdateApi.Dal;
using Nancy;
using Nancy.ModelBinding;

namespace BulkUpdateApi.Api
{
    public class EntityRef
    {
        public string Id { get; set; }
        public string Value { get; set; }
    }

    public class Person
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public EntityRef[] Tags { get; set; }
        public PoolStatus[] PoolStatuses { get; set; }
    }


    public class PersonMatch
    {
        public string Name { get; set; }
        public int[] Tags { get; set; }
        public PoolStatus[] PoolStatuses { get; set; }
    }

    public class PoolStatusMatch
    {
        public int PoolId { get; set; }
        public string Status { get; set; }
    }

    public class PoolStatus
    {
        public EntityRef Pool { get; set; }
        public string Status { get; set; }
    }

    public class BulkTagAdd
    {
        public PersonMatch Match { get; set; }
        public EntityRef AddTag { get; set; }
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

            Post["/api/person/search"] = parameters =>
            {
                var apiSearch = this.Bind<PersonMatch>();
                var searchResult = ElasticsearchQueries.SearchPeople(apiSearch);
                return !string.IsNullOrWhiteSpace(searchResult)
                    ? Response.AsText(searchResult, "application/json")
                    : HttpStatusCode.InternalServerError;
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