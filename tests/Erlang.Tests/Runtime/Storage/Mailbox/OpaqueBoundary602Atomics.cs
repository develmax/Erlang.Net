using System.Runtime.InteropServices;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.OpaqueBoundary602Names;

namespace Erlang.Tests;

// Same-runtime actual413/586 allocation recipe; no synthetic backing or fake
// canonical delegate. Guest/header authority comes from real frame/send code.
internal sealed class OpaqueBoundary602Atomics
{
    private IDisposable? invocation, producer;
    private object? owner;
    internal object Array { get; private set; } = null!;
    internal object Root { get; private set; } = null!;
    internal ResourceReferenceTerm Reference { get; private set; } = null!;

    internal void Initialize(ProcessRuntime runtime)
    {
        owner = Property(runtime, AtomicsOwner);
        invocation = (IDisposable)Call(owner, Enter)!;
        Array = Call(
            owner,
            Create,
            1UL,
            true
        )!;
        Call(
            Array,
            BindOwner,
            owner,
            runtime
        );
        object kind = Enum.Parse(typeof(ProcessRuntime).Assembly.GetType(KindType, true)!, Producer);
        producer = (IDisposable)Call(Array, Retain, kind)!;
        Root = Activator.CreateInstance(
            typeof(ProcessRuntime).Assembly.GetType(RootType, true)!,
            Members,
            null,
            [owner, Array],
            null
        )!;
        Reference = (ResourceReferenceTerm)Call(runtime.ReferenceResources, CreateReference, Root)!;
        WithCell(pointer => Marshal.WriteInt64(pointer, AtomicsSentinel));
    }

    internal void ReleaseCreation()
    {
        Exception? first = null, second = null;
        try
        {
            Interlocked.Exchange(ref producer, null)?.Dispose();
        }
        catch (Exception error)
        {
            first = error;
        }
        try
        {
            Interlocked.Exchange(ref invocation, null)?.Dispose();
        }
        catch (Exception error)
        {
            second = error;
        }
        if (first is not null || second is not null) throw new AggregateException(new[] { first, second }.OfType<Exception>());
    }

    internal long Count(string field)
    {
        lock (Field(Array, Gate)!) return (long)Field(Array, field)!;
    }

    internal void WithCell(Action<nint> body)
    {
        using var pin = (IDisposable?)Call(Array, AcquirePin) ?? throw new InvalidOperationException(Failure);
        nint memory;
        lock (Field(Array, Gate)!) memory = (nint)Field(Array, ArrayMemory)!;
        int bytes = Marshal.SizeOf(typeof(ProcessRuntime).Assembly.GetType(ArrayHeader, true)!);
        body(memory + bytes);
    }

    internal static object? Field(object value, string name) => value.GetType().GetField(name, Members)!.GetValue(value);

    internal void Cleanup(List<Exception> errors)
    {
        bool hadProducer = producer is not null;
        try
        {
            ReleaseCreation();
        }
        catch (Exception error)
        {
            errors.Add(error);
        }
        if (!hadProducer && Root is null && Array is not null && owner is not null)
            try
            {
                Call(owner, OwnerRetire, Array);
            }
            catch (Exception error)
            {
                errors.Add(error);
            }
    }
}
