using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamCallConstructionProbe;

namespace Erlang.Tests;

internal static class BeamReturnedSiblingConstructionChecks
{
    internal const string TestName = "compiler/beam/constructs-returned-siblings-from-live-call-roots";
    private const string ExpectedRootedConstruction = "Expected the call result to stay in X0 while the retained arguments construct the returned siblings.";

    internal static Task Compare()
    {
        const string fixture = """
            -module(beam_returned_sibling_construction).
            -export([build/3, bound/4, effected/3, normalize/1]).

            build(Name, Value, Modules) ->
                Result = ?MODULE:normalize(Value),
                {Name, {Name, start, []}, permanent, Result, worker, [Name] ++ Modules}.

            bound(Name, Value, Modules, Result) ->
                Result = ?MODULE:normalize(Value),
                {Name, {Name, start, []}, permanent, Result, worker, [Name] ++ Modules}.

            effected(Name, Value, Modules) ->
                Result = ?MODULE:normalize(Value),
                {Name, {Name, start, erase(effect_cell)}, permanent, Result, worker, [Name] ++ Modules}.

            normalize(Value) ->
                put(effect_cell, Value),
                Value.
            """;

        const string module = "beam_returned_sibling_construction";
        var code = new BeamCallConstructionProbe(CodeGeneration.CompileBeamModule(fixture, module + ".erl"));
        if (!RetainsResultAcrossConstructors() || !ChecksBoundResult() || !RetainsResultAcrossEffect())
        {
            throw new InvalidOperationException(ExpectedRootedConstruction);
        }

        return Task.CompletedTask;

        bool RetainsResultAcrossConstructors()
        {
            object[] body = code.Body("build");
            int call = code.CallIndex(
                body,
                module,
                "normalize",
                1
            );
            object[] operations = call < 0 ? [] : WithoutLines(body.Skip(call + 1));
            if (operations.Length < 4 || Opcode(operations[0]) != TestHeap || Value(Operand(operations[0], 1)) != 1
                || Opcode(operations[1]) != PutList || Opcode(operations[2]) != PutTuple || Opcode(operations[3]) != PutTuple)
            {
                return false;
            }

            object list = operations[1];
            object inner = operations[2];
            object outer = operations[3];
            object name = Operand(list, 0);
            object modules = Operand(list, 1);

            return Kind(name) == "y" && Kind(modules) == "y" && !SameRegister(name, modules)
                && WasSaved(
                    body,
                    call,
                    0,
                    name
                ) && WasSaved(
                    body,
                    call,
                    2,
                    modules
                )
                && IsX(Operand(list, 2), 1) && IsX(Operand(inner, 0), 2)
                && SameRegister(Operand(inner, 2), name) && SameRegister(Operand(outer, 2), name)
                && IsX(Operand(outer, 0), 0) && Value(Operand(outer, 1)) == 6
                && IsX(Operand(outer, 3), 2) && IsX(Operand(outer, 5), 0) && IsX(Operand(outer, 7), 1);
        }

        bool ChecksBoundResult()
        {
            object[] body = code.Body("bound");
            int call = code.CallIndex(
                body,
                module,
                "normalize",
                1
            );
            object[] operations = call < 0 ? [] : WithoutLines(body.Skip(call + 1));
            if (operations.Length == 0 || Opcode(operations[0]) != ExactEqualTest || !IsX(Operand(operations[0], 1), 0))
            {
                return false;
            }

            object tested = Operand(operations[0], 2);
            int failure = Value(Operand(operations[0], 0));
            int target = Array.FindIndex(operations, operation => Opcode(operation) == Label
                && Value(Operand(operation, 0)) == failure);

            return Kind(tested) == "y" && WasSaved(
                body,
                call,
                3,
                tested
            ) && target >= 0
                && target + 1 < operations.Length && Opcode(operations[target + 1]) == BadMatch
                && IsX(Operand(operations[target + 1], 0), 0);
        }

        bool RetainsResultAcrossEffect()
        {
            object[] body = code.Body("effected");
            int normalize = code.CallIndex(
                body,
                module,
                "normalize",
                1
            );
            int erase = code.CallIndex(
                body,
                "erlang",
                "erase",
                1
            );
            if (normalize < 0 || erase <= normalize)
            {
                return false;
            }

            object? spill = body.Skip(normalize + 1).Take(erase - normalize - 1)
                .FirstOrDefault(operation => Opcode(operation) == Move
                    && IsX(Operand(operation, 0), 0) && Kind(Operand(operation, 1)) == "y");
            object? tuple = body.Skip(erase + 1).FirstOrDefault(operation =>
                Opcode(operation) == PutTuple && Value(Operand(operation, 1)) == 6);

            return spill is not null && tuple is not null
                && SameRegister(Operand(spill, 1), Operand(tuple, 5));
        }
    }

    private static bool WasSaved(
        object[] body,
        int call,
        int parameter,
        object saved
    ) => body.Take(call).Any(operation => Opcode(operation) == Move
        && IsX(Operand(operation, 0), parameter) && SameRegister(Operand(operation, 1), saved));
}
