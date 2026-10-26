using System.Text;

namespace Erlang.HaltProbe;

public static class HaltSubprocessVerifier
{
    public static IReadOnlyList<string> Check(HaltSubprocessContract contract, HaltSubprocessResult result)
    {
        var errors = new List<string>();
        if (result.TimedOut || result.InfrastructureError is not null)
            errors.Add(result.InfrastructureError ?? HaltProcessParameters.TimedOut);
        int? expectedExit = OperatingSystem.IsWindows() ? contract.ExpectedWindowsExit : contract.ExpectedPosixExit;
        if (expectedExit is not null && result.RawExitCode != expectedExit)
            errors.Add("raw-exit-code:" + result.RawExitCode);
        if (!result.StandardOutput.SequenceEqual(contract.ExpectedStandardOutput))
            errors.Add("raw-standard-output");
        IReadOnlyList<byte>? expectedError = OperatingSystem.IsWindows() ? contract.ExpectedWindowsStandardError : contract.ExpectedPosixStandardError;
        if (expectedError is not null && !result.StandardError.SequenceEqual(expectedError))
            errors.Add("raw-standard-error");
        foreach (string marker in contract.ForbiddenMarkers)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(marker);
            if (result.StandardOutput.ToArray().AsSpan().IndexOf(bytes) >= 0 || result.StandardError.ToArray().AsSpan().IndexOf(bytes) >= 0)
                errors.Add("continuation-marker:" + marker);
        }
        if (result.Backend != HaltProcessBackend.Original)
        {
            string backend = result.Backend == HaltProcessBackend.Compiled ? HaltProbeNames.Compiled : HaltProbeNames.Interpreted;
            if (result.Evidence?.Backend != backend || !result.Evidence.SourceSha256.SequenceEqual(contract.Sources.Select(source => source.SourceSha256)))
                errors.Add("actual-sdk-source-identity");
        }
        if (result.Backend == HaltProcessBackend.Original)
        {
            string stage = contract.ExpectedObservation == HaltProcessObservation.HaltRequest ? HaltProcessParameters.ExecuteStage : HaltProcessParameters.ReturnStage;
            if (result.Stage != stage)
                errors.Add("native-stage:" + result.Stage);
        }
        else if (contract.ExpectedObservation == HaltProcessObservation.HaltRequest)
        {
            if (result.Evidence?.Stage != HaltProbeNames.RequestObserved || result.Evidence.RequestKind != contract.ExpectedRequestKind || result.Evidence.LogicalStatus != contract.ExpectedLogicalStatus)
                errors.Add("actual-sdk-halt-request");
        }
        else if (result.Evidence?.Stage != HaltProbeNames.Returned || result.Evidence.EntryExitReason != ProcessExitReasons.Normal)
            errors.Add("actual-sdk-entry-exit");

        return errors.AsReadOnly();
    }
}
