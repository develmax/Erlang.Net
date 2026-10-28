using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.NativeResource524Names;
using static Erlang.Tests.NativeResourceTypeRetirement575Names;

namespace Erlang.Tests;

// Finite actual type-table/handle controls using the existing controlled image
// owner. These do not claim that a genuine C NIF or original OTP executed.
internal static class NativeResourceTypeRetirement575Checks
{
    internal static Task RepeatedEmptyRetirement() => Run(async fixture =>
    {
        for (int iteration = 0; iteration < Repetitions; iteration++)
        {
            NativeResource527PendingImage image = fixture.Image();
            object load = fixture.Load(image);
            object type = NativeResource527Access.Open(load, ResourceName, Create).Type!;
            nint token = (nint)Property(type, Token);
            NativeResource527Access.Publish(load, image);
            Require(Counts(fixture.Runtime) == (1, 1) && Allocated(type));
            await NativeResource527Access.Retire(fixture.Runtime, image);
            Require(Counts(fixture.Runtime) == (0, 0) && !Allocated(type));
            // Check the old token immediately, before a new type can legitimately
            // reuse native allocator/token storage in another iteration.
            Require(!RegisteredToken(token));
        }
    });

    internal static Task PendingRollbackAndNewOwner() => Run(async fixture =>
    {
        NativeResource527PendingImage first = fixture.Image();
        object firstLoad = fixture.Load(first);
        object type = NativeResource527Access.Open(firstLoad, ResourceName, Create).Type!;
        NativeResource527Access.Publish(firstLoad, first);
        NativeResource527PendingImage pending = fixture.Image();
        object pendingLoad = fixture.Load(pending);
        Require(ReferenceEquals(NativeResource527Access.Open(pendingLoad, ResourceName, Takeover).Type, type));
        await NativeResource527Access.Retire(fixture.Runtime, first);
        Require(Counts(fixture.Runtime) == (1, 1) && Allocated(type));
        ((IDisposable)pendingLoad).Dispose();
        Require(Counts(fixture.Runtime) == (0, 0) && !Allocated(type));

        NativeResource527PendingImage original = fixture.Image();
        object originalLoad = fixture.Load(original);
        object taken = NativeResource527Access.Open(originalLoad, ResourceName, Create).Type!;
        NativeResource527Access.Publish(originalLoad, original);
        NativeResource527PendingImage replacement = fixture.Image();
        object replacementLoad = fixture.Load(replacement);
        Require(ReferenceEquals(NativeResource527Access.Open(replacementLoad, ResourceName, Takeover).Type, taken));
        NativeResource527Access.Publish(replacementLoad, replacement);
        await NativeResource527Access.Retire(fixture.Runtime, original);
        Require(Counts(fixture.Runtime) == (1, 1) && Allocated(taken));
        await NativeResource527Access.Retire(fixture.Runtime, replacement);
        Require(Counts(fixture.Runtime) == (0, 0) && !Allocated(taken));
    });

    internal static Task HeldPayloadAndInvocation() => Run(async fixture =>
    {
        NativeResource527PendingImage image = fixture.Image();
        object load = fixture.Load(image);
        object type = NativeResource527Access.Open(load, ResourceName, Create).Type!;
        NativeResource527Access.Publish(load, image);
        object payload = Call(
            Property(fixture.Runtime, Domain),
            NativeResourceLogical529Names.Allocate,
            type,
            (nuint)PayloadBytes
        )!;
        IDisposable payloadRelease = fixture.Track(() => Call(payload, NativeResourceLogical529Names.Release));
        await ((Task)Call(fixture.Runtime.Modules, NativeResourceLogical529Names.Retire, image.Image)!).WaitAsync(Deadline);
        Require(Counts(fixture.Runtime) == (1, 1) && Allocated(type));
        payloadRelease.Dispose();
        await ((Task)Property(payload, NativeResourceLogical529Names.Retired)).WaitAsync(Deadline);
        Require(Counts(fixture.Runtime) == (0, 0) && !Allocated(type));

        NativeResource527PendingImage active = fixture.Image();
        object activeLoad = fixture.Load(active);
        object empty = NativeResource527Access.Open(activeLoad, ResourceName, Create).Type!;
        NativeResource527Access.Publish(activeLoad, active);
        var pin = (IDisposable)Call(active.Bindings, PhysicalPin, (object?)null)!;
        IDisposable release = fixture.Track(pin.Dispose);
        Task retirement = (Task)Call(active.Bindings, BindingsRetire)!;
        Require(!retirement.IsCompleted && !active.Owner.Disposed);
        // Only the empty type token can disappear. The independent physical
        // invocation/image pin must still prevent native-owner disposal.
        Require(Counts(fixture.Runtime) == (0, 0) && !Allocated(empty));
        release.Dispose();
        await retirement.WaitAsync(Deadline);
        Require(active.Owner.Disposed);
    });

    private static (int DomainTypes, int RegistryTypes) Counts(ProcessRuntime runtime)
    {
        object domain = Property(runtime, Domain);
        int owned;
        lock (Field(domain, Gate))
            owned = ((IList)Field(domain, Types)).Count;
        Type registry = typeof(ProcessRuntime).Assembly.GetType(RegistryType, true)!;
        int registered;
        lock (StaticField(registry, Gate))
            registered = ((IDictionary)StaticField(registry, Types)).Values.Cast<object>()
                .Count(type => ReferenceEquals(Property(type, NativeResourceTypeRetirement575Names.Runtime), runtime));

        return (owned, registered);
    }

    private static bool Allocated(object type)
    {
        lock (Field(type, Gate))
            return ((GCHandle)Field(type, Handle)).IsAllocated;
    }

    private static bool RegisteredToken(nint token)
    {
        Type registry = typeof(ProcessRuntime).Assembly.GetType(RegistryType, true)!;
        lock (StaticField(registry, Gate))
            return ((IDictionary)StaticField(registry, Types)).Contains(token);
    }

    private static object Field(object value, string name)
        => value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(value)!;

    private static object StaticField(Type type, string name)
        => type.GetField(name, BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(Failure);
    }

    private static async Task Run(Func<Fixture, Task> body)
    {
        var fixture = new Fixture();
        var errors = new List<Exception>();
        try
        {
            await body(fixture);
        }
        catch (Exception error)
        {
            errors.Add(error);
        }
        finally
        {
            await fixture.Cleanup(errors);
        }
        if (errors.Count == 1)
            ExceptionDispatchInfo.Capture(errors[0]).Throw();
        if (errors.Count > 1)
            throw new AggregateException(CleanupFailure, errors);
    }

    private sealed class Fixture
    {
        internal ProcessRuntime Runtime { get; } = OwnedGuest506RuntimeFactory.Create();
        private readonly List<NativeResource527PendingImage> images = new();
        private readonly List<IDisposable> loads = new();
        private readonly List<IDisposable> releases = new();

        internal NativeResource527PendingImage Image()
        {
            images.EnsureCapacity(checked(images.Count + 1));
            var image = NativeResource527Access.Pending(Runtime);
            images.Add(image);

            return image;
        }

        internal object Load(NativeResource527PendingImage image)
        {
            loads.EnsureCapacity(checked(loads.Count + 1));
            object load = NativeResource527Access.Begin(Runtime, image);
            loads.Add((IDisposable)load);

            return load;
        }

        internal IDisposable Track(Action release)
        {
            var once = new Once(release);
            releases.Add(once);

            return once;
        }

        internal async Task Cleanup(List<Exception> errors)
        {
            foreach (IDisposable release in releases)
            {
                try
                {
                    release.Dispose();
                }
                catch (Exception error)
                {
                    errors.Add(error);
                }
            }
            foreach (IDisposable load in loads)
            {
                try
                {
                    load.Dispose();
                }
                catch (Exception error)
                {
                    errors.Add(error);
                }
            }
            foreach (NativeResource527PendingImage image in images)
            {
                try
                {
                    ((IDisposable)image.Reservation).Dispose();
                }
                catch (Exception error)
                {
                    errors.Add(error);
                }
                try
                {
                    await ((Task)Call(image.Bindings, BindingsRetire)!).WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    errors.Add(error);
                }
                try
                {
                    await image.Owner.DisposeAsync();
                }
                catch (Exception error)
                {
                    errors.Add(error);
                }
            }
            try
            {
                await Runtime.DisposeAsync().AsTask().WaitAsync(Deadline);
            }
            catch (Exception error)
            {
                errors.Add(error);
            }
        }
    }

    private sealed class Once(Action release) : IDisposable
    {
        private int disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) == 0)
                release();
        }
    }
}
