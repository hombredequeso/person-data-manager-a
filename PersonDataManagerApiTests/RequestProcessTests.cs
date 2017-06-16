using FluentAssertions;
using Hdq.PersonDataManager.Api.Lib;
using Hdq.PersonDataManager.Api.Modules;
using NUnit.Framework;

namespace PersonDataManagerApiTests
{
    [TestFixture]
    public class RequestProcessTests
    {
        [Test]
        public void Deserialize_WithValidInput_ReturnsRightResult()
        {
            var result =RequestProcessor.Deserialize<int>("1");
            result.Should().Be(new Either<RequestProcessor.ErrorCode, int>(1));
        }
        
        [Test]
        public void Deserialize_WithInValidInput_ReturnsLeftResult()
        {
            var result =RequestProcessor.Deserialize<int>("a");
            result.Should()
                .Be(new Either<RequestProcessor.ErrorCode, int>(RequestProcessor.ErrorCode.DeserializationError));
        }
    }
}