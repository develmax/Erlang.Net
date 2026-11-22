using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamFrameValueLivenessChecks
{
    internal const string TestName = "compiler/beam/saves-source-values-used-after-register-clobbers";
    private const string WrongSavedValues = "The frame must save exactly the defining values used after an X-register clobber.";

    internal static Task Run()
    {
        BeamFrameValueLiveness.Operation[] operations =
        [
            new([0], 4, ClobbersXRegisters: true),
            new([4, 1], 5, ClobbersXRegisters: true),
            new([4, 5, 2], 6, ClobbersXRegisters: false),
            new([6], null, ClobbersXRegisters: false)
        ];
        var saved = BeamFrameValueLiveness.FindSavedValues([0, 1, 2, 3], operations);
        var noSaved = BeamFrameValueLiveness.FindSavedValues(
            [0],
            [
                new([0], 1, ClobbersXRegisters: true),
                new([1], null, ClobbersXRegisters: false)
            ]
        );
        if (!saved.SetEquals([1, 2, 4]) || noSaved.Count != 0)
        {
            throw new InvalidOperationException(WrongSavedValues);
        }

        return Task.CompletedTask;
    }
}
