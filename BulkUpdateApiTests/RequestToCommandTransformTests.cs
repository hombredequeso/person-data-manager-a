using BulkUpdateApi.Api;
using BulkUpdateApi.Command;
using BulkUpdateApi.Domain;
using FluentAssertions;
using Xunit;

namespace BulkUpdateApi.Tests
{
    public class RequestToCommandTransformTests
    {
        [Theory]
        [InlineData("None", Status.None)]
        [InlineData("none", Status.None)]
        [InlineData("NONE", Status.None)]
        void ToDomainStatus_Correctly_Parses_Input(string s, Status expectedResult)
        {
            var result = RequestToCommandTransform.ToDomainStatus(s);
            result.Should().Be(expectedResult);
        }
    }
}
