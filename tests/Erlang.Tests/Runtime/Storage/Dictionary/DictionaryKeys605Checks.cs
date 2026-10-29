using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using Erlang.Differential;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.DictionaryKeys605Names;

namespace Erlang.Tests;

internal static class DictionaryKeys605Checks
{
    internal static Task NativeKeys() => RunNative(false, false);

    internal static Task NativeOwnerExit() => RunNative(true, false);

    internal static Task OpaqueReplacement() => RunNative(false, true);

    private static Task RunNative(bool ownerExit, bool opaque) => NativeMagicReference576Fixture.Run(async fixture =>
    {
        var failures = new List<Exception>();
        RuntimeNodeFoundationHostActor? actor = null;
        try
        {
            ProcessRuntime runtime = fixture.Runtime;
            await runtime.InitializeModulesAsync(registry => RuntimeBootstrapDebugSharing425.Register(runtime, registry));
            using var compiled = GeneratedModuleCompiler.Compile(Source, DictionaryKeys605Names.Module + ".erl");
            compiled.Definition.Register(runtime.Modules);
            actor = new(runtime);
            ProcessContext owner = await actor.Ready.WaitAsync(Deadline);
            object sourceHeap = Property(owner, Heap);
            object payload = fixture.CreatePayload();
            nint pointer = (nint)Property(payload, DictionaryKeys605Names.Pointer);
            Marshal.WriteInt32(pointer, Sentinel);
            var identity = (ResourceReferenceTerm)Call(runtime.ReferenceResources, Make, payload)!;
            IDisposable sourceFrame = fixture.EnterRoot(sourceHeap);
            int sourceSlot = (int)Call(sourceFrame, Reserve)!;
            Call(
                sourceFrame,
                Set,
                sourceSlot,
                identity
            );
            var source = (ResourceReferenceTerm)Call(sourceFrame, Read, sourceSlot)!;
            object heap = sourceHeap;
            await actor.Invoke(context =>
            {
                // The ordinary public setter retains its documented Host boundary.
                using var host = new NativeTermHostScope524(runtime);
                context.Dictionary[Term.A(Stored)] = source;

                return true;
            });
            fixture.ReleaseNative();
            Call(sourceFrame, Clear, sourceSlot);
            MajorCollect(sourceHeap);
            bool stale = false;
            try
            {
                _ = source.Words;
            }
            catch (ObjectDisposedException)
            {
                stale = true;
            }
            Require(stale && !((Task)Property(payload, Retired)).IsCompleted);
            // This temporary source frame must not hold owner physical Done.
            sourceFrame.Dispose();
            await actor.Invoke(context =>
 {
     MajorCollect(heap);

     return true;
 });
            Require(Marshal.ReadInt32(pointer) == Sentinel);

            if (opaque)
            {
                await actor.Invoke(context =>
                {
                    var data = (Dictionary<Term, Term>)typeof(ProcessDictionary)
                        .GetField(DictionaryKeys605Names.DictionaryData, Members)!.GetValue(context.Dictionary)!;
                    using var keyFrame = (IDisposable)Call(heap, Enter)!;
                    int keySlot = (int)Call(keyFrame, Reserve)!;
                    Call(
                        keyFrame,
                        Set,
                        keySlot,
                        data[Term.A(Stored)]
                    );
                    context.Dictionary.Clear();
                    context.Dictionary[(Term)Call(keyFrame, Read, keySlot)!] = Term.I(Sentinel);

                    return true;
                });
                var functions = (Dictionary<(string Module, string Function, int Arity), ErlangFunction>)typeof(ModuleRegistry)
                    .GetField(RegistryFunctions, Members)!.GetValue(runtime.Modules)!;
                ErlangFunction original = functions[(DictionaryKeys605Names.Erlang, GetKeys, 0)];
                runtime.Modules.DeleteModule(DictionaryKeys605Names.Erlang);
                runtime.Modules.Register(
                    DictionaryKeys605Names.Erlang,
                    GetKeys,
                    0,
                    original
                );
                Require(runtime.Modules.SourceOwner(DictionaryKeys605Names.Erlang, GetKeys, 0) is null);
                bool refused = false;
                try
                {
                    await actor.InvokeAsync(async context => await runtime.Modules.Call(context, DictionaryKeys605Names.Module, ResourceKeysEntry));
                }
                catch (NotSupportedException error) when (error.Message == NativeGuest535Names.ScopeRequired)
                {
                    refused = true;
                }
                Require(refused);
                await actor.Invoke(context =>
 {
     context.Dictionary.Clear();
     MajorCollect(heap);

     return true;
 });
            }
            else
            {
                Term keys = await actor.InvokeAsync(async context =>
                    await runtime.Modules.Call(context, DictionaryKeys605Names.Module, KeysEntry));
                Require(keys.Equals(Term.I(KeyChecks)) && Marshal.ReadInt32(pointer) == Sentinel);
                Require(!((Task)Property(payload, Retired)).IsCompleted);
                if (ownerExit)
                {
                    await actor.DisposeAsync().AsTask().WaitAsync(Deadline);
                    actor = null;
                }
                else
                {
                    Term words = await actor.InvokeAsync(async context =>
                        await runtime.Modules.Call(context, DictionaryKeys605Names.Module, EraseEntry));
                    Require(words.Equals(Term.I(NativeWords)));
                    // Scalar return has closed source invocation frames. Guest
                    // collection, rather than CLR finalization, retires headers.
                    await actor.Invoke(context =>
 {
     MajorCollect(heap);

     return true;
 });
                }
            }
            await ((Task)Property(payload, Retired)).WaitAsync(Deadline);
            GC.KeepAlive(source);
            GC.KeepAlive(payload);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            if (actor is not null)
                try
                {
                    await actor.DisposeAsync().AsTask().WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
        }
        Finish(failures);
    });

    internal static async Task AtomicsKeys()
    {
        var failures = new List<Exception>();
        AtomicsP2a586Fixture? fixture = null;
        RuntimeNodeFoundationHostActor? actor = null;
        IDisposable? producer = null, hostCarrier = null;
        try
        {
            fixture = new();
            producer = fixture.Lease(AtomicsP2a586Names.ProducerKind);
            ProcessRuntime runtime = fixture.Runtime;
            await runtime.InitializeModulesAsync(registry => RuntimeBootstrapDebugSharing425.Register(runtime, registry));
            using var compiled = GeneratedModuleCompiler.Compile(Source, DictionaryKeys605Names.Module + ".erl");
            compiled.Definition.Register(runtime.Modules);
            actor = new(runtime);
            ProcessContext owner = await actor.Ready.WaitAsync(Deadline);
            object heap = Property(owner, Heap);
            await actor.Invoke(context =>
 {
     context.Dictionary[Term.A(Stored)] = fixture.Reference;

     return true;
 });
            fixture.ReleaseProducer();
            producer.Dispose();
            producer = null;
            await actor.Invoke(context =>
 {
     MajorCollect(heap);

     return true;
 });
            Term keys = await actor.InvokeAsync(async context => await runtime.Modules.Call(context, DictionaryKeys605Names.Module, KeysEntry));
            Require(keys.Equals(Term.I(KeyChecks)));
            var exported = (ReferenceTerm)await actor.Invoke(context => context.Dictionary[Term.A(Stored)]);
            hostCarrier = typeof(ResourceReferenceTerm).GetProperty(HostCarrier, Members)!.GetValue(exported) as IDisposable;
            Require(hostCarrier is not null);
            ErlangFunction put = AtomicsBody(runtime, AtomicsPut, 3);
            ErlangFunction get = AtomicsBody(runtime, AtomicsGet, 2);
            await actor.InvokeAsync(async context => await put(context, [exported, Term.I(1), Term.I(Sentinel)]));
            Term actual = await actor.InvokeAsync(async context => await get(context, [exported, Term.I(1)]));
            Require(actual.Equals(Term.I(Sentinel)));
            hostCarrier!.Dispose();
            hostCarrier = null;
            Term words = await actor.InvokeAsync(async context => await runtime.Modules.Call(context, DictionaryKeys605Names.Module, EraseEntry));
            Require(words.Equals(Term.I(NativeWords)));
            await actor.Invoke(context =>
 {
     MajorCollect(heap);

     return true;
 });
            await ((Task)Property(fixture.Array, AtomicsP2a586Names.Retired)).WaitAsync(Deadline);
            Require(!(bool)Property(fixture.Array, AtomicsP2a586Names.Allocated));
            GC.KeepAlive(exported);
            GC.KeepAlive(fixture.Reference);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            try
            {
                hostCarrier?.Dispose();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            try
            {
                producer?.Dispose();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
            if (actor is not null)
                try
                {
                    await actor.DisposeAsync().AsTask().WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            if (fixture is not null) await fixture.Drain(failures);
        }
        Finish(failures);
    }

    private static ErlangFunction AtomicsBody(ProcessRuntime runtime, string function, int arity)
    {
        MethodInfo capture = typeof(ProcessRuntime).Assembly.GetType(AtomicsNativeType, true)!.GetMethod(Capture, Members)!;
        object?[] arguments = [runtime, AtomicsModule, function, arity, null];
        Require((bool)capture.Invoke(null, arguments)!);

        return (ErlangFunction)arguments[4]!;
    }

    private static void MajorCollect(object heap) => Call(
        heap,
        Major,
        true,
        ushort.MaxValue
    );

    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(Failure);
    }

    private static void Finish(List<Exception> failures)
    {
        if (failures.Count == 1) ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1) throw new AggregateException(CleanupFailure, failures);
    }
}
