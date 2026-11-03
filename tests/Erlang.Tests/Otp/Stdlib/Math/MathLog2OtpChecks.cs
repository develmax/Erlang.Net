using static Erlang.Tests.LibrarySelectionHostData;

namespace Erlang.Tests;

internal static class MathLog2OtpChecks
{
    internal static async Task Run()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }
        RuntimeModuleSelection[] choices =
        [
            RuntimeModuleSelection.Managed(MathLog2OtpNames.Module),
            RuntimeModuleSelection.Beam(MathLog2OtpNames.Module,
                LibraryDependencyOriginalHostChecks.ReadRetainedResource(MathLog2OtpNames.OriginalImage))
        ];
        foreach (RuntimeModuleSelection choice in choices)
        {
            await using var runtime = await ProcessRuntime.CreateAsync(new RuntimeLibrarySelection([choice]));
            await using var actor = new RuntimeNodeFoundationHostActor(runtime);
            await actor.InvokeAsync(async context =>
            {
                Term aboveOne = await runtime.Modules.Call(
                    context,
                    MathLog2OtpNames.Module,
                    MathLog2OtpNames.Function,
                    new FloatTerm(BitConverter.UInt64BitsToDouble(MathLog2OtpNames.AboveOneInput))
                );
                Term power = await runtime.Modules.Call(
                    context,
                    MathLog2OtpNames.Module,
                    MathLog2OtpNames.Function,
                    Term.I(MathLog2OtpNames.PowerOfTwoInput)
                );
                Require(aboveOne is FloatTerm first
                    && BitConverter.DoubleToUInt64Bits(first.Value) == MathLog2OtpNames.AboveOneResult);
                Require(power is FloatTerm second
                    && BitConverter.DoubleToUInt64Bits(second.Value) == MathLog2OtpNames.PowerOfTwoResult);

                return true;
            });
        }
    }
}
