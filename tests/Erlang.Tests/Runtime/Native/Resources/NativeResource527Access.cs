using System.Reflection;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.NativeResource524Names;

namespace Erlang.Tests;

internal static class NativeResource527Access
{
    private const string Module = "resource527_protocol";
    private const string Callbacks = "Erlang.NativeResourceCallbacks527";

    internal static object New(string name, params object?[] args)
        => Activator.CreateInstance(
            typeof(ProcessRuntime).Assembly.GetType(name, true)!,
            Members,
            null,
            args,
            null
        )!;

    internal static NativeResource527PendingImage Pending(ProcessRuntime runtime)
    {
        object image = New(Image, Module);
        object bindings = Property(image, "NifBindings");
        var descriptor = new ModuleCodeDescriptor(
            [],
            [],
            new BitString(new byte[16]),
            Nil.Value
        )
        { DeclaredNifs = [] };
        object reservation = Call(bindings, "Begin", descriptor)!;
        var owner = new NativeResource524ImageOwner();
        object provider = New(
            ImageProvider,
            runtime,
            image,
            New(AllocationOwner),
            (Func<nint>)(() => 0),
            (Func<bool>)(() => false),
            (Func<bool>)(() => owner.Disposed)
        );

        return new(
            image,
            bindings,
            reservation,
            provider,
            owner
        );
    }

    internal static object Table(ProcessRuntime runtime) => Property(Property(runtime, Domain), "ResourceTypes527");

    internal static object Begin(ProcessRuntime runtime, NativeResource527PendingImage image)
        => Call(
            Table(runtime),
            "BeginLoad527",
            image.Provider,
            image.Reservation
        )!;

    internal static (object? Type, int Tried) Open(
        object transaction,
        string name,
        int flags,
        nint destructor = 0
    )
    {
        object callbacks = New(
            Callbacks,
            destructor,
            (nint)0,
            (nint)0,
            (nint)0
        );
        object?[] args = [name, callbacks, flags, null];
        object? type = transaction.GetType().GetMethod("Open527", Members)!.Invoke(transaction, args);

        return (type, (int)args[3]!);
    }

    internal static void Publish(object transaction, NativeResource527PendingImage image)
    {
        Call(transaction, "EndLoading527");
        using var publication = (IDisposable)Call(transaction, "PreparePublication527")!;
        Call(
            image.Bindings,
            "Publish",
            image.Reservation,
            Array.Empty<KeyValuePair<(string Name, int Arity), ErlangFunction>>(),
            image.Owner
        );
        Call(publication, "Commit527");
    }

    internal static async Task Retire(ProcessRuntime runtime, NativeResource527PendingImage image)
    {
        Call(Table(runtime), "RetireImage527", image.Image);
        await ((Task)Call(image.Bindings, "RetireAsync")!).WaitAsync(Deadline);
        NativeResource524Access.Check(image.Owner.Disposed);
    }
}
