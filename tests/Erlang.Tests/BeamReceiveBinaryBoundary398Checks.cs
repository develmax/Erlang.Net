using static Erlang.Tests.BeamReceiveBinaryBoundary398Names;
using System.Collections;
using Erlang;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamBinaryHostBytes;

namespace Erlang.Tests;

internal static class BeamReceiveBinaryBoundary398Checks
{
    internal static Task StandaloneLegacyOutsideMatcherRefused()
    {
        // Only binary role is bs_start_match3 before the ordinary receive.
        // Without the function composition guard the context is overwritten
        // by the candidate message and all terminal returns are ordinary.
        byte[] image = BeamBinaryHostBytes.Image(
            Join(
                Instruction(
                    166,
                    F(7),
                    X(0),
                    U(1),
                    X(0)
                ),
                Instruction(1, U(3)),
                Instruction(23, F(6), X(0)),
                [21, 19],
                Instruction(1, U(6)),
                [22],
                Instruction(64, A(8), X(0)),
                [19],
                Instruction(1, U(7)),
                Instruction(64, A(8), X(0)),
                [19]
            ),
            arity: 1,
            labels: 8
        );
        object code = Code(image);
        object entry = ((IEnumerable)Property(code, Exports)).Cast<object>().Single();
        object[] instructions = ((IEnumerable)Property(code, Instructions)).Cast<object>().ToArray();
        Require(instructions.Count(instruction => (string)Property(Property(instruction, Definition), Name) == LegacyStartOpcode) == 1);
        Require(!instructions.Any(instruction => (string)Property(Property(instruction, Definition), Name) == Rolebsmatch));
        int scan = Array.FindIndex(instructions, instruction => (string)Property(Property(instruction, Definition), Name) == Rolelooprec);
        _ = Call(
            Type(RoleBeamReceivePlanner),
            RoleBuild,
            code,
            entry,
            scan
        );
        Refuses<NotSupportedException>(() => Call(
            Type(RoleBeamRegisterValidation),
            RoleValidate,
            code,
            entry
        ));

        return Task.CompletedTask;
    }
}
