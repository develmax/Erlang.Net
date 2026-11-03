namespace Erlang.Tests;

internal static partial class OriginalReference563Replay
{
    internal static async Task RunAtomics588(
        string repository,
        string admission,
        string admissionSha,
        string metadataCompletion,
        string metadataSha,
        bool owned,
        string output
    )
    {
        repository = System.IO.Path.GetFullPath(repository);
        OriginalAtomics588Admission receipt = OriginalAtomics588AdmissionReader.Read(repository, admission, admissionSha);
        await RunAdmitted588(
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
