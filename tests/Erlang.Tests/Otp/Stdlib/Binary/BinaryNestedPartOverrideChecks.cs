using System.Reflection;
using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BinaryNestedPartOverrideChecks
{
    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime(1);
        ModuleRegistry registry = runtime.Modules;
        const BindingFlags members = BindingFlags.Instance | BindingFlags.NonPublic;
        object gate = typeof(ModuleRegistry).GetField(BinaryNestedPartOverrideNames.Gate, members)!.GetValue(registry)!;
        var functions = (Dictionary<(string Module, string Function, int Arity), ErlangFunction>)typeof(ModuleRegistry)
            .GetField(BinaryNestedPartOverrideNames.Functions, members)!.GetValue(registry)!;
        MethodInfo selection = typeof(ModuleRegistry).GetMethod(BinaryNestedPartOverrideNames.Selection, members)!;
        ErlangFunction originalPart;
        ErlangFunction originalMatch;
        lock (gate)
        {
            originalPart = functions[(BinaryNestedPartOverrideNames.Module, BinaryNestedPartOverrideNames.Part, 2)];
            originalMatch = functions[(BinaryNestedPartOverrideNames.Module, BinaryNestedPartOverrideNames.Match, 3)];
        }
        int calls = 0, replacementCalls = 0;
        ErlangFunction observedPart = async (context, arguments) =>
        {
            var key = (BinaryNestedPartOverrideNames.Module, BinaryNestedPartOverrideNames.Part, 2);
            lock (gate)
                Require(selection.Invoke(registry, [key, functions[key]])!.ToString() == BinaryNestedPartOverrideNames.Opaque);
            object owned = typeof(ProcessContext).GetProperty(BinaryNestedPartOverrideNames.Current, members)!.GetValue(context)!;
            Require(owned is not null && owned.GetType().GetProperty(BinaryNestedPartOverrideNames.Kind, members)!
                .GetValue(owned)!.ToString() == BinaryNestedPartOverrideNames.Opaque);
            calls++;
            var binary = (BitString)arguments[0];
            if (binary.BitLength == BinaryNestedPartOverrideNames.ReplacementSize * BinaryNestedPartOverrideNames.BitsPerByte)
            {
                replacementCalls++;
                // Genuine canonical part leaves a list insertion's append writer
                // unpinned. This PUBLIC opaque observer must instead see an actual
                // Host export, with the append writer revoked before callback entry.
                Require((int)typeof(BitString).GetProperty(BinaryNestedPartOverrideNames.ReferencedBytes, members)!
                    .GetValue(binary)! == BinaryNestedPartOverrideNames.ReplacementSize);
            }

            return await originalPart(context, arguments);
        };
        // Public Register cannot replace an existing signature. Remove this
        // isolated module once, then genuinely register the reviewed wrappers and
        // public opaque observers; no private dictionary/authority mutation.
        registry.DeleteModule(BinaryNestedPartOverrideNames.Module);
        registry.Register(
            BinaryNestedPartOverrideNames.Module,
            BinaryNestedPartOverrideNames.Part,
            2,
            observedPart
        );
        registry.Register(
            BinaryNestedPartOverrideNames.Module,
            BinaryNestedPartOverrideNames.Match,
            3,
            originalMatch
        );
        BinaryWrappersModule.Register(registry);
        new Parser(BinaryNestedPartOverrideNames.Source).ParseModule().Register(registry);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        Term result = await actor.InvokeAsync(async context => await registry.Call(context,
            BinaryNestedPartOverrideNames.SourceModule, BinaryNestedPartOverrideNames.Entry));
        byte[] expected = new byte[BinaryNestedPartOverrideNames.ReplacementSize + 1];
        expected[0] = 1;
        Array.Fill(
            expected,
            (byte)7,
            1,
            BinaryNestedPartOverrideNames.ReplacementSize
        );
        Require(result is BitString output && output.ToArray().SequenceEqual(expected)
            && replacementCalls == 2 && calls == 5);
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(BinaryNestedPartOverrideNames.Failure);
    }
}
