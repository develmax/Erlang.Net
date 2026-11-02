using Erlang.Compiler;
using static Erlang.Tests.DebugGuestIdentity377Names;

namespace Erlang.Tests;

internal static class DebugGuestIdentity377Checks
{
    internal static Task Prepare(ProcessRuntime runtime)
    {
        string source = Source.Replace(ExportModuleToken, BeamOpcodeEdgeNames.Module, StringComparison.Ordinal);

        return runtime.InitializeModulesAsync(registry => new Parser(source).ParseModule().Register(registry)).AsTask();
    }

    internal static async Task SourceCase(ProcessRuntime runtime, RuntimeNodeFoundationHostActor actor)
    {
        await actor.InvokeAsync(async context =>
        {
            Term outcome = await context.Runtime.Modules.Call(context, GuestModule, SourceMethod);

            Term yes = Term.A(ErlangBooleanAtoms.True);
            Require(outcome.Equals(Term.Tuple(
                yes,
                yes,
                yes,
                Term.I(0),
                yes,
                yes,
                yes,
                yes,
                yes
            )));
            var actual = (TupleTerm)Read(context, SourceKey);
            Require(actual.Items.Count == 7 && ReferenceEquals(actual.Items[0], actual.Items[1]));
            object literalStorage = Property(actual.Items[0], TupleStorage);
            Require(Property(Property(literalStorage, Owner), Domain) is not null);
            Require(Property(actual.Items[2], TupleStorage) is not null);
            Require(Property(actual.Items[3], FunctionStorage) is not null && Property(actual.Items[4], FunctionStorage) is not null);
            Term portable = context.Dictionary[Term.A(SourceKey)];
            Portable(portable);
            Require(portable.Equals(actual));
            Require(context.Dictionary.Remove(Term.A(SourceKey)));
            CollectMajor(context);

            return Nil.Value;
        });
    }

    internal static async Task ExportCase(ProcessRuntime runtime, RuntimeNodeFoundationHostActor actor)
    {
        await actor.InvokeAsync(async context =>
        {
            Term yes = Term.A(ErlangBooleanAtoms.True);
            Term outcome = await context.Runtime.Modules.Call(
                context,
                GuestModule,
                ExportMethod,
                Term.A(BeamOpcodeEdgeNames.Module)
            );

            Require(outcome.Equals(Term.Tuple(
                yes,
                yes,
                yes,
                Term.I(7),
                Term.I(0),
                yes,
                yes,
                Term.I(7)
            )));
            var actual = (TupleTerm)Read(context, ExportKey);
            Require(ReferenceEquals(actual.Items[0], actual.Items[1]));
            Require(ExportGlobal431Access.HasModuleMarker(runtime, actual.Items[0]));
            foreach (Term child in ((TupleTerm)actual.Items[0]).Items)
                Require(runtime.Functions.IsExternalEntry((FunctionTerm)child) && !ExportGlobal431Access.HasModuleMarker(runtime, child));
            foreach (Term child in ((TupleTerm)actual.Items[2]).Items)
                Require(!runtime.Functions.IsExternalEntry((FunctionTerm)child));
            Term portable = context.Dictionary[Term.A(ExportKey)];
            Portable(portable);
            Require(portable.Equals(actual));
            Require(context.Dictionary.Remove(Term.A(ExportKey)));
            CollectMajor(context);

            return Nil.Value;
        });
    }

    internal static async Task ExportNested395(ProcessContext context)
    {
        foreach (bool fresh in new[] { false, true })
            foreach (bool copy in new[] { false, true })
            {
                Term yes = Term.A(ErlangBooleanAtoms.True);
                Term no = Term.A(ErlangBooleanAtoms.False);
                Term result = await context.Runtime.Modules.Call(
                    context,
                    GuestModule,
                    NestedMethod395,
                    fresh ? yes : no,
                    copy ? yes : no
                );
                Require(result.Equals(Term.Tuple(
                    Term.I(7),
                    Term.I(fresh ? 5 : 3),
                    no,
                    !copy || fresh ? yes : no,
                    Term.I(fresh ? 5 : copy ? 7 : 3),
                    Term.I(2),
                    Term.I(!fresh && !copy ? 0 : 2),
                    yes,
                    yes,
                    yes
                )));
                var held = (TupleTerm)Read(context, NestedKey395);
                Require(held.Items.Count == 2 && Property(held, TupleStorage) is not null);
                var root = (TupleTerm)held.Items[0];
                var copied = (TupleTerm)held.Items[1];
                Require(Property(root, TupleStorage) is not null && Property(copied, TupleStorage) is not null);
                Require(!DebugSharing425Access.Same(context, root, copied));
                Require(DebugSharing425Access.Same(context, copied.Items[0], copied.Items[1]) == (!copy || fresh));
                foreach (Term child in copied.Items)
                    Require(context.Runtime.Functions.IsExternalEntry((FunctionTerm)child) == (!fresh && !copy));
                Require(context.Dictionary[Term.A(NestedKey395)].Equals(held));
                Require(context.Dictionary.Remove(Term.A(NestedKey395)));
                CollectMajor(context);
            }
    }

    internal static async Task OpaqueBoundaries(ProcessRuntime runtime, RuntimeNodeFoundationHostActor actor, bool privateProfile)
    {
        int providerReads = 0, bodyCalls = 0;
        var opaque = new FunctionTerm(
            () =>
 {
     providerReads++;

     return new LocalFunctionIdentity(
         Term.A(GuestModule),
         null,
         0,
         0,
         new BitString(new byte[16])
     );
 },
            0,
            [],
            (_, _) =>
 {
     bodyCalls++;

     return ValueTask.FromResult<Term>(Nil.Value);
 }
        );
        bool rejected = false;
        try
        {
            _ = await actor.InvokeAsync(context => runtime.Modules.Call(
                context,
                GuestModule,
                CopyParameter,
                opaque
            ).AsTask());
        }
        catch (NotSupportedException)
        {
            rejected = true;
        }
        Require(rejected && providerReads == 0 && bodyCalls == 0);
        // Public registration is tested in its own logical VM. The original
        // preloaded facet remains intact in the genuine-source control VM.
        await using var publicRuntime = privateProfile ? OwnedGuest506RuntimeFactory.Create() : new ProcessRuntime();
        await Prepare(publicRuntime);
        await using var publicActor = new RuntimeNodeFoundationHostActor(publicRuntime);
        int callbacks = 0;
        await publicRuntime.InitializeModulesAsync(registry =>
        {
            registry.Register(
                DebugSharing425Names.Module,
                DebugSharing425Names.Copy,
                2,
                (context, arguments) =>
            {
                Require(Property(Property(context, CurrentInvocation), Kind).ToString() == Opaque);
                Portable(arguments[0]);
                callbacks++;

                return ValueTask.FromResult(arguments[0]);
            }
            );
        });
        await publicActor.InvokeAsync(async context =>
        {
            _ = await publicRuntime.Modules.Call(context, GuestModule, OverrideMethod);
            var actual = (TupleTerm)Read(context, OverrideKey);
            object storage = Property(actual, TupleStorage);
            Require(Property(Property(storage, Owner), Domain) is null && callbacks == 1);
            Require(actual.Equals(Term.Tuple(Term.A(GuestModule), Term.I(42))));
            Require(context.Dictionary.Remove(Term.A(OverrideKey)));
            CollectMajor(context);

            return Nil.Value;
        });
    }

    internal static Term Read(ProcessContext context, string key)
    {
        lock (Field(context.Dictionary, Gate))
            return ((Dictionary<Term, Term>)Field(context.Dictionary, Data))[Term.A(key)];
    }

    internal static void CollectMajor(ProcessContext context)
    {
        object heap = Property(context, Heap);
        _ = heap.GetType().GetMethod(Collect, Members)!.Invoke(heap, [true, (ushort)0]);
    }

    internal static object Property(object target, string name) => target.GetType().GetProperty(name, Members)!.GetValue(target)!;

    internal static object Field(object target, string name) => target.GetType().GetField(name, Members)!.GetValue(target)!;

    internal static void Portable(Term graph) => KnownHostGraph384Checks.RequireDetached(graph);

    internal static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }
}
