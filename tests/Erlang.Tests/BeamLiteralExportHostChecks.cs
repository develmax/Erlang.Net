using Erlang;

namespace Erlang.Tests;

internal static class BeamLiteralExportHostChecks
{
    internal static async Task Pointer()
    {
        await using var runtime = new ProcessRuntime();
        byte[] packet = Export();
        PreparedBeamImage prepared = runtime.Modules.PrepareBeam(BeamLiteralExportHostBytes.Image(packet));
        Require(!runtime.Modules.HasExport(BeamLiteralExportHostNames.Module, BeamLiteralExportHostNames.Function, 1));
        Require(runtime.Modules.PublishPreparedBeam(prepared).Status == BeamPublicationStatus.Published);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        FunctionTerm function = await actor.InvokeAsync(async context => (FunctionTerm)await runtime.Modules.Call(
            context,
            BeamOpcodeEdgeNames.Module,
            BeamOpcodeEdgeNames.Run,
            []
        ));
        Require(function.ExternalIdentity == Identity());
        bool undefined = false;
        try
        {
            await actor.InvokeAsync(context => function.Invoke(context, [Nil.Value]).AsTask());
        }
        catch (ErlangException error)
        {
            undefined = error.Reason.Equals(Term.A(ErlangErrorReasons.UndefinedFunction));
        }
        Require(undefined);
        int calls = 0;
        runtime.Modules.Register(
            BeamLiteralExportHostNames.Module,
            BeamLiteralExportHostNames.Function,
            1,
            (_, args) =>
        {
            calls++;

            return ValueTask.FromResult(args[0]);
        }
        );
        Term value = Term.Tuple(Term.I(382));
        Require(ReferenceEquals(await actor.InvokeAsync(context => function.Invoke(context, [value]).AsTask()), value) && calls == 1);
        _ = runtime.Modules.DeleteModule(BeamLiteralExportHostNames.Module);
        runtime.Modules.Register(
            BeamLiteralExportHostNames.Module,
            BeamLiteralExportHostNames.Function,
            1,
            (_, _) => ValueTask.FromResult<Term>(Nil.Value)
        );
        Require(await actor.InvokeAsync(context => function.Invoke(context, [value]).AsTask()) is Nil);
    }

    internal static async Task Interning()
    {
        await using var first = new ProcessRuntime();
        await using var second = new ProcessRuntime();
        var encodedFunction = new FunctionTerm(Identity(), (_, _) => throw new InvalidOperationException(BeamLiteralExportHostNames.Failure));
        byte[] packet = ExternalTermFormat.Encode(Term.Tuple(encodedFunction, encodedFunction));
        byte[] image = BeamLiteralExportHostBytes.Image(packet);
        TupleTerm one = (TupleTerm)await Literal(first, image);
        TupleTerm two = (TupleTerm)await Literal(second, image);
        Require(ReferenceEquals(one.Items[0], one.Items[1]) && ReferenceEquals(two.Items[0], two.Items[1]));
        Require(!ReferenceEquals(one.Items[0], two.Items[0]));
        Require(first.Functions.IsExternalEntry((FunctionTerm)one.Items[0]) && !second.Functions.IsExternalEntry((FunctionTerm)one.Items[0]));
    }

    internal static async Task GeneralPolicy()
    {
        byte[] packet = Export();
        bool plainRefused = false;
        try
        {
            _ = ExternalTermFormat.Decode(packet);
        }
        catch (ErlangException error)
        {
            plainRefused = error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument));
        }
        Require(plainRefused);
        await using var runtime = new ProcessRuntime();
        bool safeRefused = false;
        try
        {
            _ = runtime.ImportExternalTerm(packet, new EtfDecodeOptions { Safe = true });
        }
        catch (ErlangException error)
        {
            safeRefused = error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument));
        }
        Require(safeRefused);
        _ = runtime.Modules.PrepareBeam(BeamLiteralExportHostBytes.Image(packet));
        // OTP safe decoding checks export-table entry existence, including already-created stubs.
        Term loaded = runtime.ImportExternalTerm(packet, new EtfDecodeOptions { Safe = true }).Term;
        Require(loaded is FunctionTerm stub
            && stub.ExternalIdentity == Identity() && runtime.Functions.IsExternalEntry(stub));
        Require(ReferenceEquals(runtime.ImportExternalTerm(packet).Term, loaded));
        bool standaloneRefused = false;
        try
        {
            _ = BeamModule.Prepare(BeamLiteralExportHostBytes.Image(packet));
        }
        catch (InvalidDataException)
        {
            standaloneRefused = true;
        }
        Require(standaloneRefused);
    }

    internal static async Task Malformed()
    {
        await using var runtime = new ProcessRuntime();
        Term module = Term.A(BeamLiteralExportHostNames.Module), function = Term.A(BeamLiteralExportHostNames.Function);
        foreach (byte[] packet in new[]
        {
            BeamLiteralExportHostBytes.Export(Term.I(1), function, Term.I(1)),
            BeamLiteralExportHostBytes.Export(module, Term.String(BeamLiteralExportHostNames.Function), Term.I(1)),
            BeamLiteralExportHostBytes.Export(module, function, Term.I(-1)),
            BeamLiteralExportHostBytes.Export(module, function, Term.I(256)),
            BeamLiteralExportHostBytes.Export(module, function, Nil.Value)
        })
        {
            bool refused = false;
            try
            {
                _ = runtime.Modules.PrepareBeam(BeamLiteralExportHostBytes.Image(packet));
            }
            catch (InvalidDataException)
            {
                refused = true;
            }
            Require(refused);
        }
    }

    internal static async Task LocalStillRequiresFactory()
    {
        await using var runtime = new ProcessRuntime();
        byte[] packet = BeamLiteralExportHostBytes.Local(runtime);
        bool refused = false;
        try
        {
            _ = runtime.Modules.PrepareBeam(BeamLiteralExportHostBytes.Image(packet));
        }
        catch (InvalidDataException)
        {
            refused = true;
        }
        Require(refused);
    }

    private static byte[] Export() => BeamLiteralExportHostBytes.Export(
        Term.A(BeamLiteralExportHostNames.Module),
        Term.A(BeamLiteralExportHostNames.Function),
        Term.I(BeamLiteralExportHostNames.Arity)
    );

    private static ExternalFunctionIdentity Identity() => new(Term.A(BeamLiteralExportHostNames.Module), Term.A(BeamLiteralExportHostNames.Function), BeamLiteralExportHostNames.Arity);

    private static async Task<Term> Literal(ProcessRuntime runtime, byte[] image)
    {
        PreparedBeamImage prepared = runtime.Modules.PrepareBeam(image);
        Require(runtime.Modules.PublishPreparedBeam(prepared).Status == BeamPublicationStatus.Published);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);

        return await actor.InvokeAsync(context => runtime.Modules.Call(
            context,
            BeamOpcodeEdgeNames.Module,
            BeamOpcodeEdgeNames.Run,
            []
        ).AsTask());
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(BeamLiteralExportHostNames.Failure);
    }
}
