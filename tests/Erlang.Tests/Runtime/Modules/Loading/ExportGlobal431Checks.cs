namespace Erlang.Tests;

internal static class ExportGlobal431Checks
{
    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(ExportGlobal431Names.Failure);
    }

    private static ValueTask<Term> Call(ProcessContext context, string function, params Term[] arguments)
        => context.Runtime.Modules.Call(
            context,
            ExportGlobal431Names.Debug,
            function,
            arguments
        );

    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry => RuntimeBootstrapDebugSharing425.Register(runtime, registry));
        await using var other = new ProcessRuntime();
        await other.InitializeModulesAsync(_ => { });
        var identity = new ExternalFunctionIdentity(Term.A(ExportGlobal431Names.Runtime), Term.A(ExportGlobal431Names.Function), 0);
        FunctionTerm global = runtime.Functions.External(runtime, identity);
        FunctionTerm separate = other.Functions.External(other, identity);
        Require(runtime.Functions.IsExternalEntry(global) && !runtime.Functions.IsExternalEntry(separate)
            && other.Functions.IsExternalEntry(separate) && !ReferenceEquals(global, separate));
        Require(!ExportGlobal431Access.HasModuleMarker(runtime, global));
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        // Before any BEAM literal has referenced this function: actual permanent
        // export allocation already determines global storage provenance.
        await actor.InvokeAsync(async context =>
        {
            Require((await Call(context, ExportGlobal431Names.Flat, global)).Equals(Term.I(2)));
            Require((await Call(context, ExportGlobal431Names.Shared, global)).Equals(Term.I(0)));

            return Nil.Value;
        });
        FunctionTerm dynamic = (FunctionTerm)await actor.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            ExportGlobal431Names.Runtime,
            ExportGlobal431Names.MakeFun,
            identity.Module,
            identity.Function,
            Term.I(identity.Arity)
        ));
        FunctionTerm decoded = (FunctionTerm)runtime.ImportExternalTerm(ExternalTermFormat.Encode(global)).Term;
        Require(ReferenceEquals(global, dynamic) && ReferenceEquals(global, decoded));

        byte[] image = BeamLiteralExportHostBytes.Image(ExternalTermFormat.Encode(Term.Tuple(global, global)));
        PreparedBeamImage prepared = runtime.Modules.PrepareBeam(image);
        Require(runtime.Modules.PublishPreparedBeam(prepared).Status == BeamPublicationStatus.Published);
        TupleTerm literal = (TupleTerm)await actor.InvokeAsync(async context => await runtime.Modules.Call(context, BeamOpcodeEdgeNames.Module, BeamOpcodeEdgeNames.Run));
        Require(ReferenceEquals(global, literal.Items[0]) && ReferenceEquals(global, literal.Items[1])
            && runtime.Functions.IsExternalEntry((FunctionTerm)literal.Items[0]));
        Require(!ExportGlobal431Access.HasModuleMarker(runtime, global));
        Require(ExportGlobal431Access.HasModuleMarker(runtime, literal));
        await actor.InvokeAsync(async context =>
        {
            FunctionTerm keep = (FunctionTerm)await Call(
                context,
                ExportGlobal431Names.Copy,
                global,
                Term.A(ExportGlobal431Names.False)
            );
            FunctionTerm copy = (FunctionTerm)await Call(
                context,
                ExportGlobal431Names.Copy,
                global,
                Term.A(ExportGlobal431Names.True)
            );
            FunctionTerm copyFalse = (FunctionTerm)await Call(
                context,
                ExportGlobal431Names.Copy,
                copy,
                Term.A(ExportGlobal431Names.False)
            );
            FunctionTerm copyTrue = (FunctionTerm)await Call(
                context,
                ExportGlobal431Names.Copy,
                copy,
                Term.A(ExportGlobal431Names.True)
            );
            Require(ReferenceEquals(keep, global) && !ReferenceEquals(copy, global)
                && !ReferenceEquals(copy, copyFalse) && !ReferenceEquals(copy, copyTrue));
            foreach (var fresh in new[] { copy, copyFalse, copyTrue })
            {
                Require(!runtime.Functions.IsExternalEntry(fresh) && fresh.Equals(global)
                    && !DebugSharing425Access.Same(context, global, fresh));
                Require((await Call(context, ExportGlobal431Names.Flat, fresh)).Equals(Term.I(2))
                    && (await Call(context, ExportGlobal431Names.Shared, fresh)).Equals(Term.I(2)));
                Require((await fresh.Invoke(context, [])).Equals(context.Self));
            }
            // EXPORT_EXT decoding deliberately returns the canonical export slot;
            // it does not mutate the provenance of the original fresh wrapper.
            FunctionTerm imported = (FunctionTerm)runtime.ImportExternalTerm(ExternalTermFormat.Encode(copy)).Term;
            Require(ReferenceEquals(imported, global) && !runtime.Functions.IsExternalEntry(copy));
            Require((await Call(context, ExportGlobal431Names.Shared, global)).Equals(Term.I(0))
                && (await Call(context, ExportGlobal431Names.Shared, copy)).Equals(Term.I(2)));
            await Nested(
                context,
                literal,
                true,
                false
            );
            await Nested(
                context,
                Term.Tuple(global, global),
                false,
                false
            );
            await Nested(
                context,
                Term.Tuple(copy, copy),
                false,
                true
            );

            return Nil.Value;
        });
    }

    private static async Task Nested(
        ProcessContext context,
        TupleTerm root,
        bool literal,
        bool freshChildren
    )
    {
        Require((await Call(context, ExportGlobal431Names.Flat, root)).Equals(Term.I(7)));
        Require((await Call(context, ExportGlobal431Names.Shared, root)).Equals(Term.I(literal ? 0 : freshChildren ? 5 : 3)));
        foreach (bool copyLiterals in new[] { false, true })
        {
            var copied = (TupleTerm)await Call(
                context,
                ExportGlobal431Names.Copy,
                root,
                Term.A(copyLiterals ? ExportGlobal431Names.True : ExportGlobal431Names.False)
            );
            bool rootSame = DebugSharing425Access.Same(context, root, copied);
            bool childSame = DebugSharing425Access.Same(context, copied.Items[0], copied.Items[1]);
            Require(rootSame == (literal && !copyLiterals));
            Require(childSame == (!copyLiterals || freshChildren));
            long shared = literal && !copyLiterals ? 0 : freshChildren ? 5 : copyLiterals ? 7 : 3;
            Require((await Call(context, ExportGlobal431Names.Shared, copied)).Equals(Term.I(shared)));
            foreach (Term value in copied.Items)
            {
                var function = (FunctionTerm)value;
                Require(context.Runtime.Functions.IsExternalEntry(function) == (!freshChildren && !copyLiterals));
                Require((await Call(context, ExportGlobal431Names.Flat, function)).Equals(Term.I(2)));
                Require((await Call(context, ExportGlobal431Names.Shared, function)).Equals(Term.I(!freshChildren && !copyLiterals ? 0 : 2)));
                Require((await function.Invoke(context, [])).Equals(context.Self));
            }
        }
    }
}
