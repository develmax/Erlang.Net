namespace Erlang;

public static class DistributionPassThroughLayout
{
    public const byte Marker = 112;
    public const int MarkerBytes = 1;
    public const int SendOpcode = 2;
    public const int RegisteredSendOpcode = 6;
    public const int OpcodeIndex = 0;
    public const int SendArity = 3;
    public const int RegisteredSendArity = 4;
    public const int SendTargetIndex = 2;
    public const int RegisteredSenderIndex = 1;
    public const int RegisteredTargetIndex = 3;
    public const string EmptyCookie = "";
}
