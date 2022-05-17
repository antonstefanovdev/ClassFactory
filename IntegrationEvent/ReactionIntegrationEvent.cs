using Microsoft.eShopOnContainers.BuildingBlocks.EventBus.Events;

namespace RobotCuratorApi.IntegrationEvents.Events
{
    public class ReactionIntegrationEvent:IntegrationEvent
    {
        public int DocSubTypeId { get; set; }
        public int ProjectId { get; set; }
        public int CaseId { get; set; }
        public bool IsScanOrCopy { get; set; }
    }
}
