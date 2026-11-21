using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamSizedRecordIolistChecks
{
    internal const string TestName = "compiler/beam/keeps-sized-iolist-results-in-owned-record-slots";
    private const string ShapeError = "The sized record delivery missed the proved list operand.";
    private const string Source = """
        -module(beam_sized_record_iolist).
        -export([render/1]).
        -record(state, {header, seed, kind, endpoint, entries}).

        render(#state{}=State) -> deliver(State).

        deliver(State) ->
            Header = State#state.header,
            Body = body(Header),
            Length = length(State#state.seed ++ Body),
            Payload = ["head",integer_to_list(Length),":",State#state.seed,Body],
            service:deliver(State#state.kind,State#state.endpoint,Payload),
            {proceed,[{done,{sent,200,Length}}|State#state.entries]}.

        body(Headers) -> lists:flatten(Headers).
        """;

    internal static void Run()
    {
        string unknown = Source.Replace("lists:flatten(Headers)", "service:unknown(Headers)", StringComparison.Ordinal);
        if (unknown == Source || !UsesProvedList(Source) || UsesProvedList(unknown))
        {
            throw new InvalidOperationException(ShapeError);
        }
    }

    private static bool UsesProvedList(string source)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, "beam_sized_record_iolist.erl"));
        var instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();

        return instructions.Any(instruction => Opcode(instruction) == Allocate
                && Operands(instruction).Select(operand => (int)(BigInteger)Property(operand, "Value")).SequenceEqual([3, 1]))
            && instructions.Where(instruction => Opcode(instruction) == GcBifOne)
                .Any(instruction => Property(Operands(instruction)[^2], "TypeIndex") is not null
                    && (string)Property(Operands(instruction)[^1], "Kind") == BeamBinaryHostNames.Y);
    }

    private static object[] Operands(object instruction) => ((IEnumerable)Property(instruction, "Operands")).Cast<object>().ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");
}
