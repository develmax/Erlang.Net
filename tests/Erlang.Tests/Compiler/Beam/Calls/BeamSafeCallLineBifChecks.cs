using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;
using static Erlang.Tests.BeamBinaryHostNames;

namespace Erlang.Tests;

internal static class BeamSafeCallLineBifChecks
{
    internal const string TestName = "compiler/beam/keeps-safe-call-line-analysis-through-safe-bif0";
    private const string Failure = "A recognized safe BIF may carry a fresh line to the next throwing operation.";

    internal static Task Run()
    {
        var compiler = new BeamModuleCompiler("beam_safe_call_line_bif");
        var safeImports = new HashSet<int> { 0, 1 };
        object external = CreateInstruction(ExternalCall, Op(Unsigned, 0), Op(Unsigned, 0));
        object nofail = CreateInstruction(BifZero, Op(Unsigned, 1), Op(X, 0));
        object unknown = CreateInstruction(BifZero, Op(Unsigned, 2), Op(X, 0));
        object line = CreateInstruction(Line, Op(Unsigned, 1));

        if (!CanDrop(external, nofail, line)
            || CanDrop(external, unknown, line)
            || CanDrop(
                external,
                nofail,
                unknown,
                line
            ))
        {
            throw new InvalidOperationException(Failure);
        }

        return Task.CompletedTask;

        bool CanDrop(params object[] instructions)
        {
            Array body = Array.CreateInstance(BeamBinaryHostAccess.Type(Instruction), instructions.Length);
            for (int index = 0; index < instructions.Length; index++)
            {
                body.SetValue(instructions[index], index);
            }

            return (bool)Call(
                compiler,
                "CanDropSafeCallLine",
                body,
                0,
                safeImports
            );
        }
    }

    private static object CreateInstruction(byte opcode, params object[] operands)
    {
        object definition = New(
            Definition,
            opcode,
            string.Empty,
            operands.Length,
            false
        );

        return New(
            Instruction,
            0,
            definition,
            Ops(operands)
        );
    }
}
