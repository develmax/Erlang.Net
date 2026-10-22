using Erlang;

namespace Erlang.Tests;

internal static class ArtifactInspectorHostChecks
{
    internal static Task RetainedOriginalCallbacks()
    {
        byte[] bytes = ArtifactInspectorHostAssets.Read(ArtifactInspectorHostNames.CallbacksResource, ArtifactInspectorHostNames.CallbacksSha256);
        BeamArtifactInfo info = BeamArtifactInspector.Inspect(bytes);
        ArtifactInspectorHostAssets.Require(info.Name == ArtifactInspectorHostNames.CallbacksModule
            && !info.HasOnLoad && info.InputSha256 == ArtifactInspectorHostNames.CallbacksSha256);
        ArtifactInspectorHostAssets.Require(ArtifactInspectorHostAssets.Hash(bytes) == ArtifactInspectorHostNames.CallbacksSha256);
        bytes[0] ^= byte.MaxValue;
        ArtifactInspectorHostAssets.Require(info.Name == ArtifactInspectorHostNames.CallbacksModule
            && info.InputSha256 == ArtifactInspectorHostNames.CallbacksSha256);
        ArtifactInspectorHostAssets.InvalidImage(() => BeamArtifactInspector.Inspect(bytes));

        return Task.CompletedTask;
    }

    internal static async Task InspectionDoesNotPopulateRuntime()
    {
        await using var runtime = new ProcessRuntime();
        int calls = 0;
        runtime.Modules.Register(
            ArtifactInspectorHostNames.SentinelModule,
            ArtifactInspectorHostNames.SentinelFunction,
            0,
            (_, _) =>
        {
            Interlocked.Increment(ref calls);

            return ValueTask.FromResult<Term>(Nil.Value);
        }
        );
        byte[] absent = ArtifactInspectorHostAssets.Export(ArtifactInspectorHostNames.AbsentModule, ArtifactInspectorHostNames.AbsentFunction, 1);
        byte[] image = BeamLiteralExportHostBytes.Image(absent);
        byte[] sentinelImage = BeamLiteralExportHostBytes.Image(ArtifactInspectorHostAssets.Export(
            ArtifactInspectorHostNames.SentinelModule, ArtifactInspectorHostNames.SentinelFunction, 0));
        ArtifactInspectorHostAssets.InvalidEtf(() => runtime.ImportExternalTerm(absent, new EtfDecodeOptions { Safe = true }));
        string expected = ArtifactInspectorHostAssets.Hash(image);
        Task<BeamArtifactInfo>[] work = Enumerable.Range(0, ArtifactInspectorHostNames.ParallelInspections)
            .Select(_ => Task.Run(() => BeamArtifactInspector.Inspect(image))).ToArray();
        BeamArtifactInfo[] results = await Task.WhenAll(work);
        foreach (BeamArtifactInfo result in results)
            ArtifactInspectorHostAssets.Require(result.Name == BeamOpcodeEdgeNames.Module && result.InputSha256 == expected && !result.HasOnLoad);
        _ = BeamArtifactInspector.Inspect(sentinelImage);
        ArtifactInspectorHostAssets.Require(calls == 0 && !runtime.Modules.HasExport(
            ArtifactInspectorHostNames.AbsentModule, ArtifactInspectorHostNames.AbsentFunction, 1));
        ArtifactInspectorHostAssets.InvalidEtf(() => runtime.ImportExternalTerm(absent, new EtfDecodeOptions { Safe = true }));
    }

    internal static async Task DefaultAndRuntimePoliciesRemainDistinct()
    {
        byte[] packet = ArtifactInspectorHostAssets.Export(ArtifactInspectorHostNames.AbsentModule, ArtifactInspectorHostNames.AbsentFunction, 1);
        byte[] image = BeamLiteralExportHostBytes.Image(packet);
        _ = BeamArtifactInspector.Inspect(image);
        ArtifactInspectorHostAssets.InvalidEtf(() => ExternalTermFormat.Decode(packet));
        ArtifactInspectorHostAssets.InvalidImage(() => BeamModule.Prepare(image));
        await using var runtime = new ProcessRuntime();
        ArtifactInspectorHostAssets.InvalidEtf(() => runtime.ImportExternalTerm(packet, new EtfDecodeOptions { Safe = true }));
        PreparedBeamImage prepared = runtime.Modules.PrepareBeam(image);
        ArtifactInspectorHostAssets.Require(!prepared.IsConsumed && prepared.Name == BeamOpcodeEdgeNames.Module);
        Term actualEntry = runtime.ImportExternalTerm(packet, new EtfDecodeOptions { Safe = true }).Term;
        ArtifactInspectorHostAssets.Require(actualEntry is FunctionTerm function
            && runtime.Functions.IsExternalEntry(function) && function.ExternalIdentity == new ExternalFunctionIdentity(
                Term.A(ArtifactInspectorHostNames.AbsentModule), Term.A(ArtifactInspectorHostNames.AbsentFunction), 1));
        ArtifactInspectorHostAssets.Require(!runtime.Modules.HasExport(ArtifactInspectorHostNames.AbsentModule,
            ArtifactInspectorHostNames.AbsentFunction, 1));
    }

    internal static Task MalformedExportRemainsInvalid()
    {
        Atom module = Term.A(ArtifactInspectorHostNames.AbsentModule), function = Term.A(ArtifactInspectorHostNames.AbsentFunction);
        foreach (byte[] packet in new[]
        {
            BeamLiteralExportHostBytes.Export(Term.I(1), function, Term.I(1)),
            BeamLiteralExportHostBytes.Export(module, Term.String(ArtifactInspectorHostNames.AbsentFunction), Term.I(1)),
            BeamLiteralExportHostBytes.Export(module, function, Term.I(-1)),
            BeamLiteralExportHostBytes.Export(module, function, Term.I(ArtifactInspectorHostNames.MaximumArity + 1)),
            BeamLiteralExportHostBytes.Export(module, function, Nil.Value)
        })
            ArtifactInspectorHostAssets.InvalidImage(() => BeamArtifactInspector.Inspect(BeamLiteralExportHostBytes.Image(packet)));
        byte[] valid = ArtifactInspectorHostAssets.Export(ArtifactInspectorHostNames.AbsentModule, ArtifactInspectorHostNames.AbsentFunction, 1);
        ArtifactInspectorHostAssets.InvalidImage(() => BeamArtifactInspector.Inspect(BeamLiteralExportHostBytes.Image(valid[..^1])));

        return Task.CompletedTask;
    }

    internal static Task OnLoadIsMetadataNotExecution()
    {
        byte[] bytes = ArtifactInspectorHostAssets.Read(ArtifactInspectorHostNames.OnLoadResource, ArtifactInspectorHostNames.OnLoadSha256);
        BeamArtifactInfo inspected = BeamArtifactInspector.Inspect(bytes);
        ArtifactInspectorHostAssets.Require(inspected.Name == ArtifactInspectorHostNames.OnLoadModule && inspected.HasOnLoad
            && inspected.InputSha256 == ArtifactInspectorHostNames.OnLoadSha256
            && ArtifactInspectorHostAssets.Hash(bytes) == ArtifactInspectorHostNames.OnLoadSha256);

        return Task.CompletedTask;
    }

    internal static async Task NewFunLiteralStillRefuses()
    {
        await using var runtime = new ProcessRuntime();
        byte[] packet = BeamLiteralExportHostBytes.Local(runtime);
        byte[] image = BeamLiteralExportHostBytes.Image(packet);
        ArtifactInspectorHostAssets.InvalidImage(() => BeamArtifactInspector.Inspect(image));
        ArtifactInspectorHostAssets.InvalidImage(() => runtime.Modules.PrepareBeam(image));
        ArtifactInspectorHostAssets.InvalidEtf(() => ExternalTermFormat.Decode(packet));
    }
}
