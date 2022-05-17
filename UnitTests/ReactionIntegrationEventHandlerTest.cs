using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Internal;
using Moq;
using RobotCuratorApi.IntegrationEvents.Events;
using RobotCuratorApi.IntegrationEvents.Handlers;
using RobotCuratorApi.RobotServices.DecisionTableTaskImpl;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace RobotCuratorApiTest.UnitTests
{
    public class ReactionIntegrationEventHandlerTest
    {
        private readonly Mock<ITranslateReaction> _service;
        private readonly Mock<ILogger<ReactionIntegrationEventHandler>> _logger;
        public ReactionIntegrationEventHandlerTest()
        {
            _service = new Mock<ITranslateReaction>();
            _logger = new Mock<ILogger<ReactionIntegrationEventHandler>>();
        }

        [Fact]
        public async Task ReactionIntegrationEventHandle_Success()
        {
            //Arrange
            var fakeEvent = new ReactionIntegrationEvent
            {
                DocSubTypeId = 123
            };

            var handler = new ReactionIntegrationEventHandler(_service.Object, _logger.Object);

            //Act
            await handler.Handle(fakeEvent);

            //Assert
            _logger.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<FormattedLogValues>(v => v.ToString().Contains("Succesfully handled")),
                It.IsAny<Exception>(),
                It.IsAny<Func<object, Exception, string>>()
                ));
        }

        [Fact]
        public async Task ReactionIntegrationEventHandle_NotNull()
        {
            //Arrange
            var fakeEvent = new ReactionIntegrationEvent
            {
            };

            var handler = new ReactionIntegrationEventHandler(_service.Object, _logger.Object);

            //Act
            await handler.Handle(fakeEvent);

            //Assert
            _logger.Verify(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<FormattedLogValues>(v => v.ToString().Contains("DocSubTypeId cannot be null")),
                It.IsAny<Exception>(),
                It.IsAny<Func<object, Exception, string>>()
                ));
        }
    }
}
