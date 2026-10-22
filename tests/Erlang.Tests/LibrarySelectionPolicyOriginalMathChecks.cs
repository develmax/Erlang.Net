using static Erlang.Tests.LibrarySelectionHostData;
using static Erlang.Tests.LibrarySelectionPolicyControlNames;
using static Erlang.Tests.LibrarySelectionPolicyNegativeAccess;
using static Erlang.Tests.LibrarySelectionPolicyNegativeNames;
using static Erlang.Tests.LibrarySelectionCancellationPhaseAccess;

namespace Erlang.Tests;

internal static class LibrarySelectionPolicyOriginalMathChecks
{
    // Root supplies the real pinned image and its actual explicit closure. This witness does not replace original comparisons.
    internal static async Task OriginalMath24(
        ReadOnlyMemory<byte> image,
        IEnumerable<string> dependencyModules,
        IEnumerable<RuntimeModuleSelection> dependencyChoices
    )
    {
        var request = new RuntimeLibrarySelection(dependencyChoices.Append(
            RuntimeModuleSelection.Beam(MathModule, image, dependencyModules)
        ));
        await using var runtime = new ProcessRuntime();
        object? plan = null;
        PreparedBeamImage? prepared = null;
        await runtime.InitializeModulesAsync(registry =>
        {
            var before = Bodies(registry).Where(pair => pair.Key.Module == MathModule)
                .ToDictionary(pair => (pair.Key.Function, pair.Key.Arity), pair => pair.Value);
            Require(before.Count == 1 && before.ContainsKey((MathNative, 1)));
            plan = Prepare(registry, request);
            Publish(registry, plan);
            prepared = (PreparedBeamImage)registry.SourceOwner(MathModule, MathNative, 1)!;
            Require(prepared.NativePrimitiveExports.Count == OriginalMathPrimitiveCount);
            IReadOnlyDictionary<(string Function, int Arity), ErlangFunction> captured = Captured(prepared);
            Require(captured.Count == OriginalMathPrimitiveCount);
            foreach (var pair in before)
            {
                Require(captured.TryGetValue(pair.Key, out ErlangFunction? body) && ReferenceEquals(pair.Value, body));
                Require(ReferenceEquals(registry.SourceOwner(MathModule, pair.Key.Function, pair.Key.Arity), prepared));
            }
        });
        RuntimeLibrarySelectionSnapshot snapshot = await Finish(runtime, plan!, CancellationToken.None);
        RuntimeLibraryModuleSnapshot selected = snapshot.Modules.Single(value => value.Module == MathModule);
        Require(selected.Backend == RuntimeModuleBackend.Beam);
        Require(selected.NativePrimitives.Count == OriginalMathPrimitiveCount);
        Require(selected.NativeMd5.Equals(prepared!.NativeMd5));
        Require(!runtime.Modules.HasOldCode(MathModule));
    }
}
