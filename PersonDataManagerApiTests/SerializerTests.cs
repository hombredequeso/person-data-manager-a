using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NUnit.Framework;

namespace PersonDataManagerApiTests
{
    public class MyClass
    {
        public string Property1 { get; set; }
    }

    [TestFixture]
    public class SerializerTests
    {
        [Test]
        public void Serializer_CamelCases_Property_Names()
        {
            MyClass x = new MyClass() {Property1 = "abc"};
            JsonSerializerSettings settings =
                new JsonSerializerSettings {ContractResolver = new CamelCasePropertyNamesContractResolver()};
            JsonSerializer serializer = JsonSerializer.CreateDefault(settings);
            JObject jObj = JObject.FromObject(x, serializer);
            string ser = jObj.ToString();
            ser.Should().Be(ser.ToLower());
        }
    }
}