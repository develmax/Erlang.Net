using System.Collections;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamOpcodeEdgeBytes;

namespace Erlang.Tests;

internal static class BeamClosureRegistrationOrderChecks
{
    internal const string TestName = "compiler/beam/registers-lambdas-in-first-use-order";
    private const string WrongOrdinals = "Closure renumbering must follow first use and keep capture counts and shared uses.";

    private const string OwnedSource = """
        -module(beam_owned_list_callback).
        -export([run/2]).
        run(X, Y) ->
            Zed = capture_sink:make(X),
            Alpha = capture_sink:make(Y),
            capture_sink:touch(Alpha),
            F = fun({A, B}) ->
                capture_sink:use(Alpha, A, Zed),
                capture_sink:save(Alpha, A, B)
            end,
            [F(Item) || Item <- [{1,2},{3,4}]],
            capture_sink:done(Alpha).
        """;
    private const string WrongCallback = "The known callback did not use its existing lambda registration.";

    internal static Task Run()
    {
        byte[] first = [MakeFun, U(1), X(0), ExtendedListOperand, U(1), X(0)];
        byte[] second = [MakeFun, U(0), X(0), ExtendedListOperand, U(0)];
        byte[] direct = [CallFunTyped, U(0), U(1), ExtendedTypedRegisterOperand, X(1), U(0)];
        byte[] invalidDirect = [CallFunTyped, U(2), U(1), ExtendedTypedRegisterOperand, X(1), U(0)];
        if (Plan([.. first, .. second, .. first], [0, 1]) is not [1, 0]
            || Plan([.. direct, .. first, .. second], [0, 1]) is not [0, 1]
            || Plan([.. invalidDirect, .. first, .. second], [0, 1]) is not null
            || Plan([.. second, .. first], [0, 1]) is not [0, 1]
            || Plan(first, [0, 1]) is not null
            || Plan([.. first, .. second], [0, 0]) is not null)
        {
            throw new InvalidOperationException(WrongOrdinals);
        }

        object code = Code(CodeGeneration.CompileBeamModule(OwnedSource, "beam_owned_list_callback.erl"));
        var instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>();
        if (!instructions.Any(IsOwnedCallback))
        {
            throw new InvalidOperationException(WrongCallback);
        }

        return Task.CompletedTask;
    }

    private static bool IsOwnedCallback(object instruction)
    {
        byte opcode = (byte)Property(Property(instruction, "Definition"), "Opcode");
        var operands = ((IEnumerable)Property(instruction, "Operands")).Cast<object>();

        return opcode == CallFunTyped && (string)Property(operands.First(), "Kind") == BeamOperandParameters.Unsigned;
    }

    private static int[]? Plan(byte[] uses, int[] captures)
    {
        byte[] bytes =
        [
            Label, U(1), FunctionInfo, A(1), A(2), U(1), Label, U(2),
            .. uses, Return, EndCode
        ];
        var image = new ReadOnlyMemory<byte>(bytes);
        object header = New(
            "BeamCodeHeader",
            0,
            MinimumHighestOpcode,
            3,
            1,
            image
        );
        object body = Call(
            Type("BeamInstructionReader"),
            "Read",
            header,
            3,
            0,
            null,
            null
        );

        return (int[]?)Call(
            typeof(BeamModuleCompiler),
            "ReadClosureRegistrations",
            body,
            captures
        );
    }
}
