namespace Erlang;

public static class DistributionExplicitPidExitLayout
{
    public const int Arity = 4;
    public const int FlagsIndex = 1;
    public const int SenderIndex = 2;
    public const int TargetIndex = 3;
    public const int ExitAction = 16;
    public const int Priority = 1;
    public const int PriorityExitAction = ExitAction | Priority;
}
