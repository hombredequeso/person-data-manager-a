using Nancy;

namespace Hdq.PersonDataManager.Api
{
        public class IndexModule : NancyModule
        {
            public IndexModule()
            {
                Get["/test"] = _ => "hello world";
            }
        }
}