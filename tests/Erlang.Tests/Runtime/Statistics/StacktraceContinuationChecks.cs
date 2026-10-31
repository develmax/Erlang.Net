using System.Reflection;
using System.Security.Cryptography;

namespace Erlang.Tests;

internal static class StacktraceContinuationChecks
{
    internal static async Task Run()
    {
        byte[] beam = Read(StacktraceContinuationNames.BeamResource, StacktraceContinuationNames.BeamSha256);
        var expected = (TupleTerm)ExternalTermFormat.Decode(Read(
            StacktraceContinuationNames.OutcomeResource, StacktraceContinuationNames.OutcomeSha256));
        CheckShape(expected.Items[0], StacktraceContinuationNames.SameFrameCount, false);
        CheckShape(expected.Items[1], StacktraceContinuationNames.DistinctFrameCount, true);
        foreach (bool owned in new[] { false, true })
        {
            var options = new ProcessRuntimeOptions();
            typeof(ProcessRuntimeOptions).GetProperty(StacktraceContinuationNames.Profile,
                BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(options, owned);
            await using var runtime = new ProcessRuntime(
                ProcessNodeDefaults.Local,
                null,
                StacktraceContinuationNames.ProcessCapacity,
                null,
                StacktraceContinuationNames.SchedulerCount,
                options
            );
            await runtime.InitializeModulesAsync(registry =>
            {
                PreparedBeamImage image = registry.PrepareBeam(beam, new BeamDependencySelection(
                    [StacktraceContinuationNames.Module, ErlangModuleNames.Module]));
                Require(registry.PublishPreparedBeam(image).Status == BeamPublicationStatus.Published);
            });
            await using var actor = new RuntimeNodeFoundationHostActor(runtime);
            foreach ((string mode, int index) in new[] { (StacktraceContinuationNames.Same, 0), (StacktraceContinuationNames.Distinct, 1) })
            {
                Term actual = await actor.InvokeAsync(async context => await runtime.Modules.Call(
                    context,
                    StacktraceContinuationNames.Module,
                    StacktraceContinuationNames.Entry,
                    Term.A(mode)
                ));
                Require(actual.Equals(expected.Items[index]));
                CheckShape(actual, index == 0 ? StacktraceContinuationNames.SameFrameCount
                    : StacktraceContinuationNames.DistinctFrameCount, index != 0);
            }
        }
    }

    private static void CheckShape(Term outcome, int expectedCount, bool identicalRenderedFrames)
    {
        var complete = (TupleTerm)outcome;
        Term stack = Cons.Items(complete.Items[2]).Cast<TupleTerm>().Single(value =>
            value.Items[0].Equals(Term.A(StacktraceContinuationNames.Stacktrace))).Items[1];
        Term[] frames = Cons.Items(stack).ToArray();
        Require(frames.Length == expectedCount);
        if (identicalRenderedFrames)
            Require(frames.All(frame => frame.Equals(frames[0])));
    }

    private static byte[] Read(string resource, string expected)
    {
        using Stream stream = typeof(StacktraceContinuationChecks).Assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidDataException(StacktraceContinuationNames.Failure);
        using var output = new MemoryStream();
        stream.CopyTo(output);
        byte[] value = output.ToArray();
        Require(Convert.ToHexString(SHA256.HashData(value)) == expected);

        return value;
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(StacktraceContinuationNames.Failure);
    }
}
