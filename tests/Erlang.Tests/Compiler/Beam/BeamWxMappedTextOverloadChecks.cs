using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamWxMappedTextOverloadChecks
{
    internal const string TestName = "compiler/beam/frames-scalar-and-chardata-option-overloads-independently";
    private const string Fixture = "beam_wx_mapped_text_overloads.erl";
    private const string WrongForward = "Chardata forwarding did not branch directly to function_clause.";
    private const string WrongFrames = "The scalar and chardata overloads no longer own one frame slot each.";
    private const string WrongEffects = "The mapper closure or queue command retained unnecessary stack slots.";
    private const string WrongBoundEnvironment = "The wx environment check lost its equality against the bound options.";
    private const string Missing = "A compiled fixture function is missing.";

    private const string Source = """
        -module(beam_wx_mapped_text_overloads).
        -export([make/1,make/2]).

        -define(ENV,
            case erlang:get(wx_env) of
                {wx_env,Environment,_,_} -> Environment;
                _ -> erlang:error({wx,unknown_env})
            end).
        -define(CHAR_DATA(Arg), (is_list(Arg) orelse is_binary(Arg))).

        make(Value) when is_integer(Value) -> make(Value,[]);
        make(Text) when ?CHAR_DATA(Text) -> make(Text,[]).

        make(Value,Options) when is_integer(Value),is_list(Options) ->
            Mapper = fun({quality,_}=Entry) -> Entry;
                        (Bad) -> erlang:error({badoption,Bad}) end,
            Converted = lists:map(Mapper,Options),
            wxe_util:queue_cmd(Value,Converted,?ENV,840),
            wxe_util:rec(840);
        make(Text,Options) when ?CHAR_DATA(Text),is_list(Options) ->
            Bytes = unicode:characters_to_binary(Text),
            Mapper = fun({translated,Input}) ->
                            Value = unicode:characters_to_binary(Input), {translated,Value};
                        ({keep,_}=Entry) -> Entry;
                        (Bad) -> erlang:error({badoption,Bad}) end,
            Converted = lists:map(Mapper,Options),
            wxe_util:queue_cmd(Bytes,Converted,?ENV,841),
            wxe_util:rec(841).
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        int forward = Entry(1);
        int failedForward = Value(Operands(instructions[forward - 2])[0]);
        object[] oneArgument = Body(forward);
        if (oneArgument.Count(instruction => Opcode(instruction) == Jump) != 1
            || oneArgument.SingleOrDefault(instruction => Opcode(instruction) == IsBinary) is not { } binary
            || Value(Operands(binary)[0]) != failedForward)
        {
            throw new InvalidOperationException(WrongForward);
        }

        object[] twoArguments = Body(Entry(2));
        object[] frames = twoArguments.Where(instruction => Opcode(instruction) is Allocate or AllocateHeap).ToArray();
        int scalarGuard = Array.FindIndex(twoArguments, instruction => Opcode(instruction) == IsInteger);
        int firstFrame = Array.FindIndex(twoArguments, instruction => Opcode(instruction) == AllocateHeap);
        if (frames.Length != 2 || frames.Any(instruction => Value(Operands(instruction)[0]) != 1)
            || scalarGuard < 0 || firstFrame <= scalarGuard
            || twoArguments.Any(instruction => Opcode(instruction) == InitYRegisters))
        {
            throw new InvalidOperationException(WrongFrames);
        }

        if (twoArguments.Count(instruction => Opcode(instruction) == MakeFun) != 2
            || twoArguments.Count(instruction => Opcode(instruction) == TrimFrame) != 2
            || Entry(1, "-make/2-fun-0-") >= Entry(1, "-make/2-fun-1-"))
        {
            throw new InvalidOperationException(WrongEffects);
        }

        string boundEnvironment = Source.Replace("Environment", "Options", StringComparison.Ordinal);
        code = Code(CodeGeneration.CompileBeamModule(boundEnvironment, Fixture));
        atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        instructions = Items(code, "Instructions").ToArray();
        if (Body(Entry(2)).Count(instruction => Opcode(instruction) == ExactEqualTest) != 2)
        {
            throw new InvalidOperationException(WrongBoundEnvironment);
        }

        return Task.CompletedTask;

        int Entry(int arity, string name = "make")
        {
            int found = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Operands(instruction)[1])] == name && Value(Operands(instruction)[2]) == arity
            );

            return found >= 2 ? found : throw new InvalidOperationException(Missing);
        }

        object[] Body(int start) =>
            instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
    }

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
