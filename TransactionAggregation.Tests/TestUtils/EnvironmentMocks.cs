using Microsoft.AspNetCore.Hosting;
using Moq;

namespace TransactionAggregation.Tests.TestUtils;

public static class EnvironmentMocks
{
    public static IWebHostEnvironment CreateMockEnv(string contentRootPath)
    {
        var mock = new Mock<IWebHostEnvironment>();
        mock.SetupGet(e => e.ContentRootPath).Returns(contentRootPath);
        return mock.Object;
    }
}