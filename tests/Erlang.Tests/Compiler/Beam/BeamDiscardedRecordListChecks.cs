using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamDiscardedRecordListChecks
{
    internal const string TestName = "compiler/beam/isolates-a-discarded-record-list-check-before-queueing";
    private const string Fixture = "beam_discarded_record_list_probe.erl";
    private const string MissingHelper = "The effect-only checked list has no independent recursive helper.";
    private const string WrongScope = "The source record, list tail, or original command arguments were not kept live.";

    private const string Source = """
        -module(beam_discarded_record_list_probe).
        -export([open/2]).
        -record(checked_entry,{key,type,details}).
        -record(wx_env,{ref,server,extra}).

        open(Size, Entries) when is_integer(Size), is_list(Entries) ->
            _ = [Type =:= good_type orelse Type:check(good_type) || #checked_entry{type=Type} <- Entries],
            wxe_util:queue_cmd(Size, Entries,
                case erlang:get(wx_env) of
                    #wx_env{ref=Reference} -> Reference;
                    _ -> erlang:error({wx,unknown_env})
                end,
                71),
            wxe_util:rec(71).
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        object[] helper = ReadFunction("-open/2-lc$^0/1-0-", 1);
        object[] caller = ReadFunction("open", 2);
        if (helper.Length == 0)
        {
            throw new InvalidOperationException(MissingHelper);
        }

        int check = Array.FindIndex(helper, instruction => Opcode(instruction) == IsTaggedTuple);
        int alloc = Array.FindIndex(helper, instruction => Opcode(instruction) == Allocate);
        int project = Array.FindIndex(helper, instruction => Opcode(instruction) == GetTupleElement);
        if (check < 0 || alloc <= check || project <= alloc || Operands(helper[alloc]) is not [var slots, var live]
            || Value(slots) != 1 || Value(live) != 2
            || helper.Count(instruction => Opcode(instruction) == DynamicApply) != 1
            || !helper.Any(instruction => Opcode(instruction) == TailLocalCallWithFrame)
            || !helper.Any(instruction => Opcode(instruction) == TailLocalCall)
            || !caller.Any(instruction => Opcode(instruction) == LocalCall)
            || !caller.Any(instruction => Opcode(instruction) == TrimFrame
                && Operands(instruction) is [var removed, var kept] && Value(removed) == 2 && Value(kept) == 0))
        {
            throw new InvalidOperationException(WrongScope);
        }

        return Task.CompletedTask;

        object[] ReadFunction(string name, int arity)
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && atoms[Value(Operands(instruction)[1])] == name
                    && Value(Operands(instruction)[2]) == arity
            );

            return start < 0
                ? []
                : instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
