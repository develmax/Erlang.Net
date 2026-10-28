using System.Reflection;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.NativeResource524Names;
using static Erlang.Tests.NativeResult582Names;

namespace Erlang.Tests;

internal static class NativeResult582Checks
{
    private const BindingFlags Static = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
    private static readonly Type Invocation = typeof(ProcessRuntime).Assembly.GetType(InvocationType, true)!;

    // Guard-state unit control ONLY. Real NativeGuest535 owner admission is
    // tested separately by the unchanged genuine original5136 NIF observer.
    // This test gives a real bindings-admitted call a controlled guest-flag
    // input; it never forges counters, states, a public provider or a golden.
    internal static async Task RefusalsWithoutPublication()
    {
        var failures = new List<Exception>();
        ProcessRuntime? runtime = null, foreign = null;
        RuntimeNodeFoundationHostActor? actor = null;
        object? bindings = null;
        IDisposable? token = null;
        try
        {
            runtime = OwnedGuest506RuntimeFactory.Create();
            foreign = OwnedGuest506RuntimeFactory.Create();
            actor = new RuntimeNodeFoundationHostActor(runtime);
            ProcessContext context = await actor.Ready.WaitAsync(Deadline);
            object image = New(ImageType, NativeResult582Names.Module);
            object wrongImage = New(ImageType, NativeResult582Names.Module);
            bindings = Property(image, BindingsProperty);
            var descriptor = new ModuleCodeDescriptor(
                [(Probe, 0)],
                [(Probe, 0)],
                new BitString(new byte[16]),
                Nil.Value
            )
            { DeclaredNifs = [(Probe, 0)] };
            object reservation = Call(bindings, BeginLoadMethod, descriptor)!;
            object? guest = null;
            ProcessRuntime current = runtime, wrongRuntime = foreign;
            Term candidate = Term.Tuple(Term.I(41));
            ErlangFunction body = (ctx, ignoredArguments) =>
            {
                Check((bool)Call(token!, CanRetainMethod, image)!);
                Reject(
                    ctx,
                    guest!,
                    current,
                    wrongImage,
                    candidate
                );
                Reject(
                    ctx,
                    guest!,
                    wrongRuntime,
                    image,
                    candidate
                );
                InScope(ctx, false, opaque =>
                {
                    SetKind(opaque, OpaqueKind);
                    Reject(
                        ctx,
                        guest!,
                        current,
                        image,
                        candidate,
                        opaque
                    );
                });
                InScope(
                    ctx,
                    false,
                    stale =>
                {
                    Call(stale, BindGuestMethod, token);
                    ExecutionContext captured = ExecutionContext.Capture()!;
                    ((IDisposable)stale).Dispose();
                    ExecutionContext.Run(captured, ignoredState => Reject(
                        ctx,
                        stale,
                        current,
                        image,
                        candidate
                    ), null);
                }
                );

                return ValueTask.FromResult<Term>(Nil.Value);
            };
            Call(
                bindings,
                PublishMethod,
                reservation,
                new[] { new KeyValuePair<(string Name, int Arity), ErlangFunction>((Probe, 0), body) },
                new NativeResource524ImageOwner()
            );
            token = (IDisposable)Call(
                bindings,
                AdmitMethod,
                Probe,
                0
            )!;
            Check(!(bool)Property(token, GuestFlagProperty)); // fake provider never self-admits
            token.GetType().GetField(GuestFlagField, Members)!.SetValue(token, true);
            await actor.Invoke(ctx =>
            {
                InScope(
                    ctx,
                    true,
                    parentScope => InScope(
                        ctx,
                        false,
                        owned =>
                {
                    guest = owned;
                    Call(owned, BindGuestMethod, token);
                    Reject(
                        ctx,
                        owned,
                        current,
                        image,
                        candidate
                    ); // real Reserved state
                    var running = (ValueTask<Term>)Call(
                        token,
                        InvokeMethod,
                        ctx,
                        Array.Empty<Term>()
                    )!;
                    Check(running.IsCompleted);
                    _ = running.GetAwaiter().GetResult(); // actual Running -> Released lifecycle
                    Reject(
                        ctx,
                        owned,
                        current,
                        image,
                        candidate
                    );
                    Check(!(bool)Call(token, CanRetainMethod, image)!);
                }
                    )
                );

                return true;
            });
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            if (token is not null) Attempt(token.Dispose, failures);
            if (bindings is not null) await Drain(() => (Task)Call(bindings, RetireMethod)!, failures);
            if (actor is not null) await Drain(() => actor.DisposeAsync().AsTask(), failures);
            if (foreign is not null) await Drain(() => foreign.DisposeAsync().AsTask(), failures);
            if (runtime is not null) await Drain(() => runtime.DisposeAsync().AsTask(), failures);
        }
        Throw(failures);
    }

    private static void Reject(
        ProcessContext context,
        object primary,
        ProcessRuntime expected,
        object image,
        Term candidate,
        object? additional = null
    )
    {
        object frame = Property(primary, FrameProperty);
        int before = (int)Property(frame, WatermarkProperty);
        object heap = Property(context, HeapProperty);
        long physical = Convert.ToInt64(Property(Property(heap, SnapshotProperty), PhysicalWordsProperty));
        object? extra = additional is null ? null : Property(additional, FrameProperty);
        int extraBefore = extra is null ? 0 : (int)Property(extra, WatermarkProperty);
        object?[] arguments = [expected, image, candidate, null];
        bool admitted;
        try
        {
            admitted = (bool)Invocation.GetMethod(TryAdmitMethod, Static)!.Invoke(null, arguments)!;
        }
        catch (TargetInvocationException error) when (error.InnerException is { } inner)
        {
            ExceptionDispatchInfo.Capture(inner).Throw();
            throw;
        }
        Check(!admitted && arguments[3] is null && (int)Property(frame, WatermarkProperty) == before);
        Check(Convert.ToInt64(Property(Property(heap, SnapshotProperty), PhysicalWordsProperty)) == physical);
        if (extra is not null) Check((int)Property(extra, WatermarkProperty) == extraBefore);
    }

    private static void InScope(ProcessContext context, bool host, Action<object> body)
    {
        object scope = Activator.CreateInstance(
            Invocation,
            Members,
            null,
            [context, Array.Empty<Term>(), host, false],
            null
        )!;
        var failures = new List<Exception>();
        try
        {
            body(scope);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            Attempt(((IDisposable)scope).Dispose, failures);
        }
        Throw(failures);
    }

    private static void SetKind(object scope, string name)
    {
        PropertyInfo property = Invocation.GetProperty(BindingKindProperty, Members)!;
        property.SetValue(scope, Enum.Parse(property.PropertyType, name));
    }

    private static object New(string name, params object?[] arguments)
        => Activator.CreateInstance(
            typeof(ProcessRuntime).Assembly.GetType(name, true)!,
            Members,
            null,
            arguments,
            null
        )!;

    private static void Check(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }

    private static void Attempt(Action action, List<Exception> failures)
    {
        try
        {
            action();
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
    }

    private static async Task Drain(Func<Task> action, List<Exception> failures)
    {
        try
        {
            await action().WaitAsync(Deadline);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
    }

    private static void Throw(List<Exception> failures)
    {
        if (failures.Count == 1) ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1) throw new AggregateException(Cleanup, failures);
    }
}
