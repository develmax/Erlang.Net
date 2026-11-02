using TargetInvocationException = System.Reflection.TargetInvocationException;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using Erlang.Compiler;
using static Erlang.Tests.PublicReceiverFunction414Names;
namespace Erlang.Tests;

internal static class PublicReceiverFunction414Checks
{
    internal static async Task Run()
    {
        foreach (bool pilot in new[] { false, true }) await One(pilot);
    }

    private static async Task One(bool pilot)
    {
        await using var origin = OwnedGuest506RuntimeFactory.Create(pilot);
        await using var target = OwnedGuest506RuntimeFactory.Create(pilot);
        await using var missing = OwnedGuest506RuntimeFactory.Create(pilot);
        new Parser(Source).ParseModule().Register(origin.Modules);
        // Independent actual receiver source; its make is NEVER called.
        new Parser(Source).ParseModule().Register(target.Modules);
        new Parser(MissingSource).ParseModule().Register(missing.Modules);
        await using var sourceActor = new RuntimeNodeFoundationHostActor(origin);
        await using var targetActor = new RuntimeNodeFoundationHostActor(target);
        await using var missingActor = new RuntimeNodeFoundationHostActor(missing);
        var source = (FunctionTerm)await sourceActor.InvokeAsync(context => origin.Modules.Call(
            context,
            PublicReceiverFunction414Names.Module,
            Make,
            Term.I(Value)
        ).AsTask()).WaitAsync(Deadline);
        var identity = (LocalFunctionIdentity)typeof(FunctionTerm).GetProperty(StoredIdentity, Members)!.GetValue(source)!;
        var restored = origin.Functions.Restore(
            origin,
            identity,
            source.Arity,
            source.Environment
        );
        Require(typeof(FunctionTerm).GetProperty(StoredIdentity, Members)!.GetValue(restored) is null);
        Require(typeof(FunctionTerm).GetProperty(RuntimeIdentity, Members)!.GetValue(restored) is LocalFunctionIdentity);
        // This is the receiver actor's FIRST public source call, not a warmup.
        // The ordinary Host bridge was skipped before its collector admission.
        var first = (TupleTerm)await targetActor.InvokeAsync(context => target.Modules.Call(
            context,
            PublicReceiverFunction414Names.Module,
            Echo,
            Term.Tuple(source)
        ).AsTask()).WaitAsync(Deadline);
        Require((await targetActor.InvokeAsync(context => ((FunctionTerm)first.Items[0]).Invoke(context, []).AsTask()).WaitAsync(Deadline)).Equals(Term.I(Value)));
        int factoryCalls = 0;
        var sentinel = new FunctionTerm(
            identity,
            0,
            [Term.I(Value)],
            (_, _) => ValueTask.FromResult<Term>(Term.I(SentinelValue))
        );
        target.Functions.Register(
            identity,
            0,
            1,
            _ =>
 {
     factoryCalls++;

     return sentinel;
 }
        );
        foreach (FunctionTerm input in new[] { source, restored })
        {
            Term owner = await targetActor.InvokeAsync(context => target.Modules.Call(
                context,
                ErlangModule,
                FunInfo,
                input,
                Term.A(PidInfo)
            ).AsTask()).WaitAsync(Deadline);
            Require(owner.Equals(Term.Tuple(Term.A(PidInfo), target.InitializationPid)));
            Term value = await targetActor.InvokeAsync(context => input.Invoke(context, []).AsTask()).WaitAsync(Deadline);
            Require(value.Equals(Term.I(Value)) && factoryCalls == 0);
            var map = new MapTerm([new(Term.A(MapKey), input)]);
            var record = new NativeRecordTerm(
                Term.A(PublicReceiverFunction414Names.Module),
                Term.A(RecordName),
                false,
                [new(Term.A(PublicReceiverFunction414Names.RecordField), input)]
            );
            var tuple = Term.Tuple(Term.List(input), map, record);
            var copy = (TupleTerm)await targetActor.InvokeAsync(context => target.Modules.Call(
                context,
                PublicReceiverFunction414Names.Module,
                Echo,
                tuple
            ).AsTask()).WaitAsync(Deadline);
            var copiedMap = (MapTerm)copy.Items[1];
            var copiedRecord = (NativeRecordTerm)copy.Items[2];
            foreach (var copied in new[] { (FunctionTerm)((Cons)copy.Items[0]).Head, (FunctionTerm)copiedMap.Entries.Single(pair => pair.Key.Equals(Term.A(MapKey))).Value, (FunctionTerm)copiedRecord.Values[0] })
                Require((await targetActor.InvokeAsync(context => copied.Invoke(context, []).AsTask()).WaitAsync(Deadline)).Equals(Term.I(Value)));
        }
        Require(factoryCalls == 0);
        Require(ReferenceEquals(target.Functions.Restore(
            target,
            identity,
            0,
            source.Environment
        ), sentinel) && factoryCalls == 1);
        int lazyReads = 0;
        var opaque = new FunctionTerm(
            () =>
 {
     lazyReads++;

     return identity;
 },
            0,
            [source],
            (_, _) => ValueTask.FromResult<Term>(Nil.Value)
        );
        var opaqueCopy = (FunctionTerm)await targetActor.InvokeAsync(context => target.Modules.Call(
            context,
            PublicReceiverFunction414Names.Module,
            Echo,
            opaque
        ).AsTask()).WaitAsync(Deadline);
        Require(lazyReads == 0 && opaqueCopy.Environment.Count == 1);
        Require((await targetActor.InvokeAsync(context => ((FunctionTerm)opaqueCopy.Environment[0]).Invoke(context, []).AsTask()).WaitAsync(Deadline)).Equals(Term.I(Value)));
        Require((await missingActor.InvokeAsync(context => missing.Modules.Call(
            context,
            MissingModule,
            Echo,
            Nil.Value
        ).AsTask()).WaitAsync(Deadline)) is Nil);
        await Refused(async () =>
 {
     _ = await missingActor.InvokeAsync(context => missing.Modules.Call(
         context,
         ErlangModule,
         FunInfo,
         source,
         Term.A(PidInfo)
     ).AsTask()).WaitAsync(Deadline);
 });
        await BeamReceiver(
            origin,
            target,
            sourceActor,
            targetActor,
            pilot
        );
        object? frame = null;
        try
        {
            FunctionTerm native = (FunctionTerm)await sourceActor.Invoke(context =>
            {
                object heap = typeof(ProcessContext).GetProperty(Heap, Members)!.GetValue(context)!;
                frame = Call(heap, Enter)!;
                int slot = (int)Call(frame, Reserve)!;
                Call(
                    frame,
                    Set,
                    slot,
                    source
                );

                return (Term)Call(frame, Read, slot)!;
            }).WaitAsync(Deadline);
            Require(typeof(FunctionTerm).GetProperty(OwnedFunction, Members)!.GetValue(native) is not null);
            await Refused(async () =>
 {
     _ = await targetActor.InvokeAsync(context => target.Modules.Call(
         context,
         PublicReceiverFunction414Names.Module,
         Echo,
         native
     ).AsTask()).WaitAsync(Deadline);
 });
            // Raw Heap.Export remains strict despite matching receiver code.
            await Refused(async () =>
 {
     _ = await targetActor.Invoke(context =>
                {
                    object heap = typeof(ProcessContext).GetProperty(Heap, Members)!.GetValue(context)!;
                    using var local = (IDisposable)Call(heap, Enter)!;

                    return (Term)Call(
                        heap,
                        Export,
                        local,
                        source
                    )!;
                }).WaitAsync(Deadline);
 });
        }
        finally
        {
            (frame as IDisposable)?.Dispose();
        }
    }

    private static async Task BeamReceiver(
        ProcessRuntime origin,
        ProcessRuntime target,
        RuntimeNodeFoundationHostActor sourceActor,
        RuntimeNodeFoundationHostActor targetActor,
        bool pilot
    )
    {
        using Stream resource = typeof(PublicReceiverFunction414Checks).Assembly.GetManifestResourceStream(BeamResource)!;
        using var bytes = new MemoryStream();
        resource.CopyTo(bytes);
        byte[] image = bytes.ToArray();
        Require(Convert.ToHexString(SHA256.HashData(image)) == BeamHash);
        foreach (ProcessRuntime runtime in new[] { origin, target })
            await PreparedBeamFixtureScope171.RunAsync(
                runtime,
                (registry, scope) =>
            {
                PreparedBeamImage prepared = scope.Prepare(() => registry.PrepareBeam(image, new BeamDependencySelection([ErlangModule])));

                return ValueTask.FromResult<Action<ModuleRegistry>>(modules => Require(modules.PublishPreparedBeam(prepared).Status == BeamPublicationStatus.Published));
            }
            );
        var source = (FunctionTerm)await sourceActor.InvokeAsync(context => origin.Modules.Call(
            context,
            PublicReceiverFunction414Names.BeamModule,
            Make,
            Term.I(Value)
        ).AsTask()).WaitAsync(Deadline);
        // A fresh receiver's FIRST public BEAM call captures the foreign FUN;
        // no prior echo, make, or public function call admits its actor.
        await using var firstReceiver = OwnedGuest506RuntimeFactory.Create(pilot);
        await PreparedBeamFixtureScope171.RunAsync(
            firstReceiver,
            (registry, scope) =>
            {
                PreparedBeamImage prepared = scope.Prepare(() => registry.PrepareBeam(image, new BeamDependencySelection([ErlangModule])));

                return ValueTask.FromResult<Action<ModuleRegistry>>(modules => Require(modules.PublishPreparedBeam(prepared).Status == BeamPublicationStatus.Published));
            }
        );
        await using var firstActor = new RuntimeNodeFoundationHostActor(firstReceiver);
        var outer = (FunctionTerm)await firstActor.InvokeAsync(context => firstReceiver.Modules.Call(
            context,
            PublicReceiverFunction414Names.BeamModule,
            Make,
            source
        ).AsTask()).WaitAsync(Deadline);
        var captured = (FunctionTerm)outer.Environment.Single();
        Require((await firstActor.InvokeAsync(context => captured.Invoke(context, [Term.I(BeamArgument)]).AsTask()).WaitAsync(Deadline)).Equals(Term.I(BeamResult)));
        Require((await firstActor.InvokeAsync(context => firstReceiver.Modules.Call(
            context,
            ErlangModule,
            FunInfo,
            captured,
            Term.A(PidInfo)
        ).AsTask()).WaitAsync(Deadline)).Equals(Term.Tuple(Term.A(PidInfo), firstReceiver.InitializationPid)));
        var identity = (LocalFunctionIdentity)typeof(FunctionTerm).GetProperty(StoredIdentity, Members)!.GetValue(source)!;
        FunctionTerm restored = origin.Functions.Restore(
            origin,
            identity,
            source.Arity,
            source.Environment
        );
        Require(typeof(FunctionTerm).GetProperty(StoredIdentity, Members)!.GetValue(restored) is null);
        Require(typeof(FunctionTerm).GetProperty(RuntimeIdentity, Members)!.GetValue(restored) is LocalFunctionIdentity);
        foreach (FunctionTerm input in new[] { source, restored })
        {
            Require((await targetActor.InvokeAsync(context => input.Invoke(context, [Term.I(BeamArgument)]).AsTask()).WaitAsync(Deadline)).Equals(Term.I(BeamResult)));
            var map = new MapTerm([new(Term.A(MapKey), input)]);
            var copied = (MapTerm)await targetActor.InvokeAsync(context => target.Modules.Call(
                context,
                PublicReceiverFunction414Names.Module,
                Echo,
                map
            ).AsTask()).WaitAsync(Deadline);
            var function = (FunctionTerm)copied.Entries.Single().Value;
            Require((await targetActor.InvokeAsync(context => function.Invoke(context, [Term.I(BeamArgument)]).AsTask()).WaitAsync(Deadline)).Equals(Term.I(BeamResult)));
        }
    }

    private static async Task Refused(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (NotSupportedException error) when (error.Message == RefusedClosure)
        {
            return;
        }
        catch (InvalidOperationException error) when (error.Message == ForeignRoot)
        {
            return;
        }
        throw new InvalidOperationException(Failure);
    }

    private static object? Call(object target, string name, params object[] arguments)
    {
        try
        {
            return target.GetType().GetMethod(name, Members)!.Invoke(target, arguments);
        }
        catch (TargetInvocationException error) when (error.InnerException is { } actual)
        {
            ExceptionDispatchInfo.Capture(actual).Throw();
            throw;
        }
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }
}
