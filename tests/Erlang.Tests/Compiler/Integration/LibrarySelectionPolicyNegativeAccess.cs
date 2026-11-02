using System.Reflection;
using static Erlang.Tests.LibrarySelectionPolicyNegativeNames;
using static Erlang.Tests.LibrarySelectionHostData;

namespace Erlang.Tests;

internal static class LibrarySelectionPolicyNegativeAccess
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.NonPublic;

    internal static Dictionary<(string Module, string Function, int Arity), ErlangFunction> Bodies(ModuleRegistry registry)
        => (Dictionary<(string Module, string Function, int Arity), ErlangFunction>)typeof(ModuleRegistry)
            .GetField(Functions, Members)!.GetValue(registry)!;

    internal static IReadOnlyDictionary<(string Function, int Arity), ErlangFunction> Captured(PreparedBeamImage prepared)
    {
        object identity = typeof(PreparedBeamImage).GetProperty(ExecutionIdentity, Members)!.GetValue(prepared)!;

        return (IReadOnlyDictionary<(string Function, int Arity), ErlangFunction>)identity.GetType()
            .GetProperty(NativePrimitives, Members)!.GetValue(identity)!;
    }

    internal static void RejectPrepare(
        ModuleRegistry registry,
        RuntimeLibrarySelection request,
        string expectedMessage,
        string? expectedInnerMessage = null
    )
    {
        var before = Bodies(registry).ToDictionary(pair => pair.Key, pair => pair.Value);
        var modules = registry.LoadedModules.Order(StringComparer.Ordinal).ToArray();
        var images = modules.ToDictionary(module => module, registry.CurrentCodeImage, StringComparer.Ordinal);
        var old = modules.ToDictionary(module => module, registry.HasOldCode, StringComparer.Ordinal);
        var owners = before.Keys.ToDictionary(key => key, key => registry.SourceOwner(key.Module, key.Function, key.Arity));
        bool refused = false;
        object? unexpected = null;
        try
        {
            unexpected = LibrarySelectionCancellationPhaseAccess.Prepare(registry, request);
        }
        catch (TargetInvocationException error) when (
            error.InnerException is InvalidOperationException policy
            && policy.GetType() == typeof(InvalidOperationException)
            && policy.Message == expectedMessage
            && (expectedInnerMessage is null
                ? policy.InnerException is null
                : policy.InnerException is InvalidDataException detail
                    && detail.GetType() == typeof(InvalidDataException)
                    && detail.Message == expectedInnerMessage))
        {
            refused = true;
        }
        if (unexpected is IDisposable plan) plan.Dispose();
        Require(refused);
        Require(modules.SequenceEqual(registry.LoadedModules.Order(StringComparer.Ordinal)));
        Require(before.Count == Bodies(registry).Count);
        foreach (var pair in before)
        {
            Require(ReferenceEquals(Bodies(registry)[pair.Key], pair.Value));
            Require(ReferenceEquals(registry.SourceOwner(pair.Key.Module, pair.Key.Function, pair.Key.Arity), owners[pair.Key]));
        }
        foreach (string module in modules)
        {
            Require(ReferenceEquals(registry.CurrentCodeImage(module), images[module]));
            Require(registry.HasOldCode(module) == old[module]);
        }
    }

    internal static void RejectFactory(Action action)
    {
        bool refused = false;
        try
        {
            action();
        }
        catch (ArgumentException)
        {
            refused = true;
        }
        Require(refused);
    }
}
