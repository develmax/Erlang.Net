using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamCallConstructionProbe;

namespace Erlang.Tests;

internal static class BeamDynamicConstructionArgumentChecks
{
    internal const string TestName = "compiler/beam/writes-pure-dynamic-call-constructions-to-outgoing-registers";
    private const string IncorrectArguments = "A constructed call operand lost its outgoing register or selector effect.";

    internal static Task Run()
    {
        const string source = """
            -module(beam_dynamic_construction_argument_probe).
            -export([prepared/3, computed_selector/3]).
            prepared(Module, Database, Key) ->
                Result = Module:read(Database, {alias, Key}),
                {done, Result}.

            computed_selector(Source, Database, Key) ->
                Result = (probe:choose(Source)):read(Database, {alias, Key}),
                {done, Result}.
            """;
        var code = new BeamCallConstructionProbe(CodeGeneration.CompileBeamModule(source, "beam_dynamic_construction_argument_probe.erl"));
        object[] prepared = code.Body("prepared");
        int transfer = Array.FindIndex(prepared, instruction => Opcode(instruction) == DynamicApply);
        int construction = Array.FindIndex(prepared, instruction => Opcode(instruction) == PutTuple);
        if (construction < 0 || transfer <= construction
            || !IsX(Operand(prepared[construction], 0), 1)
            || prepared.Skip(construction + 1).Take(transfer - construction - 1)
                .Any(instruction => Opcode(instruction) == Move && Kind(Operand(instruction, 1)) == "y"))
        {
            throw new InvalidOperationException(IncorrectArguments);
        }

        object[] computed = code.Body("computed_selector");
        int selector = code.CallIndex(
            computed,
            "probe",
            "choose",
            1
        );
        int tuple = Array.FindIndex(computed, instruction => Opcode(instruction) == PutTuple);
        int apply = Array.FindIndex(computed, instruction => Opcode(instruction) == DynamicApply);
        if (selector < 0 || tuple <= selector || apply <= tuple)
        {
            throw new InvalidOperationException(IncorrectArguments);
        }

        return Task.CompletedTask;
    }
}
