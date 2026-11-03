using System.Reflection;
using Erlang.Compiler;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.NativeRecordDefaults313Names;

namespace Erlang.Tests;

internal static class NativeRecordDefaults313Checks
{
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    internal static async Task Run()
    {
        bool illegalDefaultRejected = false;
        try
        {
            _ = new Parser(IllegalDefault).ParseModule();
        }
        catch (CompileException error) when (error.Code == DefaultDiagnostic)
        {
            illegalDefaultRejected = true;
        }
        Require(illegalDefaultRejected);
        var failures = new List<Exception>();
        foreach (bool privateProfile in new[] { false, true })
            await NativeRecordDefaults331Scope.Attempt(
                failures,
                privateProfile ? NativeRecordDefaults331Names.SourcePrivate : NativeRecordDefaults331Names.SourceOrdinary,
                scope => Source(privateProfile, scope)
            );
        foreach (bool privateProfile in new[] { false, true })
            await NativeRecordDefaults331Scope.Attempt(
                failures,
                privateProfile ? NativeRecordDefaults331Names.BeamPrivate : NativeRecordDefaults331Names.BeamOrdinary,
                scope => NativeRecordDefaults313BeamChecks.Run(privateProfile, scope)
            );
        if (failures.Count != 0) throw new AggregateException(Failure, failures);
    }

    private static async Task Source(bool privateProfile, NativeRecordDefaults331Scope scope)
    {
        ProcessRuntime runtime = scope.Own(OwnedGuest506RuntimeFactory.Create(privateProfile));
        await runtime.InitializeModulesAsync(_ => { });
        ModuleDefinition first = new Parser(SourceV1).ParseModule();
        ModuleDefinition second = new Parser(SourceV2).ParseModule();
        ModuleDefinition consumer = new Parser(SourceConsumer).ParseModule();
        PreparedSourceModule819 oldPrepared = scope.Own(first.PrepareSourceModule819(runtime.Modules));
        PreparedSourceModule819 nextPrepared = scope.Own(second.PrepareSourceModule819(runtime.Modules));
        PreparedSourceModule819 importedPrepared = scope.Own(consumer.PrepareSourceModule819(runtime.Modules));
        await runtime.InitializeModulesAsync(_ =>
 {
     oldPrepared.Register();
     importedPrepared.Register();
 });
        ModuleCodeImage oldImage = runtime.Modules.CurrentCodeImage(Owner)!;
        object oldDomain = Property(oldImage, SourceLiteral819Names.ImageDomain);
        Task oldRetirement = (Task)Property(oldDomain, SourceLiteral819Names.Retirement);
        Require(Property(first.NativeRecords[0].Fields[0].Value!, SourceLiteral819Names.Storage) is null);
        var actor = scope.Own(new RuntimeNodeFoundationHostActor(runtime));
        await actor.InvokeAsync(async context =>
        {
            await Store(context, OldKey, new Expr.Call(Owner, Make, []));
            await Store(context, FunKey, new Expr.Call(Owner, Capture, []));
            Require(ReferenceEquals(Domain(PayloadOf(Read(context, OldKey))), oldDomain));

            return true;
        });
        await runtime.InitializeModulesAsync(registry =>
        {
            registry.DeleteModule(Owner);
            nextPrepared.Register();
            registry.Register(
                OpaqueModule,
                OpaqueFunction,
                0,
                async (context, _) =>
            {
                Require(Property(Property(context, Invocation), Binding).ToString() == OpaqueBinding);
                Term portable = await runtime.Modules.Call(
                    context,
                    Records,
                    GetDefinition,
                    Term.A(Owner),
                    Term.A(Record)
                );
                RequirePortableDefinition(portable, second.NativeRecords[0].Fields[0].Value!);

                return portable;
            }
            );
        });
        ModuleCodeImage newImage = runtime.Modules.CurrentCodeImage(Owner)!;
        object newDomain = Property(newImage, SourceLiteral819Names.ImageDomain);
        Require(!ReferenceEquals(oldDomain, newDomain));
        await actor.InvokeAsync(async context =>
        {
            await Store(context, NewKey, new Expr.Call(Owner, Make, []));
            await Store(context, ExternalKey, new Expr.Call(Consumer, Make, []));
            await Store(
                context,
                CapturedKey,
                new Expr.Apply(new Expr.Call(SourceLiteral819Names.DictionaryModule, Get, [new Expr.Literal(Term.A(FunKey))]), [])
            );
            await Store(
                context,
                DefinitionKey,
                new Expr.Call(Records, GetDefinition, [new Expr.Literal(Term.A(Owner)), new Expr.Literal(Term.A(Record))])
            );
            Term local = PayloadOf(Read(context, NewKey));
            Term external = PayloadOf(Read(context, ExternalKey));
            Term old = PayloadOf(Read(context, CapturedKey));
            Require(ReferenceEquals(Domain(local), newDomain) && ReferenceEquals(Domain(external), newDomain));
            Require(ReferenceEquals(local, external));
            Require(ReferenceEquals(Domain(old), oldDomain) && ReferenceEquals(old, PayloadOf(Read(context, OldKey))));
            Require(((TupleTerm)local).Items[0].Equals(Term.A(CurrentTag)) && ((TupleTerm)old).Items[0].Equals(Term.A(OldTag)));
            var definition = (TupleTerm)Read(context, DefinitionKey);
            var payloadPair = (TupleTerm)Cons.Items(definition.Items[1]).First(item => item is TupleTerm pair && pair.Items[0].Equals(Term.A(Payload)));
            Require(ReferenceEquals(payloadPair.Items[1], local));
            Term hostDefinition = await runtime.Modules.Call(
                context,
                Records,
                GetDefinition,
                Term.A(Owner),
                Term.A(Record)
            );
            RequirePortableDefinition(hostDefinition, local);
            await Store(context, OpaqueKey, new Expr.Call(OpaqueModule, OpaqueFunction, []));
            Term opaqueDefault = DefinitionDefault(Read(context, OpaqueKey));
            Require(opaqueDefault.Equals(local) && !ReferenceEquals(opaqueDefault, local) && !ReferenceEquals(Domain(opaqueDefault), newDomain));
            await BorrowedDefinition(context, runtime);
            await Reject(context, [], NoValue);
            await Reject(context, [new RecordField(UnknownField, new Expr.Literal(Nil.Value))], BadField);
            Term explicitValue = Term.Tuple(Term.A(Explicit), new BitString(new byte[] { 7, 8 }));
            Term explicitRecord = await Execution.EvaluateAsync(
                new NativeRecordExpression(
                    NativeRecordOperation.Construct,
                    Owner,
                    Record,
                    Consumer,
                    true,
                    null,
                    [new RecordField(Payload, new Expr.Literal(explicitValue)), new RecordField(Optional, new Expr.Literal(Nil.Value))]
                ),
                context
            );
            Require(PayloadOf(explicitRecord).Equals(explicitValue));
            // Old code purge can retire its code owner while actual returned
            // omitted defaults retain the precise original literal area.
            Require(context.Dictionary.Remove(Term.A(FunKey)));
            Require(await (ValueTask<bool>)Call(
                runtime,
                SourceLiteral819Names.Purge,
                context,
                Owner
            )!);
            Require(!oldRetirement.IsCompleted && old.Equals(PayloadOf(Read(context, OldKey))));
            Require(context.Dictionary.Remove(Term.A(OldKey)) && context.Dictionary.Remove(Term.A(CapturedKey)));
            Call(
                Property(context, SourceLiteral819Names.Heap),
                SourceLiteral819Names.Collect,
                true,
                (ushort)0
            );

            return true;
        });
        await oldRetirement.WaitAsync(Deadline);
        Task newRetirement = (Task)Property(newDomain, SourceLiteral819Names.Retirement);
        runtime.Modules.DeleteModule(Owner);
        await actor.InvokeAsync(async context =>
        {
            Require(await (ValueTask<bool>)Call(
                runtime,
                SourceLiteral819Names.Purge,
                context,
                Owner
            )!);
            Require(!newRetirement.IsCompleted && PayloadOf(Read(context, ExternalKey)).Equals(PayloadOf(Read(context, NewKey))));
            bool rejected = false;
            try
            {
                _ = await Execution.EvaluateAsync(new Expr.Call(Consumer, Make, []), context);
            }
            catch (ErlangException error) when (error.Reason is TupleTerm { Items: [Atom { Name: BadRecord }, _] })
            {
                rejected = true;
            }
            Require(rejected);
            foreach (string key in new[] { NewKey, ExternalKey, DefinitionKey, OpaqueKey }) Require(context.Dictionary.Remove(Term.A(key)));
            Call(
                Property(context, SourceLiteral819Names.Heap),
                SourceLiteral819Names.Collect,
                true,
                (ushort)0
            );

            return true;
        });
        await newRetirement.WaitAsync(Deadline);
        Require(Property(first.NativeRecords[0].Fields[0].Value!, SourceLiteral819Names.Storage) is null);
    }

    private static async Task BorrowedDefinition(ProcessContext context, ProcessRuntime runtime)
    {
        object heap = Property(context, SourceLiteral819Names.Heap);
        using var frame = (IDisposable)Call(heap, SourceLiteral819Names.Enter)!;
        Term borrowed = (Term)Call(
            frame,
            SourceLiteral819Names.Adopt,
            (int)Call(frame, SourceLiteral819Names.Reserve)!,
            Term.Tuple(Term.A(BorrowedTag), Term.List(Term.I(17), Term.I(23)))
        )!;
        var definition = new NativeRecordDefinition(Record, true, [new KeyValuePair<Atom, Term?>(Term.A(Payload), borrowed)]);
        runtime.Modules.PublishModuleCode(BorrowedModule, new ModuleCodeDescriptor(
            [],
            [],
            new BitString(new byte[16]),
            Nil.Value,
            nativeRecords: [definition]
        ));
        Require(Property(runtime.Modules.CurrentCodeImage(BorrowedModule)!, SourceLiteral819Names.ImageDomain) is null);
        try
        {
            await Store(
                context,
                BorrowedKey,
                new Expr.Call(Records, GetDefinition, [new Expr.Literal(Term.A(BorrowedModule)), new Expr.Literal(Term.A(Record))])
            );
            Term copied = DefinitionDefault(Read(context, BorrowedKey));
            // Actual live supplying frame is retained throughout the call, but
            // public borrowed metadata must keep the old opaque copy boundary.
            Require(copied.Equals(borrowed) && !ReferenceEquals(copied, borrowed));
            Require(!ReferenceEquals(Property(copied, SourceLiteral819Names.Storage), Property(borrowed, SourceLiteral819Names.Storage)));
            Require(context.Dictionary.Remove(Term.A(BorrowedKey)));
        }
        finally
        {
            runtime.Modules.DeleteModule(BorrowedModule);
        }
    }

    private static Term DefinitionDefault(Term value)
        => ((TupleTerm)Cons.Items(((TupleTerm)value).Items[1]).First(item => item is TupleTerm pair && pair.Items[0].Equals(Term.A(Payload)))).Items[1];

    private static void RequirePortableDefinition(Term value, Term expected)
    {
        Term payload = DefinitionDefault(value);
        Require(payload.Equals(expected) && Property(payload, SourceLiteral819Names.Storage) is null);
    }

    private static async Task Reject(ProcessContext context, IReadOnlyList<RecordField> fields, string reason)
    {
        try
        {
            _ = await Execution.EvaluateAsync(new NativeRecordExpression(
                NativeRecordOperation.Construct,
                Owner,
                Record,
                Consumer,
                true,
                null,
                fields
            ), context);
        }
        catch (ErlangException error) when (error.Reason is TupleTerm { Items: [Atom tag, _] } && tag.Name == reason)
        {
            return;
        }
        throw new InvalidOperationException(Failure);
    }

    private static Term PayloadOf(Term value) => ((NativeRecordTerm)value).Values[0];

    private static object Domain(Term value) => Property(
        Property(Property(value, SourceLiteral819Names.Storage), SourceLiteral819Names.StorageOwner),
        SourceLiteral819Names.HeapDomain
    );

    private static Term Read(ProcessContext context, string key)
    {
        object dictionary = context.Dictionary;
        object gate = dictionary.GetType().GetField(SourceLiteral819Names.DictionaryGate, Members)!.GetValue(dictionary)!;
        lock (gate)
            return ((Dictionary<Term, Term>)dictionary.GetType().GetField(SourceLiteral819Names.DictionaryData, Members)!.GetValue(dictionary)!)[Term.A(key)];
    }

    private static async Task Store(ProcessContext context, string key, Expr expression)
        => _ = await Execution.EvaluateAsync(
            new Expr.Call(SourceLiteral819Names.DictionaryModule, SourceLiteral819Names.Put, [new Expr.Literal(Term.A(key)), expression]),
            context
        );

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }
}
