using static Erlang.Tests.LibrarySelectionHostData;

namespace Erlang.Tests;

internal static class MathBackendChecks
{
    internal static async Task Mfa(string function, int arity)
    {
        await using var runtime = new ProcessRuntime();
        Term rows = runtime.ImportExternalTerm(LibraryDependencyOriginalHostChecks.ReadRetainedResource(MathBackendNames.OriginalValues)).Term;
        string caseName = arity == 0 ? MathBackendNames.ConstantPrefix + function : function + MathBackendNames.OrdinarySuffix;
        Term? expected = null;
        for (Term remaining = rows; remaining is Cons row; remaining = row.Tail)
        {
            var pair = (TupleTerm)row.Head;
            if (pair.Items[0] is Atom name && name.Name == caseName)
            {
                expected = ((TupleTerm)pair.Items[1]).Items[1];
            }
        }
        Require(expected is FloatTerm);
        Term[] arguments = arity switch
        {
            0 => [],
            2 => [new FloatTerm(1.5), new FloatTerm(2.0)],
            _ => [new FloatTerm(function switch
            {
                MathBackendNames.Acosh => 1.5,
                MathBackendNames.Log or MathBackendNames.Log2 or MathBackendNames.Log10 or MathBackendNames.Sqrt => 2.0,
                _ => 0.5
            })]
        };
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term actual = await runtime.Modules.Call(
                context,
                MathBackendNames.Module,
                function,
                arguments
            );
            Require(ExternalTermFormat.Encode(expected!).AsSpan().SequenceEqual(ExternalTermFormat.Encode(actual)));

            return true;
        });
    }

    internal static async Task ManagedSelection()
    {
        var request = new RuntimeLibrarySelection([RuntimeModuleSelection.Managed(MathBackendNames.Module)]);
        await using var runtime = await ProcessRuntime.CreateAsync(request);
        Require(runtime.Modules.Exports.Count(export => export.Module == MathBackendNames.Module) == MathBackendNames.ExportCount);
        RuntimeLibraryModuleSnapshot selected = runtime.LibrarySelection.Modules.Single();
        Require(selected.Module == MathBackendNames.Module && selected.Backend == RuntimeModuleBackend.Managed);
        Require(selected.ManagedAssemblies.Count > 0);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term pi = await runtime.Modules.Call(context, MathBackendNames.Module, MathBackendNames.Pi);
            Term tau = await runtime.Modules.Call(context, MathBackendNames.Module, MathBackendNames.Tau);
            Require(pi is FloatTerm p && BitConverter.DoubleToUInt64Bits(p.Value) == BitConverter.DoubleToUInt64Bits(Math.PI));
            Require(tau is FloatTerm t && BitConverter.DoubleToUInt64Bits(t.Value) == BitConverter.DoubleToUInt64Bits(Math.Tau));

            return true;
        });
    }
}
