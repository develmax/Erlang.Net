using System.Reflection;

namespace Erlang.Tests;

internal static class BinaryWrapperCapabilityChecks
{
    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime(1);
        ModuleRegistry registry = runtime.Modules;
        const BindingFlags members = BindingFlags.Instance | BindingFlags.NonPublic;
        object gate = typeof(ModuleRegistry).GetField(BinaryWrapperCapabilityNames.Gate, members)!.GetValue(registry)!;
        var functions = (Dictionary<(string Module, string Function, int Arity), ErlangFunction>)typeof(ModuleRegistry)
            .GetField(BinaryWrapperCapabilityNames.Functions, members)!.GetValue(registry)!;
        MethodInfo selection = typeof(ModuleRegistry).GetMethod(BinaryWrapperCapabilityNames.Selection, members)!;
        var originals = new List<(int Arity, ErlangFunction Body)>();
        foreach (int arity in new[] { 3, 4 })
        {
            var key = (BinaryWrapperCapabilityNames.Module, BinaryWrapperCapabilityNames.Function, arity);
            ErlangFunction original;
            lock (gate)
            {
                original = functions[key];
                Require(selection.Invoke(registry, [key, original])!.ToString() == BinaryWrapperCapabilityNames.Admitted);
                ErlangFunction forged = (_, values) => ValueTask.FromResult(values[0]);
                Require(selection.Invoke(registry, [key, forged])!.ToString() == BinaryWrapperCapabilityNames.Opaque);
            }
            // Even replaying the EXACT reviewed delegate through the public
            // registration API must not mint the internal publication capability.
            originals.Add((arity, original));
        }
        Require(registry.DeleteModule(BinaryWrapperCapabilityNames.Module).Equals(Term.A(ErlangBooleanAtoms.True)));
        foreach ((int arity, ErlangFunction original) in originals)
        {
            var key = (BinaryWrapperCapabilityNames.Module, BinaryWrapperCapabilityNames.Function, arity);
            registry.Register(
                key.Item1,
                key.Item2,
                arity,
                original
            );
            lock (gate)
                Require(selection.Invoke(registry, [key, original])!.ToString() == BinaryWrapperCapabilityNames.Opaque);
        }
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(BinaryWrapperCapabilityNames.Failure);
    }
}
