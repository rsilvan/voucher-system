namespace VoucherSystem.Application;

/// <summary>
/// Thrown when a state machine transition is invalid for the current entity status.
/// Maps to HTTP 400 Bad Request — the caller attempted an invalid state change.
/// </summary>
public class CampaignStateMachineException : Exception
{
    public CampaignStateMachineException(string message) : base(message) { }
    public CampaignStateMachineException(string message, Exception inner) : base(message, inner) { }
}
