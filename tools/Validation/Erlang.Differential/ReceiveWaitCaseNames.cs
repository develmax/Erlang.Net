namespace Erlang.Differential;

public static class ReceiveWaitCaseNames
{
    public const string Prefix = "receive-wait-timeout-";
    public const string Matched = "matched";
    public const string Caught = "caught";
    public const string Preserved = "preserved";
    public const string Evaluated = "evaluated";
    public const string QueuedAtom = Prefix + "queued-match-atom";
    public const string QueuedNegative = Prefix + "queued-match-negative";
    public const string QueuedFloat = Prefix + "queued-match-float";
    public const string QueuedAboveLimit = Prefix + "queued-match-above-limit";
    public const string UnmatchedInvalid = Prefix + "unmatched-retained-invalid";
    public const string ExpressionBeforeMatch = Prefix + "timeout-expression-before-match";
    public const string ExpressionError = Prefix + "timeout-expression-error-before-match";
    public const string QueuedZero = Prefix + "queued-match-zero";
}
