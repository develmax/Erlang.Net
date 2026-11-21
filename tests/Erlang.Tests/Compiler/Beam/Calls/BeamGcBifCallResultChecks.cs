using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamCallConstructionProbe;

namespace Erlang.Tests;

internal static class BeamGcBifCallResultChecks
{
    internal const string TestName = "compiler/beam/gc-bif-keeps-call-result-root";

    internal static Task Run()
    {
        const string source = """
            -module(gc_call_result).
            -export([remaining/1, observe/1, caught/1, increment/1]).
            remaining(Deadline) ->
                Now = probe:seconds(probe:clock()),
                Diff = Deadline - Now,
                if Diff > 0 -> Diff; true -> 0 end.
            observe(Deadline) ->
                Now = probe:seconds(probe:clock()),
                {Deadline - Now, Now}.
            caught(Deadline) ->
                try remaining(Deadline)
                catch error:badarith -> recovered
                end.
            increment(N) ->
                probe:source(N) + 1.
            """;
        var code = new BeamCallConstructionProbe(CodeGeneration.CompileBeamModule(source, "gc_call_result.erl"));
        object[] remaining = code.Body("remaining");
        object[] observe = code.Body("observe");
        object[] increment = code.Body("increment");
        if (!LeavesSecondsRooted(remaining) || !KeepsSecondsForTuple(observe) || !LeavesIncrementRooted(increment))
        {
            throw new InvalidOperationException(BeamGcBifCallResultDiagnostics.ResultRootMismatch);
        }

        return Task.CompletedTask;

        bool LeavesSecondsRooted(object[] body)
        {
            int call = code.CallIndex(
                body,
                "probe",
                "seconds",
                1
            );
            object[] operations = call < 0 ? [] : WithoutLines(body.Skip(call + 1));

            return operations.Length > 0 && IsDifference(operations[0]) && Value(Operand(operations[0], 1)) == 1
                && SavedDeadline(body, call, Operand(operations[0], 3))
                && IsX(Operand(operations[0], 4), 0) && IsX(Operand(operations[0], 5), 0);
        }

        bool KeepsSecondsForTuple(object[] body)
        {
            int call = code.CallIndex(
                body,
                "probe",
                "seconds",
                1
            );
            object[] operations = call < 0 ? [] : WithoutLines(body.Skip(call + 1));
            int first = operations.Length > 0 && Opcode(operations[0]) == Move ? 1 : 0;
            if (operations.Length < first + 3 || !IsDifference(operations[first])
                || Opcode(operations[first + 1]) != TestHeap || Opcode(operations[first + 2]) != PutTuple)
            {
                return false;
            }

            object subtraction = operations[first];
            object heap = operations[first + 1];
            object tuple = operations[first + 2];
            object seconds = Operand(subtraction, 4);
            object result = Operand(subtraction, 5);
            if (!SavedDeadline(body, call, Operand(subtraction, 3))
                || !IsX(Operand(tuple, 0), 0) || Value(Operand(tuple, 1)) != 2
                || !SameRegister(result, Operand(tuple, 2)) || !SameRegister(seconds, Operand(tuple, 3)))
            {
                return false;
            }

            if (first == 0)
            {
                return Value(Operand(subtraction, 1)) == 1 && IsX(seconds, 0)
                    && IsX(result, 1) && Value(Operand(heap, 1)) == 2;
            }

            object spill = operations[0];

            return IsX(Operand(spill, 0), 0) && Kind(seconds) == "y"
                && SameRegister(Operand(spill, 1), seconds) && IsX(result, 0)
                && Value(Operand(subtraction, 1)) == 0 && Value(Operand(heap, 1)) == 1;
        }

        bool LeavesIncrementRooted(object[] body)
        {
            int call = code.CallIndex(
                body,
                "probe",
                "source",
                1
            );
            object[] operations = call < 0 ? [] : WithoutLines(body.Skip(call + 1));

            return operations.Length > 0 && Opcode(operations[0]) == GcBifTwo
                && code.UsesImport(
                    operations[0],
                    2,
                    "erlang",
                    "+",
                    2
                )
                && Value(Operand(operations[0], 1)) == 1 && IsX(Operand(operations[0], 3), 0)
                && IsX(Operand(operations[0], 5), 0);
        }

        bool IsDifference(object operation) =>
            Opcode(operation) == GcBifTwo && code.UsesImport(
                operation,
                2,
                "erlang",
                "-",
                2
            );
    }

    private static bool SavedDeadline(
        object[] body,
        int call,
        object register
    ) => Kind(register) == "y" && body.Take(call).Any(operation =>
        Opcode(operation) == Move && IsX(Operand(operation, 0), 0) && SameRegister(Operand(operation, 1), register));
}
