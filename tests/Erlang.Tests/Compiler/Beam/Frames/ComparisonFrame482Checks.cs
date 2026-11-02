using Erlang;
using static Erlang.Tests.ComparisonFrame482Names;

namespace Erlang.Tests;

internal static class ComparisonFrame482Checks
{
    internal static Task Uninitialized() => Refuse(UninitializedImage);

    internal static Task Opaque() => Refuse(OpaqueImage);

    internal static Task Roots() => Refuse(RootsImage);

    internal static Task Failable() => Refuse(FailableImage);

    internal static Task Destination() => Refuse(DestinationImage);

    private static async Task Refuse(string name)
    {
        await using var runtime = new ProcessRuntime();
        bool refused = false;
        await global::Erlang.Tests.PreparedBeamFixtureScope171.RunOutsideAsync(
            runtime,
            (registry, scope) =>
        {
            try
            {
                scope.Prepare(() => registry.PrepareBeam(ComparisonOriginal482Checks.Read(name), new BeamDependencySelection(ComparisonOriginal482Names.Imports)));
            }
            catch (InvalidDataException error) when (error.Message == FrameDiagnostic)
            {
                refused = true;
            }

            return ValueTask.CompletedTask;

        }
        );
        ComparisonOriginal482Checks.Require(refused);
    }
}
