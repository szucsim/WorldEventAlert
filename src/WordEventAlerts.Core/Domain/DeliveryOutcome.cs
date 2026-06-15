namespace WordEventAlerts.Core.Domain;

public enum DeliveryOutcome
{
    Succeeded = 0,
    FailedTransient = 1,
    FailedPermanent = 2,
    DeadLettered = 3
}
