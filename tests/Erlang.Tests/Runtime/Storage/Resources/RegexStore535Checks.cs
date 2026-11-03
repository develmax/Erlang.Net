using System.Reflection;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.RegexStore535Names;

namespace Erlang.Tests;

// Real store APIs and physical ownership controls. The final semantic operation
// invokes the actual search/regex implementation; it is not an oracle comparer.
internal static class RegexStore535Checks
{
    internal static async Task RegexEts()
    {
        await Regex(false);
        await Regex(false, compressed: true);
        await Regex(false, compressed: true, keyIdentity: true);
    }

    internal static Task RegexPersistent() => Regex(true);

    private static Task Regex(bool persistent, bool compressed = false, bool keyIdentity = false) => RegexCustody597Fixture.Run(fixture =>
        Transfer(
            fixture.Runtime,
            fixture.Actor!,
            fixture.Heap,
            fixture.Reference,
            fixture.Backing!,
            RegexCustody597Names.Retired,
            RegexCustody597Names.Freed,
            RegexHost,
            fixture.Release,
            persistent,
            async (context, reference) =>
            {
                Term pattern = Term.Tuple(
                    Term.A(RegexNative411Names.Compiled),
                    Term.I(0),
                    Term.I(0),
                    Term.I(0),
                    reference
                );
                Term actual = await fixture.Engine!.RunAsync(
                    context,
                    RegexNative411Names.Bytes(RegexCustody597Names.Word),
                    pattern,
                    Nil.Value
                );
                Require(actual.Equals(RegexNative411Access.Match(0, MatchLength)));
            },
            compressed,
            keyIdentity
        ));

    private static async Task Transfer(
        ProcessRuntime runtime,
        RuntimeNodeFoundationHostActor sender,
        object sourceHeap,
        ResourceReferenceTerm identity,
        object backing,
        string retiredName,
        string freedName,
        string hostName,
        Action releaseProducer,
        bool persistent,
        Func<ProcessContext, ResourceReferenceTerm, Task> use,
        bool compressed = false,
        bool keyIdentity = false
    )
    {
        var failures = new List<Exception>();
        RuntimeNodeFoundationHostActor? receiver = null;
        IDisposable? sourceFrame = null, destinationFrame = null, returnedHost = null;
        Func<Task>? removeStore = null, exerciseStore = null;
        try
        {
            receiver = new(runtime);
            ProcessContext destinationContext = await receiver.Ready.WaitAsync(Deadline);
            object destinationHeap = Property(destinationContext, Heap);
            sourceFrame = (IDisposable)Call(sourceHeap, Enter)!;
            destinationFrame = (IDisposable)Call(destinationHeap, Enter)!;
            int sourceSlot = (int)Call(sourceFrame, Reserve)!;
            int destinationSlot = (int)Call(destinationFrame, Reserve)!;
            Call(
                sourceFrame,
                Set,
                sourceSlot,
                identity
            );
            var source = (ResourceReferenceTerm)Call(sourceFrame, Read, sourceSlot)!;
            Term key = Term.A(Key);
            Func<Task<Term>> read;
            if (persistent)
            {
                object store = PersistentTerm408HostAccess.Store(runtime);
                removeStore = async () =>
                {
                    await sender.InvokeAsync(context =>
                        ((ValueTask<Term>)PersistentTerm408HostAccess.Call(
                            store,
                            PersistentTerm408HostNames.Erase,
                            context,
                            key
                        )!).AsTask());
                };
                await sender.InvokeAsync(context => PersistentTerm408HostAccess.Put(
                    store,
                    context,
                    key,
                    source
                ));
                read = () => receiver.Invoke(context =>
                    (Term)PersistentTerm408HostAccess.Call(
                        store,
                        PersistentTerm408HostNames.Get,
                        context,
                        key
                    )!);
            }
            else
            {
                object store = EtsFoundation405Access.Get(runtime);
                if (compressed)
                {
                    Term table = await sender.Invoke(context => (Term)EtsFoundation405Access.Invoke(
                        store,
                        EtsKeyKernel407Names.New,
                        context,
                        Term.A(Key),
                        Term.List(Term.A(EtsCompressed441Names.Compressed), Term.A(EtsTraversal416HostNames.Ordered))
                    )!);
                    removeStore = async () =>
                    {
                        await sender.Invoke(context => EtsFoundation405Access.Invoke(
                            store,
                            EtsKeyKernel407Names.DeleteTable,
                            context,
                            table
                        ));
                    };
                    TupleTerm row = keyIdentity
                        ? Term.Tuple(source, key, Term.I(EtsCompressed441Names.CustodyInitial))
                        : Term.Tuple(key, source, Term.I(EtsCompressed441Names.CustodyInitial));
                    await sender.InvokeAsync(context => ((ValueTask<Term>)EtsFoundation405Access.Invoke(
                        store,
                        EtsKeyKernel407Names.Insert,
                        context,
                        table,
                        row
                    )!).AsTask());
                    Term lookupKey = keyIdentity ? identity : key;
                    exerciseStore = async () =>
                    {
                        Term incremented = await sender.InvokeAsync(context => ((ValueTask<Term>)EtsFoundation405Access.Invoke(
                            store,
                            EtsCompressed441Names.CustodyUpdateCounter,
                            context,
                            table,
                            lookupKey,
                            Term.Tuple(Term.I(EtsCompressed441Names.CustodyField), Term.I(EtsCompressed441Names.CustodyIncrement)),
                            false,
                            Nil.Value
                        )!).AsTask());
                        Require(incremented.Equals(Term.I(EtsCompressed441Names.CustodyIncrement)));
                        Term updated = await sender.InvokeAsync(context => ((ValueTask<Term>)EtsFoundation405Access.Invoke(
                            store,
                            EtsCompressed441Names.CustodyUpdateElement,
                            context,
                            table,
                            lookupKey,
                            Term.Tuple(Term.I(EtsCompressed441Names.CustodyField), Term.I(EtsCompressed441Names.CustodyUpdated)),
                            false,
                            Nil.Value
                        )!).AsTask());
                        Require(updated.Equals(Term.A(ErlangBooleanAtoms.True)));
                        Term next = await sender.Invoke(context => (Term)EtsFoundation405Access.Invoke(
                            store,
                            EtsCompressed441Names.CustodyTraverse,
                            context,
                            table,
                            lookupKey,
                            true,
                            false,
                            false
                        )!);
                        Require(next.Equals(Term.A(EtsTraversal416HostNames.End)));
                        TupleTerm unmatched = keyIdentity
                            ? Term.Tuple(identity, key, Term.I(EtsCompressed441Names.CustodyNonmatch))
                            : Term.Tuple(key, identity, Term.I(EtsCompressed441Names.CustodyNonmatch));
                        Term removed = await sender.InvokeAsync(context => ((ValueTask<Term>)EtsFoundation405Access.Invoke(
                            store,
                            EtsCompressed441Names.CustodyRemove,
                            context,
                            table,
                            unmatched,
                            false
                        )!).AsTask());
                        Require(removed.Equals(Term.A(ErlangBooleanAtoms.True)));
                        await sender.Invoke(context =>
                        {
                            object?[] arguments = [context, table, Term.A(EtsCompressed441Names.CustodyData), null];
                            var printed = (Term)EtsFoundation405Access.Invoke(
                                store,
                                EtsCompressed441Names.CustodyInfo,
                                arguments
                            )!;
                            Require(printed.Equals(Term.A(ErlangBooleanAtoms.True))
                                && arguments[EtsCompressed441Names.CustodyPrintedArgument] is byte[] { Length: > 0 });

                            return true;
                        });
                    };
                    read = () => receiver.Invoke(context =>
                    {
                        var restoredRow = (TupleTerm)Cons.Items((Term)EtsFoundation405Access.Invoke(
                            store,
                            EtsKeyKernel407Names.Lookup,
                            context,
                            table,
                            lookupKey
                        )!).Single();
                        Require(restoredRow.Items[EtsCompressed441Names.CustodyField - 1]
                            .Equals(Term.I(EtsCompressed441Names.CustodyUpdated)));

                        return restoredRow.Items[keyIdentity ? 0 : 1];
                    });
                }
                else
                {
                    Term table = await sender.Invoke(context => EtsFoundation405Access.Create(store, context));
                    removeStore = async () =>
                    {
                        await sender.Invoke(context => EtsFoundation405Access.Invoke(
                            store,
                            EtsFoundation405Names.Destroy,
                            context,
                            table
                        ));
                    };
                    long slot = await sender.Invoke(context => EtsFoundation405Access.Store(
                        store,
                        context,
                        table,
                        Term.Tuple(key, source)
                    ));
                    read = () => receiver.Invoke(context =>
                        ((TupleTerm)EtsFoundation405Access.Read(
                            store,
                            context,
                            table,
                            slot
                        )).Items[1]);
                }
            }
            // Do not pre-export the source: the actual store must capture it.
            releaseProducer();
            Call(sourceFrame, Clear, sourceSlot);
            Call(
                sourceHeap,
                CollectMajor,
                true,
                ushort.MaxValue
            );
            Require(!(bool)Property(backing, freedName));
            bool sourceRetired = false;
            try
            {
                _ = source.Words[0];
            }
            catch (ObjectDisposedException)
            {
                sourceRetired = true;
            }
            Require(sourceRetired);
            if (exerciseStore is not null) await exerciseStore();
            var restored = (ResourceReferenceTerm)await read();
            returnedHost = restored.GetType().GetProperty(hostName, Members)!.GetValue(restored) as IDisposable;
            Call(
                destinationFrame,
                Set,
                destinationSlot,
                restored
            );
            var destination = (ResourceReferenceTerm)Call(destinationFrame, Read, destinationSlot)!;
            returnedHost?.Dispose();
            returnedHost = null;
            await removeStore();
            removeStore = null;
            await receiver.InvokeAsync(async context =>
 {
     await use(context, destination);

     return true;
 });
            Require(!(bool)Property(backing, freedName));
            Call(destinationFrame, Clear, destinationSlot);
            Call(
                destinationHeap,
                CollectMajor,
                true,
                ushort.MaxValue
            );
            await ((Task)Property(backing, retiredName)).WaitAsync(Deadline);
            Require((bool)Property(backing, freedName));
            GC.KeepAlive(source);
            GC.KeepAlive(restored);
            GC.KeepAlive(destination);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            if (removeStore is not null)
                try
                {
                    await removeStore();
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            try
            {
                returnedHost?.Dispose();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            try
            {
                destinationFrame?.Dispose();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            try
            {
                sourceFrame?.Dispose();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            if (receiver is not null)
                try
                {
                    await receiver.DisposeAsync().AsTask().WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
        }
        if (failures.Count == 1) ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1) throw new AggregateException(CleanupFailure, failures);
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }
}
