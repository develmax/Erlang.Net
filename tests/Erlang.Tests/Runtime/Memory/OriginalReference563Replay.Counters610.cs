namespace Erlang.Tests;

internal static partial class OriginalReference563Replay
{
    internal static async Task RunCounters610(
        string repository,
        string admission,
        string admissionSha,
        string metadataCompletion,
        string metadataSha,
        bool owned,
        string output
    )
    {
        repository = Path.GetFullPath(repository);
        var receipt = OriginalCounters610AdmissionReader.Read(repository, admission, admissionSha);
        await RunAdmittedCounters610(
            repository,
            receipt.Worker,
            metadataCompletion,
            metadataSha,
            owned,
            output,
            receipt
        );
    }
}
