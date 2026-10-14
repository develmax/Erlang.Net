namespace Erlang;

public sealed class BeamOnLoadCallbackAbortedException(Term downReason) : Exception(BeamOnLoadDiagnostics.ExitedBeforeBody)
{
    public Term DownReason { get; } = downReason;
}
