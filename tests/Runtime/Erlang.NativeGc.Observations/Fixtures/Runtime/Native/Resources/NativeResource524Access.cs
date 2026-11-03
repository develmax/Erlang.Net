using System.Reflection;
using static Erlang.Tests.NativeResource524Names;

namespace Erlang.Tests;

internal static class NativeResource524Access
{
    internal static object Property(object value, string name)
        => value.GetType().GetProperty(name, Members)!.GetValue(value)!;

    internal static object? Call(object value, string name, params object?[] args)
    {
        try
        {
            return value.GetType().GetMethod(name, Members)!.Invoke(value, args);
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    private static object New(string name, params object?[] args)
        => Activator.CreateInstance(
            typeof(ProcessRuntime).Assembly.GetType(name, true)!,
            Members,
            null,
            args,
            null
        )!;

    internal static (object Provider, object Bindings, NativeResource524ImageOwner Owner) Provider(ProcessRuntime runtime)
    {
        object image = New(Image, TypeName);
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
        Call(
            bindings,
            "Publish",
            reservation,
            Array.Empty<KeyValuePair<(string Name, int Arity), ErlangFunction>>(),
            owner
        );
        object provider = New(
            ImageProvider,
            runtime,
            image,
            New(AllocationOwner),
            (Func<nint>)(() => 0),
            (Func<bool>)(() => false),
            (Func<bool>)(() => owner.Disposed)
        );

        return (provider, bindings, owner);
    }

    internal static object Allocate(ProcessRuntime runtime, object provider, nint destructor)
    {
        object domain = Property(runtime, Domain);
        object type = Call(
            domain,
            "OpenDestructorOnly524",
            provider,
            TypeName,
            destructor,
            (nint)0,
            (nint)0,
            (nint)0
        )!;

        return Call(
            domain,
            "Allocate524",
            type,
            (nuint)PayloadBytes
        )!;
    }

    internal static BitString Binary(ProcessRuntime runtime, object payload)
        => (BitString)Call(
            Property(runtime, Domain),
            "Binary524",
            payload,
            (nint)Property(payload, "Pointer524"),
            (nuint)PayloadBytes
        )!;

    internal static void Check(bool condition)
    {
        if (!condition) throw new InvalidOperationException(nameof(NativeResource524Checks));
    }
}
