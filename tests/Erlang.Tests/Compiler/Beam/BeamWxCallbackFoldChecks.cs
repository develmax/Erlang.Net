using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamWxCallbackFoldChecks
{
    internal const string TestName = "compiler/beam/keeps-callback-fold-state-in-two-constructor-slots";
    private const string Fixture = "beam_wx_fold_callback_options.erl";
    private const string WrongCommandFrame = "Callback fold emission lost the two-slot command schedule.";
    private const string WrongMapper = "Callback fold emission lost the shared dispatch or accumulator frame.";
    private const string WrongBoundEnvironment = "The wx environment check lost its equality against an existing binding.";
    private const string Missing = "A compiled fixture function is missing.";

    private const string Source = """
        -module(beam_wx_fold_callback_options).
        -export([open/3]).
        -define(ENV,
            case erlang:get(wx_env) of
                {wx_env,Environment,_,_} -> Environment;
                _ -> erlang:error({wx,unknown_env})
            end).

        open(Title, MainCallback, Options) when is_list(Title), is_function(MainCallback), is_list(Options) ->
            MainId = wxe_util:get_cbId(MainCallback),
            Mapper = fun({write_line, Work},Acc) when is_function(Work) ->
                            [{write_line,wxe_util:get_cbId(Work)}|Acc];
                        ({before_begin, Handler},Acc) when is_function(Handler) ->
                            [{before_begin,wxe_util:get_cbId(Handler)}|Acc];
                        ({finish_window, Done},Acc) when is_function(Done) ->
                            [{finish_window,wxe_util:get_cbId(Done)}|Acc] end,
            Converted = lists:foldl(Mapper,[],Options),
            TitleBytes = unicode:characters_to_binary(Title),
            Command = 447,
            wxe_util:queue_cmd(TitleBytes,MainId,Converted,?ENV,Command),
            wxe_util:rec(Command).
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        var atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        var instructions = Items(code, "Instructions").ToArray();
        int commandEntry = Entry("open");
        object[] command = Body(commandEntry);
        object[] commandFrames = command.Where(instruction => Opcode(instruction) is Allocate or AllocateHeap).ToArray();
        if (commandFrames.Length != 1 || Opcode(commandFrames[0]) != Allocate
            || Value(Operands(commandFrames[0])[0]) != 2
            || command.Count(instruction => Opcode(instruction) == MakeFun) != 1
            || command.Count(instruction => Opcode(instruction) == Swap) != 1
            || command.Count(instruction => Opcode(instruction) == TrimFrame) != 1
            || command.Any(instruction => Opcode(instruction) == InitYRegisters))
        {
            throw new InvalidOperationException(WrongCommandFrame);
        }

        int mapperEntry = Entry("-open/3-fun-0-");
        object[] mapper = Body(mapperEntry);
        int functionClause = Value(Operands(instructions[mapperEntry - 2])[0]);
        int tuple = Array.FindIndex(mapper, instruction => Opcode(instruction) == IsTuple);
        int select = Array.FindIndex(mapper, instruction => Opcode(instruction) == SelectValue);
        object[] guards = mapper.Where(instruction => Opcode(instruction) == IsFunction).ToArray();
        object[] frames = mapper.Where(instruction => Opcode(instruction) == Allocate).ToArray();
        if (tuple < 0 || select <= tuple || guards.Length != 3 || frames.Length != guards.Length
            || frames.Any(frame => Value(Operands(frame)[0]) != 1 || Value(Operands(frame)[1]) != 4)
            || guards.Any(guard => Value(Operands(guard)[0]) != functionClause)
            || mapper.Count(instruction => Opcode(instruction) == PutList) != frames.Length)
        {
            throw new InvalidOperationException(WrongMapper);
        }

        string boundEnvironment = Source.Replace("Environment", "Title", StringComparison.Ordinal);
        code = Code(CodeGeneration.CompileBeamModule(boundEnvironment, Fixture));
        atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        instructions = Items(code, "Instructions").ToArray();
        if (!Body(Entry("open")).Any(instruction => Opcode(instruction) == ExactEqualTest))
        {
            throw new InvalidOperationException(WrongBoundEnvironment);
        }

        return Task.CompletedTask;

        int Entry(string name)
        {
            int entry = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Operands(instruction)[1])] == name
            );

            return entry >= 2 ? entry : throw new InvalidOperationException(Missing);
        }

        object[] Body(int entry) =>
            instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
    }

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
