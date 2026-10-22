using System.Collections;
using System.Reflection;
using Erlang;
using static Erlang.Tests.NativeStubShape400Names;

namespace Erlang.Tests;

internal static class NativeStubShape400Checks
{
    internal static Task Run()
    {
        Check(Recognized(NativeStubShape400Bytes.Image(ModuleName, FunctionName, Undefined)));
        Check(Recognized(NativeStubShape400Bytes.Image(ModuleName, FunctionName, Undef)));
        Check(!Recognized(NativeStubShape400Bytes.Image(OrdinaryModule, FunctionName, Undefined)));
        Check(!Recognized(NativeStubShape400Bytes.Image(ModuleName, OrdinaryFunction, Undefined)));
        Check(!Recognized(NativeStubShape400Bytes.Image(
            ModuleName,
            FunctionName,
            Undefined,
            arity: 2
        )));
        Check(!Recognized(NativeStubShape400Bytes.Image(ModuleName, FunctionName, OtherReason)));
        Check(!Recognized(NativeStubShape400Bytes.Image(
            ModuleName,
            FunctionName,
            Undefined,
            extraInstruction: true
        )));
        Check(!Recognized(NativeStubShape400Bytes.Image(
            ModuleName,
            FunctionName,
            Undefined,
            import: OrdinaryFunction
        )));
        Check(!Recognized(NativeStubShape400Bytes.Image(
            ModuleName,
            FunctionName,
            Undefined,
            destination: 1
        )));
        Check(!Recognized(NativeStubShape400Bytes.Image(
            ModuleName,
            FunctionName,
            Undefined,
            callArity: 2,
            importArity: 2
        )));
        Check(RefusedOrNotRecognized(NativeStubShape400Bytes.Image(
            ModuleName,
            FunctionName,
            Undefined,
            importArity: 2
        )));
        Check(!Recognized(NativeStubShape400Bytes.Image(
            ModuleName,
            FunctionName,
            Undefined,
            importModule: OrdinaryModule
        )));
        // Recognition alone must not supply a missing native handler.
        var registry = new ModuleRegistry();
        CoreModules.Register(registry);
        var before = LibrarySelectionPolicyNegativeAccess.Bodies(registry).ToDictionary(entry => entry.Key, entry => entry.Value);
        string[] modules = registry.LoadedModules.Order(StringComparer.Ordinal).ToArray();
        byte[] unbound = NativeStubShape400Bytes.Image(
            ModuleName,
            UnboundFunction,
            Undefined,
            arity: 2
        );
        Check(Recognized(unbound));
        try
        {
            registry.PrepareBeam(unbound, new BeamDependencySelection([ErlangModule]));
            throw new InvalidOperationException(Invariant);
        }
        catch (NotSupportedException)
        {
        }
        Check(!registry.HasExport(ModuleName, UnboundFunction, 2));
        Check(modules.SequenceEqual(registry.LoadedModules.Order(StringComparer.Ordinal)));
        Check(before.Count == LibrarySelectionPolicyNegativeAccess.Bodies(registry).Count);
        foreach (var entry in before)
            Check(ReferenceEquals(LibrarySelectionPolicyNegativeAccess.Bodies(registry)[entry.Key], entry.Value));

        // A recognized canonical signature captures its real body rather than a placeholder.
        ErlangFunction canonicalBody = before[(ModuleName, FunctionName, 1)];
        foreach (string reason in new[] { Undefined, Undef })
        {
            byte[] canonical = NativeStubShape400Bytes.Image(ModuleName, FunctionName, reason);
            PreparedBeamImage prepared = registry.PrepareBeam(canonical, new BeamDependencySelection([ErlangModule]));
            Check(prepared.NativePrimitiveExports.Count == 1);
            var captured = LibrarySelectionPolicyNegativeAccess.Captured(prepared);
            Check(captured.Count == 1 && ReferenceEquals(captured[(FunctionName, 1)], canonicalBody));
            Check(registry.SourceOwner(ModuleName, FunctionName, 1) is null);
        }
        Check(before.Count == LibrarySelectionPolicyNegativeAccess.Bodies(registry).Count);
        foreach (var entry in before)
            Check(ReferenceEquals(LibrarySelectionPolicyNegativeAccess.Bodies(registry)[entry.Key], entry.Value));

        return Task.CompletedTask;
    }

    private static bool RefusedOrNotRecognized(byte[] bytes)
    {
        try
        {
            return !Recognized(bytes);
        }
        catch (Exception failure) when (failure is InvalidDataException or NotSupportedException)
        {
            // A malformed import/call arity pair can be refused by linking before classification.
            return true;
        }
    }

    private static bool Recognized(byte[] bytes)
    {
        BeamModule module = BeamModule.Prepare(bytes);
        object code = typeof(BeamModule).GetField(CodeField, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(module)!;
        IEnumerable functions = (IEnumerable)code.GetType().GetProperty(Functions, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(code)!;
        object function = functions.Cast<object>().Single();
        Type type = typeof(BeamModule).Assembly.GetType(Detector, true)!;

        return (bool)type.GetMethod(IsStub, BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [code, function])!;
    }

    private static void Check(bool value)
    {
        if (!value)
            throw new InvalidOperationException(Invariant);
    }
}
