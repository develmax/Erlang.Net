using System.Numerics;
using Erlang.Compiler;

namespace Erlang.Tests;

internal static class PersistentArena416ValuesChecks
{
    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime(1);
        new Parser(GuestStoredFunction407Names.Source).ParseModule().Register(runtime.Modules);
        PreparedBeamImage beam = BeamStoredFunction406Access.Publish(runtime);
        object store = PersistentArena416HostAccess.Create(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        try
        {
            await actor.InvokeAsync(async context =>
            {
                var shared = Term.Tuple(Term.A(PersistentArena416HostNames.Shared), Term.List(Term.I(41), Term.I(6)));
                var resource = runtime.ReferenceResources.CreateBinary(PersistentArena416HostNames.ResourceBytes);
                var buffer = new BitString(Enumerable.Range(0, 2048).Select(index => (byte)index).ToArray());
                BitString sub = buffer.SliceBits(5, 1001);
                var environment = Term.Tuple(shared, resource, sub);
                var ast = (FunctionTerm)await runtime.Modules.Call(
                    context,
                    GuestStoredFunction407Names.ModuleName,
                    GuestStoredFunction407Names.Make,
                    environment
                );
                // Public module entry imports/exports a detached environment.
                // Build the persistent input alias from the actual returned FUN;
                // equal Host input bytes alone do not establish object sharing.
                var actualEnvironment = (TupleTerm)ast.Environment.Single();
                var actualSub = (BitString)actualEnvironment.Items[2];
                PersistentArena416HostAccess.Require(actualEnvironment.Equals(environment) && actualSub.Equals(sub));
                var external = (FunctionTerm)await runtime.Modules.Call(context, GuestStoredFunction407Names.ModuleName,
                    GuestStoredFunction407Names.External);
                var original = (FunctionTerm)await runtime.Modules.Call(
                    context,
                    beam.Name,
                    BeamClosureHostNames.Make,
                    Term.I(BeamClosureHostNames.Capture)
                );
                var restored = (FunctionTerm)runtime.ImportExternalTerm(ExternalTermFormat.Encode(original,
                    new EtfEncodingOptions { FunctionOwner = context.Self })).Term;
                var record = new NativeRecordTerm(
                    Term.A(PersistentArena416HostNames.RecordModule),
                    Term.A(PersistentArena416HostNames.RecordName),
                    true,
                    [new KeyValuePair<Atom, Term>(Term.A(PersistentArena416HostNames.Field), shared)]
                );
                var map = new MapTerm([new KeyValuePair<Term, Term>(Term.I(1), shared),
                    new KeyValuePair<Term, Term>(new FloatTerm(1), Term.I(2))]);
                var aliases = Term.Tuple(
                    shared,
                    shared,
                    new TupleTerm(shared.Items),
                    new TupleTerm(shared.Items)
                );
                var primitives = Term.Tuple(
                    Nil.Value,
                    Term.A(PersistentArena416HostNames.Atom),
                    new Integer(BigInteger.One << 4096),
                    new FloatTerm(-0.0),
                    new Cons(Term.I(1), Term.I(2)),
                    map,
                    record,
                    context.Self,
                    new Pid(PersistentArena416HostNames.ExternalNode, 17, 3),
                    new PortTerm(PersistentArena416HostNames.ExternalNode, 19, 3),
                    new ReferenceTerm(PersistentArena416HostNames.ExternalNode, new uint[] { 1, 3, 7 }, 3),
                    resource,
                    new BitString(PersistentArena416HostNames.ResourceBytes),
                    actualSub
                );
                var functions = Term.Tuple(
                    ast,
                    external,
                    original,
                    restored
                );
                var key = Term.Tuple(shared, Term.A(PersistentArena416HostNames.Key));
                var value = Term.Tuple(aliases, primitives, functions);
                PersistentArena416HostAccess.Require(ReferenceEquals(
                    ((TupleTerm)((FunctionTerm)functions.Items[0]).Environment.Single()).Items[2],
                    primitives.Items[^1]
                ));
                await PersistentArena416HostAccess.Put(
                    store,
                    context,
                    key,
                    value
                );
                var copy = (TupleTerm)PersistentArena416HostAccess.Get(store, context, key);
                PersistentArena416HostAccess.Require(copy.Equals(value) && !ReferenceEquals(copy, value));
                var copyAlias = (TupleTerm)copy.Items[0];
                PersistentArena416HostAccess.Require(ReferenceEquals(copyAlias.Items[0], copyAlias.Items[1])
                    && !ReferenceEquals(copyAlias.Items[0], shared)
                    && !ReferenceEquals(copyAlias.Items[2], copyAlias.Items[3]));
                var pair = (TupleTerm)Cons.Items(PersistentArena416HostAccess.GetAll(store, context)).Single();
                var copiedKey = (TupleTerm)pair.Items[0];
                var copiedValue = (TupleTerm)pair.Items[1];
                PersistentArena416HostAccess.Require(ReferenceEquals(copiedKey.Items[0], ((TupleTerm)copiedValue.Items[0]).Items[0]));
                var copyPrimitives = (TupleTerm)copy.Items[1];
                var copiedSub = (BitString)copyPrimitives.Items[^1];
                PersistentArena416HostAccess.Require(copiedSub.Equals(sub)
                    && PersistentArena416HostAccess.Number(copiedSub, PersistentArena416HostNames.BinaryReferenced)
                        == PersistentArena416HostAccess.Number(sub, PersistentArena416HostNames.BinaryReferenced));
                var copyFunctions = (TupleTerm)copy.Items[2];
                var copiedEnvironment = (TupleTerm)((FunctionTerm)copyFunctions.Items[0]).Environment.Single();
                PersistentArena416HostAccess.Require(ReferenceEquals(copiedEnvironment.Items[2], copiedSub));
                for (int index = 0; index < functions.Items.Count; index++)
                    GuestStoredFunction407Access.Equal((FunctionTerm)functions.Items[index], (FunctionTerm)copyFunctions.Items[index], context.Self);
                PersistentArena416HostAccess.Require((await ((FunctionTerm)copyFunctions.Items[0]).Invoke(context, [Nil.Value])).Equals(Term.Tuple(environment, Nil.Value))
                    && (await ((FunctionTerm)copyFunctions.Items[1]).Invoke(context, [Term.List(Term.I(1), Term.I(2))])).Equals(Term.I(2))
                    && (await ((FunctionTerm)copyFunctions.Items[2]).Invoke(context, [Term.I(BeamClosureHostNames.Argument)])).Equals(Term.I(BeamClosureHostNames.Result)));
                object before = PersistentArena416HostAccess.Info(store, context);
                await PersistentArena416HostAccess.Put(
                    store,
                    context,
                    key,
                    value,
                    true
                );
                PersistentArena416HostAccess.Require(before.Equals(PersistentArena416HostAccess.Info(store, context)));
                Term deep = Nil.Value;
                for (int index = 0; index < PersistentArena416HostNames.Deep; index++) deep = new Cons(Term.I(index), deep);
                await PersistentArena416HostAccess.Put(
                    store,
                    context,
                    Term.A(PersistentArena416HostNames.OtherKey),
                    deep
                );
                PersistentArena416HostAccess.Require(PersistentArena416HostAccess.Get(store, context, Term.A(PersistentArena416HostNames.OtherKey)).Equals(deep));

                return true;
            });
        }
        finally
        {
            await PersistentArena416HostAccess.Close(store);
        }
        PersistentArena416HostAccess.Empty(store);
    }
}
