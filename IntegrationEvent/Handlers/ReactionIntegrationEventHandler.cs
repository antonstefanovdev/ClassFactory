using Microsoft.eShopOnContainers.BuildingBlocks.EventBus.Abstractions;
using Microsoft.Extensions.Logging;
using RobotCuratorApi.IntegrationEvents.Events;
using RobotCuratorApi.RobotServices.DecisionTableTaskImpl;
using System;
using System.Threading.Tasks;

namespace RobotCuratorApi.IntegrationEvents.Handlers
{
    public class ReactionIntegrationEventHandler : IIntegrationEventHandler<ReactionIntegrationEvent>
    {
        private readonly ITranslateReaction _service;
        private readonly ILogger<ReactionIntegrationEventHandler> _logger;

        public ReactionIntegrationEventHandler(ITranslateReaction service, ILogger<ReactionIntegrationEventHandler> logger)
        {
            _service = service;
            _logger = logger;
        }

        public async Task Handle(ReactionIntegrationEvent @event)
        {
            _logger.LogInformation("----- Handling integration event: {IntegrationEventId} at {AppName} - ({@IntegrationEvent})", @event.Id, "RobotCurator", @event);
            if (@event.DocSubTypeId > 0)
            {
                try
                {
                    var result = await _service.React(@event);
                    _logger.LogInformation("----- Succesfully handled");
                }
                catch (ArgumentNullException ae)
                {
                    _logger.LogError("----- Wrong argument");
                    _logger.LogError(ae.Message);
                }
                catch (Exception e)
                {
                    _logger.LogError("----- Something went wrong, check logs for details");
                    _logger.LogError(e.Message);
                }
            }
            else
            {
                _logger.LogError("----- DocSubTypeId cannot be null.");
                return;
            }
        }
    }
}
