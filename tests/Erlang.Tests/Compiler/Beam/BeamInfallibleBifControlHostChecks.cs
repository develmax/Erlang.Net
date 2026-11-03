using Erlang;
using static Erlang.Tests.BeamInfallibleBifControlNames;

namespace Erlang.Tests;

internal static class BeamInfallibleBifControlHostChecks
{
    internal static async Task CanonicalOpcodeAndRemoteOverride()
    {
        foreach ((string function, int arity) in new[] { (Get, 1), (Min, 2), (Max, 2) })
        {
            await using var runtime = new ProcessRuntime();
            await DebugSame420HostAccess.Initialize(runtime);
            runtime.Modules.DeleteModule(ErlangModuleNames.Module);
            int overrides = 0;
            runtime.Modules.Register(
                ErlangModuleNames.Module,
                function,
                arity,
                (_, _) =>
            {
                overrides++;
                throw new ErlangException(Term.A(OverrideFailure));
            }
            );
            BeamModule opcode = BeamModule.Prepare(BeamInfallibleBifControlBytes.Image(function, arity));
            BeamModule remote = BeamModule.Prepare(BeamInfallibleBifControlBytes.Remote(function, arity));
            Require(opcode.Supports(BeamOpcodeEdgeNames.Run, arity) && remote.Supports(BeamOpcodeEdgeNames.Run, arity));
            BeamModule identity = BeamModule.Prepare(BeamInfallibleBifControlBytes.Identity(function, arity),
                new BeamDependencySelection([DebugSame420HostNames.Module]));
            Require(identity.Supports(BeamOpcodeEdgeNames.Run, arity));
            object? measuredHeap = null;
            await using var actor = new RuntimeNodeFoundationHostActor(runtime);
            await actor.InvokeAsync(async context =>
            {
                Term first = Term.I(1), second = new FloatTerm(1.0), stored = Term.Tuple(Term.A(OverrideFailure), Term.I(394));
                context.Dictionary[first] = stored;
                Term[] arguments = arity == 1 ? [first] : [first, second];
                Term expected = arity == 1 ? stored : first;
                Term result = await opcode.Invoke(context, BeamOpcodeEdgeNames.Run, arguments);
                Require(result.Equals(expected) && overrides == 0);
                if (arity == 1)
                {
                    // Boxed Host results detach at the public boundary. Observe
                    // actual boxed word identity INSIDE genuine guest execution.
                    Require((await identity.Invoke(context, BeamOpcodeEdgeNames.Run, first))
                        .Equals(Term.A(ErlangBooleanAtoms.True)));
                    // The cold invocation above warms the actual original same
                    // primitive and frame machinery before the bounded window.
                    measuredHeap = BeamBinaryHostAccess.Property(context, HeapProperty);
                    var before = Memory(measuredHeap);
                    Require((await identity.Invoke(context, BeamOpcodeEdgeNames.Run, first))
                        .Equals(Term.A(ErlangBooleanAtoms.True)));
                    var after = Memory(measuredHeap);
                    Require(after.Words == before.Words && after.Collections == before.Collections
                        && after.Frames == before.Frames && after.Blocks == before.Blocks);
                    Require(overrides == 0);
                    Require((await opcode.Invoke(context, BeamOpcodeEdgeNames.Run, Term.I(2))).Equals(Term.A(ProcessDictionaryAtoms.Undefined)));
                }
                else
                {
                    Require(result is Integer);
                    Term firstBox = Term.Tuple(Term.I(394));
                    Term secondBox = Term.Tuple(Term.I(394));
                    Require(!ReferenceEquals(firstBox, secondBox));
                    // Equal distinct boxes distinguish the selected argument;
                    // the observer executes before either public result export.
                    Require((await identity.Invoke(
                        context,
                        BeamOpcodeEdgeNames.Run,
                        firstBox,
                        secondBox
                    ))
                        .Equals(Term.A(ErlangBooleanAtoms.True)));
                    Require(overrides == 0);
                }
                bool failed = false;
                try
                {
                    _ = await remote.Invoke(context, BeamOpcodeEdgeNames.Run, arguments);
                }
                catch (ErlangException error)
                {
                    failed = error.Reason.Equals(Term.A(OverrideFailure));
                }
                Require(failed && overrides == 1);
                failed = false;
                try
                {
                    _ = await runtime.Modules.Call(
                        context,
                        ErlangModuleNames.Module,
                        function,
                        arguments
                    );
                }
                catch (ErlangException error)
                {
                    failed = error.Reason.Equals(Term.A(OverrideFailure));
                }
                Require(failed && overrides == 2 && context.Mailbox.Count == 0);

                return true;
            });
            if (measuredHeap is not null)
            {
                await actor.DisposeAsync();
                var retired = Memory(measuredHeap);
                Require(retired.Words == 0 && retired.Blocks == 0 && retired.Frames == 0);
            }
        }
    }

    private static (long Words, long Collections, long Frames, long Blocks) Memory(object heap)
    {
        lock (BeamBinaryHostAccess.Property(heap, HeapGateProperty))
        {
            object snapshot = BeamBinaryHostAccess.Property(heap, HeapSnapshotProperty);
            object generations = BeamBinaryHostAccess.Property(heap, GenerationsProperty);

            return (Convert.ToInt64(BeamBinaryHostAccess.Property(snapshot, HeapWordsProperty)),
                Convert.ToInt64(BeamBinaryHostAccess.Property(generations, CollectionsProperty)),
                Convert.ToInt64(BeamBinaryHostAccess.Property(snapshot, HeapFramesProperty)),
                Convert.ToInt64(BeamBinaryHostAccess.Property(snapshot, HeapBlocksProperty)));
        }
    }

    internal static Task FallibleGcAndOpaqueRefusals()
    {
        foreach (byte[] bytes in new[]{
            BeamInfallibleBifControlBytes.Image(Length,1),
            BeamInfallibleBifControlBytes.Image(TupleSize,1),
            BeamInfallibleBifControlBytes.Image(Get,1,gc:true),
            BeamInfallibleBifControlBytes.Image(Get,1,opaque:true),
            BeamInfallibleBifControlBytes.Image(Get,1,missingArgument:true)})
        {
            bool rejected = false;
            try
            {
                rejected = !BeamModule.Prepare(bytes).Supports(BeamOpcodeEdgeNames.Run, 1);
            }
            catch (Exception error) when (error is InvalidDataException or NotSupportedException)
            {
                rejected = true;
            }
            Require(rejected);
        }

        return Task.CompletedTask;
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }
}
