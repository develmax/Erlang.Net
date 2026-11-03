using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using Erlang.Differential;
using static Erlang.Tests.NativeGuest535Names;

namespace Erlang.Tests;

internal static class NativeGuest535Checks
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void Destructor(nint environment, nint payload);

    internal static Task ReadersAndPublicRefusal() => WithResource(async (
        runtime,
        actor,
        binary,
        payload
    ) =>
    {
        Term value = await actor.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            GuestModule,
            Read,
            binary
        ));
        Check(value is TupleTerm { Items.Count: 3 });
        var tuple = (TupleTerm)value;
        Term expectedList = Term.List(Enumerable.Range(0, NativeResource524Names.PayloadBytes).Select(n => Term.I(n)).ToArray());
        Check(tuple.Items[0].Equals(expectedList));
        Type layout = typeof(ProcessRuntime).Assembly.GetType(MagicLayout, true)!;
        int offset = (int)layout.GetProperty(UserOffset, BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        nint user = (nint)NativeResource524Access.Property(payload, NativeGuest535Names.Pointer);
        long referenced = Marshal.ReadInt64(user - offset, NativeResource527TestLayout.OriginalSize);
        Check(tuple.Items[1].Equals(Term.I(referenced)) && tuple.Items[2].Equals(Term.I(NativeResource524Names.PayloadBytes)));
        // A genuine public native-bearing result still REQUIRES explicit host custody.
        await Refused(async () => _ = await actor.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            GuestModule,
            Echo,
            binary
        )));
        bool readable = await actor.InvokeAsync(async context =>
        {
            using var host = new NativeTermHostScope524(runtime);
            Term allowed = await runtime.Modules.Call(
                context,
                GuestModule,
                Echo,
                binary
            );

            return allowed is BitString bytes && bytes.ToArray().SequenceEqual(Enumerable.Range(0, NativeResource524Names.PayloadBytes).Select(n => (byte)n));
        });
        Check(readable);
    });

    internal static Task PublicReregisterStaysOpaque() => WithResource(async (
        runtime,
        actor,
        binary,
        _
    ) =>
    {
        var functions = (Dictionary<(string Module, string Function, int Arity), ErlangFunction>)typeof(ModuleRegistry).GetField(Functions, Members)!.GetValue(runtime.Modules)!;
        ErlangFunction oldCanonical = functions[(NativeGuest535Names.BinaryModule, ReferencedFunction, 1)];
        runtime.Modules.DeleteModule(NativeGuest535Names.BinaryModule);
        // Same exact former canonical delegate, but a PUBLIC new registration.
        runtime.Modules.Register(
            NativeGuest535Names.BinaryModule,
            ReferencedFunction,
            1,
            oldCanonical
        );
        await Refused(async () => _ = await actor.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            GuestModule,
            Referenced,
            binary
        )));
    });

    internal static Task ManagedNifProviderStaysOpaque() => WithResource(async (
        runtime,
        actor,
        binary,
        _
    ) =>
    {
        Type imageType = typeof(ProcessRuntime).Assembly.GetType(NativeImage, true)!;
        object image = Activator.CreateInstance(
            imageType,
            Members,
            null,
            new object[] { FakeModule },
            null
        )!;
        object bindings = NativeResource524Access.Property(image, NativeBindings);
        var descriptor = new ModuleCodeDescriptor(
            new[] { (Read, 1) },
            new[] { (Read, 1) },
            new BitString(new byte[16]),
            Nil.Value
        )
        { DeclaredNifs = new[] { (Read, 1) } };
        object reservation = NativeResource524Access.Call(bindings, Begin, descriptor)!;
        int called = 0;
        ErlangFunction opaque = (_, _) =>
 {
     Interlocked.Increment(ref called);

     return ValueTask.FromResult<Term>(Nil.Value);
 };
        var owner = new NativeResource524ImageOwner();
        NativeResource524Access.Call(
            bindings,
            Publish,
            reservation,
            new[] { new KeyValuePair<(string Name, int Arity), ErlangFunction>((Read, 1), opaque) },
            owner
        );
        try
        {
            using var admitted = (IDisposable)NativeResource524Access.Call(
                bindings,
                Admit,
                Read,
                1
            )!;
            Check(!(bool)NativeResource524Access.Property(admitted, GuestNative));
            await Refused(async () => _ = await actor.InvokeAsync(async context =>
            {
                ValueTask<Term> result;
                try
                {
                    result = (ValueTask<Term>)typeof(ProcessContext).GetMethod(InvokeImage, Members)!.Invoke(context, new object[] { admitted, new Term[] { binary } })!;
                }
                catch (TargetInvocationException error) when (error.InnerException is { } inner)
                {
                    ExceptionDispatchInfo.Capture(inner).Throw();
                    throw;
                }

                return await result;
            }));
            Check(called == 0);
        }
        finally
        {
            await ((Task)NativeResource524Access.Call(bindings, RetireImage)!).WaitAsync(Deadline);
            Check(owner.Disposed);
        }
    });

    private static async Task WithResource(Func<ProcessRuntime, RuntimeNodeFoundationHostActor, BitString, object, Task> exercise)
    {
        using var guest = GeneratedModuleCompiler.Compile(Source);
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        guest.Definition.Register(runtime.Modules);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext context = await actor.Ready;
        var provider = NativeResource524Access.Provider(runtime);
        int destroyed = 0;
        Destructor callback = (_, _) => Interlocked.Increment(ref destroyed);
        object payload = NativeResource524Access.Allocate(runtime, provider.Provider, Marshal.GetFunctionPointerForDelegate(callback));
        nint pointer = (nint)NativeResource524Access.Property(payload, NativeGuest535Names.Pointer);
        for (int n = 0; n < NativeResource524Names.PayloadBytes; n++) Marshal.WriteByte(pointer, n, (byte)n);
        BitString binary = NativeResource524Access.Binary(runtime, payload);
        try
        {
            await exercise(
                runtime,
                actor,
                binary,
                payload
            );
        }
        finally
        {
            NativeResource524Access.Call(payload, ReleaseNative);
            NativeResource524Access.Call(NativeResource524Access.Property(context, Heap), Collect);
            await ((Task)NativeResource524Access.Property(payload, Retired)).WaitAsync(Deadline);
            await ((Task)NativeResource524Access.Call(provider.Bindings, RetireImage)!).WaitAsync(Deadline);
            Check(destroyed == 1 && provider.Owner.Disposed);
            GC.KeepAlive(callback);
        }
    }

    private static async Task Refused(Func<Task> operation)
    {
        try
        {
            await operation();
        }
        catch (NotSupportedException error) when (error.Message == ScopeRequired)
        {
            return;
        }
        throw new InvalidOperationException(Failure);
    }

    private static void Check(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }
}
