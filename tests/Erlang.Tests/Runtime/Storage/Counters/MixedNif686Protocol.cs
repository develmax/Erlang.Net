using System.Runtime.InteropServices;
using static Erlang.Tests.MixedNif686Names;
using static Erlang.Tests.ProcessPayloadCore641Access;

namespace Erlang.Tests;

// Managed protocol fixture, not a fabricated NIF binding or a PE/ELF loader test.
// Calls the actual UnmanagedCallersOnly enif_make_copy implementation via its
// authentic unmanaged entrypoint; all environments and backing allocations are real.
internal sealed class MixedNif686Protocol(ProcessRuntime runtime, ModuleCodeImage image) : IDisposable
{
    private object? independent;
    private nuint held;
    internal bool SourceContextOwned { get; private set; }
    internal bool IndependentOwned { get; private set; }
    internal bool DestinationOwned { get; private set; }

    internal ValueTask<Term> Store(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        Check(!(bool)Property(runtime, Profile) && arguments.Count == 1);
        SourceContextOwned = (bool)Property(context, Capability);
        Check(SourceContextOwned);
        object failure = New(FailureType);
        using var scope = (IDisposable)New(ScopeType, New(AllocationsType), failure);
        using var source = (IDisposable)New(
            EnvironmentType,
            context,
            image,
            (nint)0,
            failure
        );
        try
        {
            Call(source, Attach, context);
            Check((bool)Property(source, UsesOwned));
            independent ??= New(EnvironmentType, runtime, image);
            IndependentOwned = (bool)Property(independent, UsesOwned);
            Check(!IndependentOwned);
            nuint original = (nuint)Call(source, Add, arguments[0])!;
            held = CopyTo(independent, original);
            Call(failure, ThrowPending);
            Check(held != 0);

            return ValueTask.FromResult<Term>(Term.A(Ok));
        }
        finally
        {
            // This public opaque callback takes ownership of its exported Host
            // argument and explicitly drops it after the independent copy.
            if (arguments[0] is ResourceReferenceTerm resource && Property(resource, Host) is IDisposable host)
                host.Dispose();
        }
    }

    internal ValueTask<Term> Pop(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        Check(arguments.Count == 0 && independent is not null && held != 0);
        Check(!(bool)Property(runtime, Profile) && !(bool)Property(context, Capability));
        object failure = New(FailureType);
        using var scope = (IDisposable)New(ScopeType, New(AllocationsType), failure);
        using var destination = (IDisposable)New(
            EnvironmentType,
            context,
            image,
            (nint)0,
            failure
        );
        Call(destination, Attach, context);
        DestinationOwned = (bool)Property(destination, UsesOwned);
        Check(!DestinationOwned);
        nuint copied = CopyTo(destination, held);
        Call(failure, ThrowPending);
        Check(copied != 0);
        // The real destination already retained its copy before producer drops.
        Call(independent!, Clear);
        held = 0;
        Term result = (Term)Call(destination, Export, copied)!;
        Call(failure, ThrowPending);

        return ValueTask.FromResult(result); // real destination Dispose precedes Host receipt
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nuint CopyEntry686(nint destination, nuint source);

    private static nuint CopyTo(object destination, nuint source)
    {
        nint entry = typeof(ProcessRuntime).Assembly.GetType(CallbackType, true)!
            .GetMethod(Copy, Members)!.MethodHandle.GetFunctionPointer();

        return Marshal.GetDelegateForFunctionPointer<CopyEntry686>(entry)((nint)Property(destination, Pointer), source);
    }

    private static object New(string name, params object?[] arguments)
        => Activator.CreateInstance(
            typeof(ProcessRuntime).Assembly.GetType(name, true)!,
            ConstructorMembers,
            null,
            arguments,
            null
        )!;

    private static void Check(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }

    public void Dispose() => ((IDisposable?)Interlocked.Exchange(ref independent, null))?.Dispose();
}
