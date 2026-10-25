namespace Erlang.Differential;

public sealed class OracleReportCheckpoint
{
    private int lastExecuted = -1;
    private int lastFailed = -1;

    public bool ShouldSave(
        int executed,
        int failed,
        bool complete,
        string? infrastructureError
    )
    {
        if (!complete && infrastructureError is null && failed == lastFailed && executed - lastExecuted < OracleReportCheckpointParameters.CaseInterval)
            return false;
        lastExecuted = executed;
        lastFailed = failed;

        return true;
    }
}
