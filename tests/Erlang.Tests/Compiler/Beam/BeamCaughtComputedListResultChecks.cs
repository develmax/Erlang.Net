using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCaughtComputedListResultChecks
{
    internal const string TestName = "compiler/beam/preserves-computed-list-roots-across-catch";
    internal const string Fixture = "beam_caught_list_result.erl";
    private const string IncorrectFrameLayout = "The protected list continuation did not reuse expired outgoing roots.";

    internal const string Source = """
        -module(beam_caught_list_result).
        -export([run/3, run/4, observe/0]).

        run(false, _Format, _Arguments) -> skipped;
        run(Entry, Format, Arguments) ->
            First = first(Entry),
            Second = second(get(layout_context)),
            Tail = append_item(Arguments, []),
            catch consume("prefix" ++ Format ++ "suffix",
                          [head(), Second, First | Tail]).

        run(false, _Extra, _Format, _Arguments) -> skipped;
        run(Entry, Extra, Format, Arguments) ->
            First = first(Entry),
            Second = second(get(layout_context)),
            Tail = append_item(Arguments, []),
            catch consume("prefix" ++ Format ++ "suffix",
                          [head(), Second, Extra, First | Tail]).

        first(Value) -> {first, Value}.
        second(Value) -> {second, Value}.
        append_item(Arguments, End) -> lists:reverse(Arguments, End).
        head() -> get(head_context).
        consume(Format, Payload) -> {Format, Payload}.

        observe() ->
            put(layout_context, fixture),
            put(head_context, head_result),
            [run(false, invalid, invalid),
             run(normal, "", [one, two]),
             run(normal, extra, "middle", []),
             element(1, run(normal, invalid_format, [])),
             try run(normal, "", invalid_list)
             catch error:Reason -> Reason end].
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] instructions = Items(code, "Instructions").ToArray();
        int[] frameWidths = instructions
            .Where(instruction => Opcode(instruction) == Allocate)
            .Select(instruction => Value(Items(instruction, "Operands").First()))
            .ToArray();
        int[][] heapAnnouncements = instructions
            .Where(instruction => Opcode(instruction) == TestHeap)
            .Select(instruction => Items(instruction, "Operands").Select(Value).ToArray())
            .ToArray();
        if (!frameWidths.Contains(5) || !frameWidths.Contains(6)
            || !heapAnnouncements.Any(operands => operands is [6, 1])
            || !heapAnnouncements.Any(operands => operands is [8, 1]))
        {
            throw new InvalidOperationException(IncorrectFrameLayout);
        }

        return Task.CompletedTask;
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
