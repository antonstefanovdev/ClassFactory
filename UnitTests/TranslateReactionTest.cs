using Moq;
using RobotCuratorApi.IntegrationEvents.Events;
using RobotCuratorApi.Models;
using RobotCuratorApi.RobotServices;
using RobotCuratorApi.RobotServices.DecisionTableTaskImpl;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace RobotCuratorApiTest.UnitTests
{
    public class TranslateReactionTest
    {
        [Fact]
        public async Task React_Success()
        {
            //Arrange
            var reactionIntegrationEvent = new ReactionIntegrationEvent
            {
                DocSubTypeId = 1,
                ProjectId = 33,
                IsScanOrCopy = true,
                CaseId = 77
            };
            var fakeReactionInput = new GetReactionsInput
            {
                DocSubTypeID = 1,
                ProjectID = 33,
                forScanOrCopy = true,
                forOriginal = false
            };
            var fakeReactionOutput = new GetReactionsOutput
            {
                CalledMethods = ""
            };

            var decisionTable = new Mock<DecisionTableServiceLetsFormInputForActionWhenClose>();
            decisionTable.Setup(x => x.GetReactionsOutput(
                It.Is<GetReactionsInput>(s => s.DocSubTypeID == fakeReactionInput.DocSubTypeID)
                ))
                .Returns(Task.FromResult(fakeReactionOutput));

            //var service = new TranslateReaction(decisionTable.Object);
            //Act
            var result = await service.React(reactionIntegrationEvent);

            //Assert
            Assert.NotNull(result);
        }

        [Theory]
        [InlineData("called method result 1")]
        [InlineData("called method result 2")]
        [InlineData("called method result 3")]
        public async Task React_Success_Many(string expectedCalledMethod)
        {
            //Arrange
            var reactionIntegrationEvent = new ReactionIntegrationEvent
            {
                DocSubTypeId = 1,
                ProjectId = 33,
                IsScanOrCopy = true,
                CaseId = 77
            };
            var fakeReactionInput = new GetReactionsInput
            {
                DocSubTypeID = 1,
                ProjectID = 33,
                forScanOrCopy = true,
                forOriginal = false
            };
            var fakeReactionOutput = new GetReactionsOutput
            {
                CalledMethods = expectedCalledMethod
            };

            var decisionTable = new Mock<DecisionTableServiceLetsFormInputForActionWhenClose>();
            decisionTable.Setup(x => x.GetReactionsOutput(
                It.Is<GetReactionsInput>(s => s.DocSubTypeID == fakeReactionInput.DocSubTypeID)
                ))
                .Returns(Task.FromResult(fakeReactionOutput));

            //var service = new TranslateReaction(decisionTable.Object);
            //Act
            var result = await service.React(reactionIntegrationEvent);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(expectedCalledMethod, result);
        }
    }
}
