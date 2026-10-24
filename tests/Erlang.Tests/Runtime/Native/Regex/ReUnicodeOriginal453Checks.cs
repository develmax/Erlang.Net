using System.Security.Cryptography;
using System.Text.Json;
using Erlang;

namespace Erlang.Tests;

internal static class ReUnicodeOriginal453Checks
{
    internal static async Task Whole()
    {
        string output = Path.Combine(ReUnicodeOriginal453Names.Output, Guid.NewGuid().ToString(ReUnicodeOriginal453Names.GuidFormat));
        Directory.CreateDirectory(output);
        var failures = new List<object>();
        var receipts = new List<object>();
        await using ProcessRuntime runtime = await RegexThreeOriginal413Checks.Start(RegexThreeOriginal413Names.Suite);
        await runtime.InitializeModulesAsync(registry =>
        {
            var dependencies = new BeamDependencySelection(RegexThreeOriginal413Names.OriginalModules.Concat(
                [RegexThreeOriginal413Names.Erlang, ReUnicodeOriginal453Names.Worker]));
            PreparedBeamImage image = registry.PrepareBeam(ReUnicodeOriginal453Access.Read(ReUnicodeOriginal453Names.WorkerImage), dependencies);
            ReUnicodeOriginal453Access.Require(image.NativePrimitiveExports.Count == 0 && registry.PublishPreparedBeam(image).Status == BeamPublicationStatus.Published);
            foreach (string route in new[] { ReUnicodeOriginal453Names.Run, ReUnicodeOriginal453Names.Stacks })
                ReUnicodeOriginal453Access.Require(ReferenceEquals(registry.SourceOwner(image.Name, route, 0), image));
            ReUnicodeOriginal453Access.Require(registry.SourceOwner(ReUnicodeOriginal453Names.Re, ReUnicodeOriginal453Names.UnicodeCompile, 2) is PreparedBeamImage);
            ReUnicodeOriginal453Access.Require(registry.SourceOwner(ReUnicodeOriginal453Names.Re, ReUnicodeOriginal453Names.UnicodeRun, 3) is PreparedBeamImage);
        });
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term? values = await Capture(
            runtime,
            actor,
            ReUnicodeOriginal453Names.Run,
            ReUnicodeOriginal453Names.Values,
            output,
            failures
        );
        Term? stacks = await Capture(
            runtime,
            actor,
            ReUnicodeOriginal453Names.Stacks,
            ReUnicodeOriginal453Names.StackValues,
            output,
            failures
        );
        int raw = 0, own = 0, authenticated = 0;
        foreach ((string file, string route, Term? actual) in new[]
        {
            (ReUnicodeOriginal453Names.Values, ReUnicodeOriginal453Names.Run, values),
            (ReUnicodeOriginal453Names.StackValues, ReUnicodeOriginal453Names.Stacks, stacks)
        })
        {
            byte[] bytes = ReUnicodeOriginal453Access.Read(file);
            File.WriteAllBytes(Path.Combine(output, ReUnicodeOriginal453Names.ExpectedPrefix + file), bytes);
            Term expected = ExternalTermFormat.Decode(bytes); // Only after both guest routes captured.
            File.WriteAllText(
                Path.Combine(output, ReUnicodeOriginal453Names.ExpectedPrefix + file + ReUnicodeOriginal453Names.JsonSuffix),
                JsonSerializer.Serialize(IoPrintableOriginal404Terms.Typed(expected))
            );
            if (actual is null) continue;
            if (route == ReUnicodeOriginal453Names.Run)
            {
                var count = await ReUnicodeOriginal453Comparison.Rows(
                    runtime,
                    actor,
                    route,
                    Term.A(ReUnicodeOriginal453Names.Default),
                    expected,
                    actual,
                    receipts,
                    failures
                );
                raw += count.Raw;
                own += count.OwnReference;
                authenticated += count.Authenticated;
            }
            else
            {
                Term[] expectedProfiles = Cons.Items(expected).ToArray(), actualProfiles = Cons.Items(actual).ToArray();
                Term[] labels = [Term.A(ReUnicodeOriginal453Names.Default), Term.I(0), Term.I(1), Term.I(2), Term.I(8)];
                ReUnicodeOriginal453Access.Require(expectedProfiles.Length == labels.Length && actualProfiles.Length == labels.Length);
                for (int index = 0; index < labels.Length; index++)
                {
                    var e = (TupleTerm)expectedProfiles[index];
                    var a = (TupleTerm)actualProfiles[index];
                    ReUnicodeOriginal453Access.Require(e.Items.Count == 2 && a.Items.Count == 2 && e.Items[0].Equals(labels[index]) && a.Items[0].Equals(labels[index]));
                    var count = await ReUnicodeOriginal453Comparison.Rows(
                        runtime,
                        actor,
                        route,
                        labels[index],
                        e.Items[1],
                        a.Items[1],
                        receipts,
                        failures
                    );
                    raw += count.Raw;
                    own += count.OwnReference;
                    authenticated += count.Authenticated;
                }
            }
        }
        bool actorClosed = false, runtimeClosed = false;
        try
        {
            await actor.DisposeAsync();
            actorClosed = true;
        }
        catch (Exception error)
        {
            failures.Add(new { ActorCleanup = error.ToString() });
        }
        try
        {
            await runtime.DisposeAsync().AsTask().WaitAsync(ReUnicodeOriginal453Names.Deadline);
            runtimeClosed = true;
        }
        catch (Exception error)
        {
            failures.Add(new { RuntimeCleanup = error.ToString() });
        }
        bool passed = failures.Count == 0 && receipts.Count == ReUnicodeOriginal453Names.Rows && raw == ReUnicodeOriginal453Names.RawRows && own == ReUnicodeOriginal453Names.ReferenceRows && authenticated == own && actorClosed && runtimeClosed;
        File.WriteAllText(
            Path.Combine(output, ReUnicodeOriginal453Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Passed = passed,
                Examined = receipts.Count,
                RawRows = raw,
                OwnReferenceRows = own,
                AuthenticatedSameResourcePatterns = authenticated,
                ActorClosed = actorClosed,
                RuntimeClosed = runtimeClosed,
                FrameFieldsProjected = false,
                ApiCompatibilityPromotion = false,
                RuntimeSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(ProcessRuntime).Assembly.Location))),
                SourceImageSha256 = Convert.ToHexString(SHA256.HashData(ReUnicodeOriginal453Access.Read(ReUnicodeOriginal453Names.WorkerImage))),
                SourceTextSha256 = Convert.ToHexString(SHA256.HashData(ReUnicodeOriginal453Access.Read(ReUnicodeOriginal453Names.WorkerSource))),
                OriginalValuesSha256 = Convert.ToHexString(SHA256.HashData(ReUnicodeOriginal453Access.Read(ReUnicodeOriginal453Names.Values))),
                OriginalStacksSha256 = Convert.ToHexString(SHA256.HashData(ReUnicodeOriginal453Access.Read(ReUnicodeOriginal453Names.StackValues))),
                Comparisons = receipts,
                Failures = failures
            })
        );
        ReUnicodeOriginal453Access.Require(passed);
    }

    private static async Task<Term?> Capture(
        ProcessRuntime runtime,
        RuntimeNodeFoundationHostActor actor,
        string route,
        string file,
        string output,
        List<object> failures
    )
    {
        try
        {
            Term result = await actor.InvokeAsync(async context => await runtime.Modules.Call(context, ReUnicodeOriginal453Names.Worker, route))
                .WaitAsync(ReUnicodeOriginal453Names.Deadline);
            File.WriteAllBytes(Path.Combine(output, ReUnicodeOriginal453Names.ActualPrefix + file), ExternalTermFormat.Encode(result));
            File.WriteAllText(
                Path.Combine(output, ReUnicodeOriginal453Names.ActualPrefix + file + ReUnicodeOriginal453Names.JsonSuffix),
                JsonSerializer.Serialize(IoPrintableOriginal404Terms.Typed(result))
            );

            return result;
        }
        catch (Exception error)
        {
            failures.Add(new { Route = route, Failure = error.ToString(), CompleteReason = error is ErlangException e ? IoPrintableOriginal404Terms.Typed(e.Reason) : null, CompleteTrace = error is ErlangException s ? IoPrintableOriginal404Terms.Typed(s.StackTraceTerm) : null });

            return null;
        }
    }
}
