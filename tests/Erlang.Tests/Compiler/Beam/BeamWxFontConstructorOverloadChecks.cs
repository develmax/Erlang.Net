using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamWxFontConstructorOverloadChecks
{
    internal const string TestName = "compiler/beam/dispatches-static-wx-scalar-tuple-options";
    private const string Fixture = "beam_wx_font_commands.erl";
    private const string WrongText = "The text branch did not enter its empty conversion frame after the type check.";
    private const string WrongForward = "The disjoint integer and tuple forwarding paths lost their common guard failures.";
    private const string WrongOptions = "Expected separate four-slot option mapper frames for integer and tuple sizes.";
    private const string WrongBinding = "The specialized path replaced a source-level exact comparison.";
    private const string Missing = "A compiled fixture function is missing.";

    private const string Source = """
        -module(beam_wx_font_commands).
        -export([construct/1,construct/4,construct/5]).
        -record(wx_ref,{ref,type,state=[]}).
        -define(ENV,
            case erlang:get(wx_env) of
                {wx_env,Environment,_,_} -> Environment;
                _ -> erlang:error({wx,unknown_env})
            end).
        -define(CLASS(Type, Class), ((Type) =:= Class) orelse (Type):parent_class(Class)).

        construct(Text) when is_list(Text) orelse is_binary(Text) ->
            Converted = unicode:characters_to_binary(Text),
            wxe_util:queue_cmd(Converted,?ENV,1850),
            wxe_util:rec(1850);
        construct(#wx_ref{type=Type}=Font) ->
            ?CLASS(Type,wxFont),
            wxe_util:queue_cmd(Font,?ENV,1851),
            wxe_util:rec(1851).

        construct(Point,Family,Style,Weight)
          when is_integer(Point),is_integer(Family),is_integer(Style),is_integer(Weight) ->
            construct(Point,Family,Style,Weight,[]);
        construct({W,H}=Pixels,Family,Style,Weight)
          when is_integer(W),is_integer(H),is_integer(Family),is_integer(Style),is_integer(Weight) ->
            construct(Pixels,Family,Style,Weight,[]).

        construct(Point,Family,Style,Weight,Options)
          when is_integer(Point),is_integer(Family),is_integer(Style),is_integer(Weight),is_list(Options) ->
            Mapper = fun({underlined,_}=Entry) -> Entry;
                        ({face,Face}) -> Converted = unicode:characters_to_binary(Face), {face,Converted};
                        (Bad) -> erlang:error({badoption,Bad}) end,
            Effective = lists:map(Mapper,Options),
            wxe_util:queue_cmd(Point,Family,Style,Weight,Effective,?ENV,1852),
            wxe_util:rec(1852);
        construct({W,H}=Pixels,Family,Style,Weight,Options)
          when is_integer(W),is_integer(H),is_integer(Family),is_integer(Style),is_integer(Weight),is_list(Options) ->
            Mapper = fun({underline,_}=Entry) -> Entry;
                        ({faceName,Face}) -> Converted = unicode:characters_to_binary(Face), {faceName,Converted};
                        (Bad) -> erlang:error({badoption,Bad}) end,
            Effective = lists:map(Mapper,Options),
            wxe_util:queue_cmd(Pixels,Family,Style,Weight,Effective,?ENV,1853),
            wxe_util:rec(1853).
        """;

    internal static Task Run()
    {
        var get = ReadFunctions(Source);
        var (one, _) = get("construct", 1);
        int list = Array.FindIndex(one, instruction => Opcode(instruction) == IsList);
        int frame = Array.FindIndex(one, instruction => Opcode(instruction) == Allocate);
        if (list < 0 || frame <= list || Value(Operands(one[frame])[0]) != 0)
        {
            throw new InvalidOperationException(WrongText);
        }

        var (four, failure) = get("construct", 4);
        object[] scalarGuards = four.TakeWhile(instruction => Opcode(instruction) != TailLocalCall)
            .Where(instruction => Opcode(instruction) == IsInteger).ToArray();
        if (scalarGuards.Length != 4 || Value(Operands(scalarGuards[0])[0]) == failure
            || scalarGuards.Skip(1).Any(instruction => Value(Operands(instruction)[0]) != failure))
        {
            throw new InvalidOperationException(WrongForward);
        }

        var (five, _) = get("construct", 5);
        if (five.Count(instruction => Opcode(instruction) == MakeFun) != 2
            || five.Count(instruction => Opcode(instruction) == AllocateHeap && Value(Operands(instruction)[0]) == 4) != 2)
        {
            throw new InvalidOperationException(WrongOptions);
        }

        string repeated = Source.Replace("{W,H}=Pixels", "{W,W}=Pixels", StringComparison.Ordinal)
            .Replace("is_integer(W),is_integer(H)", "is_integer(W)", StringComparison.Ordinal);
        if (!ReadFunctions(repeated)("construct", 5).Body
            .Where(instruction => Opcode(instruction) == ExactEqualTest)
            .Select(Operands)
            .Any(operands => operands.Skip(1).All(value => (string)Property(value, "Kind") is "x" or "y")))
        {
            throw new InvalidOperationException(WrongBinding);
        }

        string bound = Source.Replace(
            "Effective,?ENV,1853",
            "Effective,case erlang:get(wx_env) of {wx_env,W,_,_} -> W; _ -> erlang:error({wx,unknown_env}) end,1853",
            StringComparison.Ordinal
        );
        if (ReadFunctions(bound)("construct", 5).Body.Count(instruction => Opcode(instruction) == AllocateHeap) == 2)
        {
            throw new InvalidOperationException(WrongBinding);
        }

        string boundText = Source.Replace(
            "Converted,?ENV,1850",
            "Converted,case erlang:get(wx_env) of {wx_env,Converted,_,_} -> Converted; _ -> erlang:error({wx,unknown_env}) end,1850",
            StringComparison.Ordinal
        );
        if (!ReadFunctions(boundText)("construct", 1).Body
            .Where(instruction => Opcode(instruction) == ExactEqualTest)
            .Select(Operands)
            .Any(operands => operands.Skip(1).All(value => (string)Property(value, "Kind") is "x" or "y")))
        {
            throw new InvalidOperationException(WrongBinding);
        }

        return Task.CompletedTask;
    }

    private static Func<string, int, (object[] Body, int Failure)> ReadFunctions(string source)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, Fixture));
        var instructions = Items(code, "Instructions").ToArray();
        var atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();

        return (name, arity) =>
        {
            int entry = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Operands(instruction)[1])] == name && Value(Operands(instruction)[2]) == arity
            );
            if (entry < 2)
            {
                throw new InvalidOperationException(Missing);
            }

            return (instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray(),
                Value(Operands(instructions[entry - 2])[0]));
        };
    }

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
