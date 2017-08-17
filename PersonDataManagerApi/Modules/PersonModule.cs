using System;
using Hdq.PersonDataManager.Api.Dal;
using Hdq.PersonDataManager.Api.Domain;
using Hdq.PersonDataManager.Api.Lib;
using Nancy;
using Nancy.Extensions;
using Nancy.ModelBinding;
using Newtonsoft.Json;

namespace Hdq.PersonDataManager.Api.Modules
{
    public class Coord
    {
        public decimal Lat { get; set; }
        public decimal Lon { get; set; }
    }

    public class Name
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    public class ContactDetails
    {
        public PhoneContact[] Phone { get; set; }
        public EmailContact[] Email { get; set; }
        
    }

    public class PhoneContact
    {
        public string Label { get; set; }
        public string Number { get; set; }
    }

    public class EmailContact
    {
        public string Label { get; set; }
        public string Address { get; set; }
    }

    public class Person
    {
        public string Id { get; set; }
        public Name Name { get; set; }
        public string[] Tags { get; set; }
        public PoolStatus[] PoolStatuses { get; set; }
        public ContactDetails Contact { get; set; }
        public Address Address { get; set; }
    }

    public class Address
    {
        public string Region { get; set; }
        public Geo Geo { get; set; }
    }

    public class Geo
    {
        public Coord Coord { get; set; }
    }

    public class GeoDistance
    {
        public Coord Coord { get; set; }
        public decimal Distance { get; set; }
    }

    public class PostFilter
    {
        public string[] Tags { get; set; }
    }

    public class PersonMatch
    {
        public Name Name { get; set; }
        public string[] Tags { get; set; }
        public PoolStatus[] PoolStatuses { get; set; }
        public GeoDistance Near { get; set; }
        public PostFilter PostFilter { get; set; }
    }

    public class PoolStatus
    {
        public Pool Pool { get; set; }
        public string Status { get; set; }
    }

    public class Pool
    {
        public Guid Id { get; set; }
        public string Description { get; set; }
    }

    public class BulkTagAdd
    {
        public PersonMatch Match { get; set; }
        public string AddTag { get; set; }
    }

    public static class DynamicParser
    {
        public static Either<HttpError, int> GetQueryParameter(dynamic x, int defaultValue)
        {
            if (!x.HasValue)
            {
                return defaultValue;
            }
            int i;
            if (!Int32.TryParse(x, out i))
                return new HttpError(HttpStatusCode.BadRequest, "");
            return i;
        }
    }
    
    
    public static class RequestProcessor
    {
        public enum ErrorCode
        {
           DeserializationError = 1 
        };
        
        public static Either<HttpError, Pager> GetPager(Request r)
        {
            Either<HttpError, int> @from = DynamicParser.GetQueryParameter(r.Query["from"], 0);
            return @from.Match(
                e => new Either<HttpError, Pager>(new HttpError(HttpStatusCode.BadRequest, "")),
                f =>
                {
                    Either<HttpError, int> size1 = DynamicParser.GetQueryParameter(r.Query["size"], 10);
                    return size1.Match(
                        e => new Either<HttpError, Pager>(new HttpError(HttpStatusCode.BadRequest, "")),
                        s => new Either<HttpError, Pager>(new Pager(f, s)));
                });
        }
        
        public static Either<ErrorCode, T> Deserialize<T>(string s)
        {
            try
            {
                T result = JsonConvert.DeserializeObject<T>(s);
                return new Either<ErrorCode, T>(result);
            }
            catch (JsonException)
            {
                return new Either<ErrorCode, T>(ErrorCode.DeserializationError);
            }
        }
    }

    public class Pager
    {
        public Pager(int @from, int size)
        {
            if (@from < 0)
                throw new ArgumentException("from cannot be negative");
            if (size < 0)
                throw new ArgumentException("size cannot be negative");
            From = @from;
            Size = size;
        }

        public int From { get; }
        public int Size { get; }
    }

    public class HttpError
    {
        public HttpError(HttpStatusCode statusCode, string errorMessage)
        {
            StatusCode = statusCode;
            ErrorMessage = errorMessage;
        }

        public HttpStatusCode StatusCode { get; }
        public string ErrorMessage { get; }
    }
    
    
    public class PersonModule : NancyModule
    {
        public Response Search(PersonMatch apiSearch, Pager pager)
        {
            if (apiSearch.PoolStatuses == null)
            {
                apiSearch.PoolStatuses = new PoolStatus[0];
            }
            var searchResult = ElasticsearchQueries.SearchPeople(
                apiSearch,
                pager.From,
                pager.Size);
            Response resp =  !string.IsNullOrWhiteSpace(searchResult)
                ? Response.AsText(searchResult, "application/json")
                : HttpStatusCode.InternalServerError;
            return resp;
            
        }

        public PersonModule()
        {
            Get["/api/person/{id}"] = parameters =>
            {
                string id = parameters.id;
                Person personFromElastic = ElasticsearchQueries.GetPerson(id);
                return personFromElastic != null
                    ? Response.AsJson(personFromElastic)
                    : HttpStatusCode.NotFound;
            };

            Post["api/person"] = parameters =>
            {
                var person = this.Bind<Person>();
                var success = ElasticsearchQueries.IndexPerson(person);
                return success ? HttpStatusCode.Created : HttpStatusCode.InternalServerError;
            };

            Post["/api/person/search"] = parameters =>
            {
                var eitherPager = RequestProcessor.GetPager(Request);
                return eitherPager.Match(
                    e => HttpStatusCode.BadRequest,
                    pager =>
                    {
                        var apiSearch2 = RequestProcessor.Deserialize<PersonMatch>(Request.Body.AsString());
                        return apiSearch2.Match(
                            e => HttpStatusCode.BadRequest,
                            apiSearch => Search(apiSearch, pager));
                    }
                );
            };

            Post["/api/person/morelike"] = parameters =>
            {
                var apiSearch = this.Bind<string[]>();
                var searchResult = ElasticsearchQueries.MoreLikePeople(apiSearch);
                return !string.IsNullOrWhiteSpace(searchResult)
                    ? Response.AsText(searchResult, "application/json")
                    : HttpStatusCode.InternalServerError;
            };

            Post["/api/person/tag/"] = parameters =>
            {
                var bulkTagAdd = this.Bind<BulkTagAdd>();
                var cmd = new Command<BulkTagAdd>(Guid.NewGuid(), bulkTagAdd);
                ElasticsearchQueries.CommandResponse result = ElasticsearchQueries.UpdateMatchingPersonTags(cmd);
                return Response
                    .AsJson(ToResponseBody(result))
                    .WithStatusCode(result.Success ? HttpStatusCode.OK : HttpStatusCode.InternalServerError);
            };
        }

        public static object ToResponseBody(ElasticsearchQueries.CommandResponse result)
        {
            return new
            {
                cmdId = result.CommandId,
                success = result.Success
            };
        }
    }
}