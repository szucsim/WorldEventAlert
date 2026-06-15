namespace WordEventAlerts.Core.Domain;

public sealed class NotificationSendResult
{
    private NotificationSendResult(bool isSuccess, bool isTransientFailure, string? errorCode, string? errorMessage)
    {
        IsSuccess = isSuccess;
        IsTransientFailure = isTransientFailure;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public bool IsSuccess { get; }

    public bool IsTransientFailure { get; }

    public string? ErrorCode { get; }

    public string? ErrorMessage { get; }

    public static NotificationSendResult Succeeded() => new(true, false, null, null);

    public static NotificationSendResult FailedTransient(string errorCode, string errorMessage) =>
        new(false, true, errorCode, errorMessage);

    public static NotificationSendResult FailedPermanent(string errorCode, string errorMessage) =>
        new(false, false, errorCode, errorMessage);
}
