using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TaskMaster.API.Controllers;
using TaskMaster.API.Models.Common;
using TaskMaster.API.Models.System;

namespace TaskMaster.Test.UnitTests.APITests;

public class SystemInfoControllerTests
{
    private static SystemInfoController CreateController(string environmentName)
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.Setup(e => e.EnvironmentName).Returns(environmentName);
        return new SystemInfoController(environment.Object, NullLogger<SystemInfoController>.Instance);
    }

    [Test]
    public void GetInfo_ShouldReturnVersionAndEnvironment()
    {
        // Arrange
        var controller = CreateController(Environments.Production);

        // Act
        var result = controller.GetInfo();

        // Assert
        var ok = result!.Result as OkObjectResult;
        var payload = ok!.Value as ApiResponse<SystemInfoResponse>;
        Assert.That(payload!.IsSuccess, Is.True);
        Assert.That(payload.Data!.Version, Is.Not.Empty);
        Assert.That(payload.Data.Environment, Is.EqualTo(Environments.Production));
    }

    [TestCase("Development")]
    [TestCase("Staging")]
    [TestCase("Production")]
    public void GetInfo_ShouldEchoEnvironmentName(string environmentName)
    {
        // Arrange
        var controller = CreateController(environmentName);

        // Act
        var result = controller.GetInfo();

        // Assert
        var ok = result!.Result as OkObjectResult;
        var payload = ok!.Value as ApiResponse<SystemInfoResponse>;
        Assert.That(payload!.Data!.Environment, Is.EqualTo(environmentName));
    }
}