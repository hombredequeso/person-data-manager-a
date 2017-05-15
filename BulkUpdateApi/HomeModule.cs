using Nancy;

namespace BulkUpdateApi
{
        public class IndexModule : NancyModule
        {
            public IndexModule()
            {
                Get["/test"] = _ => "hello world";
            }
        }
}