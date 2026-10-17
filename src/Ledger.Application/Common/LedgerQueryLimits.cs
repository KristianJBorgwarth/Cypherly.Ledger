namespace Ledger.Application.Common;

public static class LedgerQueryLimits
{
    public const int MaxEventsPerRead = 500;
    public const int MaxPayloadBytes = 64 * 1024;
    public const int MaxKeysPerEvent = 256;
    public const int ReadSignatureMaxAgeSeconds = 120;
}
