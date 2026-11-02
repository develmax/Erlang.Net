using System.Reflection;
using Erlang.Compiler;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.FunctionIdentityValue255Names;
using static Erlang.Tests.Fingerprint274Names;

namespace Erlang.Tests;

internal static class Fingerprint274Checks
{
    internal static ModuleCodeDescriptor Create(
        BitString fingerprint,
        ModuleAttributePolicy policy = ModuleAttributePolicy.AddDefaultVersion
    ) => new(
        [],
        [],
        fingerprint,
        Nil.Value,
        attributePolicy: policy
    );

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(FingerprintFailure274);
    }

    internal static void Verify(ModuleCodeDescriptor descriptor, byte[] expected)
    {
        BitString actual = descriptor.Fingerprint;
        // Check storage metadata BEFORE any data/length getter: a failed old
        // implementation must not dereference an already-retired native input.
        Require(Property(actual, Owned) is null && Property(actual, OwnedHeap) is null
            && Property(Property(actual, Backing), Native) is null);
        Require(actual.BitLength == FingerprintBits274 && actual.ToArray().SequenceEqual(expected));
    }

    internal static void PublishAndVerify(ProcessRuntime runtime, ModuleCodeDescriptor descriptor, byte[] expected)
    {
        Verify(descriptor, expected);
        runtime.Modules.PublishModuleCode(FingerprintModule274, descriptor);
        Term observed = runtime.Modules.ModuleInformation(FingerprintModule274, Md5Key274);
        Require(ReferenceEquals(observed, descriptor.Fingerprint));
        Require(((BitString)observed).ToArray().SequenceEqual(expected));
    }

    private static void BadArgument(BitString value, ModuleAttributePolicy policy)
    {
        try
        {
            _ = Create(value, policy);
        }
        catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
        {
            return;
        }

        throw new InvalidOperationException(FingerprintFailure274);
    }

    internal static void RequireStale(BitString input)
    {
        try
        {
            _ = Create(input);
        }
        catch (ObjectDisposedException)
        {
            return;
        }

        throw new InvalidOperationException(FingerprintFailure274);
    }

    internal static void ManagedAndWritable()
    {
        byte[] expected = Enumerable.Range(0, FingerprintBytes274).Select(value => (byte)value).ToArray();
        var stable = new BitString(expected);
        ModuleCodeDescriptor descriptor = Create(stable);
        Require(ReferenceEquals(descriptor.Fingerprint, stable));
        Verify(descriptor, expected);
        var invalid = (ModuleAttributePolicy)InvalidPolicy274;
        foreach (int bytes in new[] { 0, FingerprintBytes274 - 1, FingerprintBytes274 + 1 })
        {
            BadArgument(new BitString(new byte[bytes]), ModuleAttributePolicy.AddDefaultVersion);
            BadArgument(new BitString(new byte[bytes]), invalid);
        }
        BadArgument(new BitString(expected, PartialBits274), ModuleAttributePolicy.AddDefaultVersion);
        BadArgument(new BitString(expected, PartialBits274), invalid);
        bool policyRejected = false;
        try
        {
            _ = Create(stable, invalid);
        }
        catch (ArgumentOutOfRangeException error) when (error.ParamName == PolicyArgument274)
        {
            policyRejected = true;
        }
        Require(policyRejected);
        bool nullRejected = false;
        try
        {
            _ = Create(null!, invalid);
        }
        catch (NullReferenceException)
        {
            nullRejected = true;
        }
        Require(nullRejected);
        MethodInfo initialize = typeof(BitString).GetMethod(InitializeWritable, BindingFlags.Static | BindingFlags.NonPublic)!;
        MethodInfo append = typeof(BitString).GetMethod(Append, BindingFlags.Static | BindingFlags.NonPublic)!;
        var empty = (BitString)initialize.Invoke(null, [Capacity])!;
        var writer = (BitString)append.Invoke(null, [empty, new BitString(expected)])!;
        ModuleCodeDescriptor copied = Create(writer);
        _ = append.Invoke(null, [writer, new BitString(new byte[FingerprintBytes274 * 2])]);
        Verify(copied, expected);
    }

    internal static (ModuleCodeDescriptor Descriptor, byte[] Bytes, BitString Source) NativeWritable(IDisposable frame)
    {
        byte[] expected = Enumerable.Range(0, FingerprintBytes274).Select(value => (byte)value).ToArray();
        MethodInfo initialize = typeof(BitString).GetMethod(InitializeWritable, BindingFlags.Static | BindingFlags.NonPublic)!;
        MethodInfo append = typeof(BitString).GetMethod(Append, BindingFlags.Static | BindingFlags.NonPublic)!;
        var empty = (BitString)initialize.Invoke(null, [Capacity])!;
        var writer = (BitString)append.Invoke(null, [empty, new BitString(expected)])!;
        var physical = (BitString)Call(
            frame,
            Adopt,
            (int)Call(frame, Reserve)!,
            writer
        )!;
        object native = Property(Property(physical, Backing), Native);
        Require((bool)Property(native, IsWritable));
        ModuleCodeDescriptor descriptor = Create(physical);
        Require((bool)Property(native, IsWritable));
        Verify(descriptor, expected);

        return (descriptor, expected, physical);
    }

    internal static async Task<ModuleCodeDescriptor> PhysicalResource(
        RuntimeNodeFoundationHostActor actor,
        BitString input
    )
    {
        ModuleCodeDescriptor? descriptor = null;
        BitString? source = null;
        byte[] expected = input.ToArray();
        await actor.InvokeAsync(async context =>
        {
            _ = await Execution.EvaluateAsync(new Expr.Literal(Nil.Value), context);
            object heap = Property(context, Heap);
            using (var frame = (IDisposable)Call(heap, Enter)!)
            {
                source = (BitString)Call(
                    frame,
                    Adopt,
                    (int)Call(frame, Reserve)!,
                    input
                )!;
                Require(Property(source, Owned) is not null);
                descriptor = Create(source);
                Require(!ReferenceEquals(descriptor.Fingerprint, source));
                Verify(descriptor, expected);
            }

            return true;
        });
        await actor.InvokeAsync(context =>
        {
            Call(
                Property(context, Heap),
                Collect,
                true,
                Major
            );
            RequireStale(source!);
            Verify(descriptor!, expected);

            return Task.FromResult(true);
        });

        return descriptor!;
    }
}
