namespace Arbitrage.Api.Enums
{
    public enum ResponseStatus
    {
        Ok,
        InvalidLogin,
        NotAllowed,
        AlreadyExists,
        NotFound,
        SoftwareLocationChanged,
        NotActive,
        Unknown
    }
    public enum SubscriptionStatus
    {
        None,
        Requested,
        Rejected,
        Active,
        Blocked
    }
}