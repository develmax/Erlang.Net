using Erlang;

namespace Erlang.Tests;

internal static class IoProtocolHostChecks
{
    public static async Task ConcurrentOutput()
    {
        using var output = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
        await using var runtime = new ProcessRuntime(output: output);
        var writers = Enumerable.Range(0, IoProtocolCheckParameters.ConcurrentWriters).Select(index => runtime.Spawn(async context =>
        {
            Term reply = await runtime.Modules.Call(
                context,
                IoModuleNamesForChecks.Module,
                IoModuleNamesForChecks.Format,
                Term.String(IoProtocolCheckParameters.Format),
                Cons.From([Term.I(index)])
            );
            if (reply is not Atom { Name: IoResultAtoms.Ok })
                throw new InvalidOperationException(IoProtocolCheckDiagnostics.Output);

            return Term.I(0);
        })).ToArray();
        Term[] reasons = await Task.WhenAll(writers.Select(writer => writer.Completion)).WaitAsync(TimeSpan.FromSeconds(IoProtocolCheckParameters.TimeoutSeconds));
        if (reasons.Any(reason => reason is not Atom { Name: ProcessExitReasons.Normal }))
            throw new InvalidOperationException(IoProtocolCheckDiagnostics.Output);
        string[] lines = output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        string[] expected = Enumerable.Range(0, IoProtocolCheckParameters.ConcurrentWriters).Select(index => index.ToString(System.Globalization.CultureInfo.InvariantCulture)).Order(StringComparer.Ordinal).ToArray();
        if (!lines.Order(StringComparer.Ordinal).SequenceEqual(expected))
            throw new InvalidOperationException(IoProtocolCheckDiagnostics.Output);
    }
}
