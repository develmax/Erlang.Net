using Erlang;
using static Erlang.Tests.BeamOpcodeEdgeBytes;
using static Erlang.Tests.ReceiveControl483Names;

namespace Erlang.Tests;

internal static class ReceiveControl483Checks
{
    internal static Task Subject() => Refuse(
        Binary,
        X(9),
        I(0),
        F(5)
    );

    internal static Task Arity() => Refuse(
        Function,
        X(0),
        X(9),
        F(5)
    );

    internal static Task Kind() => Refuse(
        Function,
        X(0),
        U(1),
        F(5)
    );

    internal static Task Branch() => Refuse(
        Function,
        X(0),
        I(1),
        F(2)
    );

    private static async Task Refuse(
        byte opcode,
        byte subject,
        byte arity,
        byte failure
    )
    {
        byte[] Build(
            byte operation,
            byte value,
            byte count,
            byte target
        )
        {
            byte[] guard = operation == Function ? [operation, target, value, count] : [operation, target, value];

            return Image(
                [.. Start(
                    1,
                    2,
                    2,
                    1
                ), Allocate, U(0), U(1),
            Label, U(3), Scan, F(6), X(0), .. guard, Remove, Deallocate, U(0), Return,
            Label, U(5), Reject, F(3), Label, U(6), Wait, F(3), I(0), TimeoutOpcode,
            Move, A(7), X(0), Deallocate, U(0), Return],
                labels: 7,
                arity: 1
            );
        }
        byte[] image = Build(
            opcode,
            subject,
            arity,
            failure
        );
        bool refused = false;
        await using var runtime = new ProcessRuntime();
        await runtime.InitializeModulesAsync(registry =>
        {
            registry.PrepareBeam(Build(
                opcode,
                X(0),
                I(1),
                F(5)
            ));
            try
            {
                registry.PrepareBeam(image);
            }
            catch (Exception error) when ((error is InvalidDataException or NotSupportedException)
                && error.Message == (subject == X(9) || arity == X(9) ? FrameDiagnostic
                    : arity == U(1) ? RoleDiagnostic : MatcherDiagnostic))
            {
                refused = true;
            }
        });
        ReceiveOriginal483Checks.Require(refused);
    }
}
