namespace Erlang.Tests;

internal static class ReferenceResourceHostNames
{
    public const string Test = "runtime/owned-reference-live-ETF-text-prepared-resource-lifetime";
    public const string Failure = "Owned reference resource assertion failed";
    public const string Entry = "run";
    public const string OtherNode = "reference-resource-peer@host";
    public const int GarbageCollectionAttempts = 8;
    public const int ConcurrentCopies = 32;
    public static readonly TimeSpan BarrierTimeout = TimeSpan.FromSeconds(5);
    public static readonly byte[] Binary = [1, 2, 3, 4];
}
