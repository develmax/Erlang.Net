namespace Erlang;

public static class DistributionSpawnLayout
{
    public const int RequestArity = 6;
    public const int RequestTraceArity = 7;
    public const int ReplyArity = 5;
    public const int ReplyTraceArity = 6;
    public const int ReferenceIndex = 1;
    public const int ParentIndex = 2;
    public const int GroupLeaderIndex = 3;
    public const int MfaIndex = 4;
    public const int OptionsIndex = 5;
    public const int RequestTokenIndex = 6;
    public const int ReplyFlagsIndex = 3;
    public const int ReplyResultIndex = 4;
    public const int ReplyTokenIndex = 5;
    public const int LinkFlag = 1;
    public const int MonitorFlag = 2;
    public const int MaximumReplyFlags = (1 << 27) - 1;
}
