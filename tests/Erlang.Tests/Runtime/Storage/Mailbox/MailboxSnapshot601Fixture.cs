using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.MailboxSnapshot601TestNames;

namespace Erlang.Tests;

internal sealed class MailboxSnapshot601Fixture(NativeMagicReference576Fixture native)
{
    internal ProcessRuntime Runtime => native.Runtime;
    internal RuntimeNodeFoundationHostActor Target { get; private set; } = null!;
    internal ProcessContext TargetContext { get; private set; } = null!;
    internal object MailboxOwner => Property(TargetContext, MailboxProperty);
    internal object Payload { get; private set; } = null!;
    internal object DestinationHeap { get; private set; } = null!;
    internal IDisposable Destination { get; private set; } = null!;
    internal int DestinationSlot { get; private set; }
    internal Task<Term>? Pump { get; private set; }

    private async Task Initialize()
    {
        var registered = (Dictionary<(string Module, string Function, int Arity), ErlangFunction>)typeof(ModuleRegistry)
            .GetField(Functions, Members)!.GetValue(Runtime.Modules)!;
        var signature = (ExportGlobal431Names.Debug, ExportGlobal431Names.Flat, 1);
        bool providerBefore = registered.ContainsKey(signature);
        await Runtime.InitializeModulesAsync(registry => RuntimeBootstrapDebugSharing425.Register(Runtime, registry));
        Console.WriteLine(DebugProviderObservation, providerBefore, registered.ContainsKey(signature));
        Require(registered.ContainsKey(signature));
        Target = new(Runtime);
        TargetContext = await Target.Ready.WaitAsync(Deadline);
        object sourceHeap = await native.CreateHeap();
        DestinationHeap = await native.CreateHeap();
        Payload = native.CreatePayload();
        Marshal.WriteInt32((nint)Property(Payload, PointerProperty), Sentinel);
        var identity = (ResourceReferenceTerm)Call(Runtime.ReferenceResources, Make, Payload)!;
        IDisposable sourceFrame = native.EnterRoot(sourceHeap);
        int slot = (int)Call(sourceFrame, Reserve)!;
        Call(
            sourceFrame,
            Set,
            slot,
            identity
        );
        var source = (ResourceReferenceTerm)Call(sourceFrame, Read, slot)!;
        Term portable = (Term)Static(
            CopyType,
            Copy,
            Runtime,
            source,
            false,
            null
        )!;
        using (var publication = (IDisposable)Static(
            PublicationType,
            Capture,
            Runtime,
            portable,
            null
        )!)
            Call(
                MailboxOwner,
                Send,
                portable,
                null,
                false,
                false,
                null,
                true,
                null,
                publication
            );
        native.ReleaseNative();
        Call(sourceFrame, Clear, slot);
        Call(
            sourceHeap,
            Major,
            true,
            ushort.MaxValue
        );
        bool stale = false;
        try
        {
            _ = source.Words;
        }
        catch (ObjectDisposedException)
        {
            stale = true;
        }
        Require(stale && !((Task)Property(Payload, Retired)).IsCompleted);
        Destination = native.EnterRoot(DestinationHeap);
        DestinationSlot = (int)Call(Destination, Reserve)!;
        GC.KeepAlive(source);
    }

    internal void BeginPump()
    {
        Pump = Target.InvokeAsync(async context =>
        {
            using var host = new NativeTermHostScope524(Runtime);
            await context.ReceiveAsync<Term>(value => value is Atom atom && atom.Name == Stop ? value : null, Deadline);

            return (Term)Nil.Value;
        });
    }

    internal async Task Dequeue()
    {
        if (Pump is not null)
        {
            Runtime.Send(TargetContext.Self, Term.A(Stop));
            await Pump.WaitAsync(Deadline);
            Pump = null;
        }
        await Target.InvokeAsync(async context =>
        {
            using var host = new NativeTermHostScope524(Runtime);
            await context.ReceiveAsync<Term>(value => value is ResourceReferenceTerm ? value : null, Deadline);

            return Nil.Value;
        });
    }

    internal void Adopt(Term value) => Call(
        Destination,
        Set,
        DestinationSlot,
        value
    );

    internal async Task VerifyAndRetire()
    {
        var value = (ResourceReferenceTerm)Call(Destination, Read, DestinationSlot)!;
        object actual = Static(
            Roots,
            Direct,
            value,
            Runtime
        )!;
        Require(ReferenceEquals(actual, Payload) && Marshal.ReadInt32((nint)Property(actual, PointerProperty)) == Sentinel);
        Call(Destination, Clear, DestinationSlot);
        Call(
            DestinationHeap,
            Major,
            true,
            ushort.MaxValue
        );
        await ((Task)Property(Payload, Retired)).WaitAsync(Deadline);
        GC.KeepAlive(value);
    }

    internal static Task Run(Func<MailboxSnapshot601Fixture, Task> body) => NativeMagicReference576Fixture.Run(async native =>
    {
        var fixture = new MailboxSnapshot601Fixture(native);
        var failures = new List<Exception>();
        try
        {
            await fixture.Initialize();
            await body(fixture);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            if (fixture.Pump is not null)
            {
                try
                {
                    fixture.Runtime.Send(fixture.TargetContext.Self, Term.A(Stop));
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
                try
                {
                    await fixture.Pump.WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            }
            if (fixture.Target is not null)
                try
                {
                    await fixture.Target.DisposeAsync().AsTask().WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
        }
        if (failures.Count == 1) ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1) throw new AggregateException(CleanupFailure, failures);
    });

    internal static object? Static(string type, string method, params object?[] args)
    {
        try
        {
            return typeof(ProcessRuntime).Assembly.GetType(type, true)!.GetMethod(method, Members)!.Invoke(null, args);
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    internal static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }
}
