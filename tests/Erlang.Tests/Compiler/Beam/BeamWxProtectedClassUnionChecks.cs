using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamWxProtectedClassUnionChecks
{
    internal const string TestName = "compiler/beam/keeps-protected-wx-class-selection-across-index-and-map-arms";
    private const string Fixture = "beam_wx_protected_class_union.erl";
    private const string WrongFrame = "The protected wx class selection lost its shared catch token or argument slots.";
    private const string WrongBinding = "The protected class command lost a source-level binding comparison.";
    private const string Missing = "A compiled fixture function is missing.";

    private const string Source = """
        -module(beam_wx_protected_class_union).
        -export([indexed/3,mapped/3]).
        -record(wx_ref,{ref,type,state=[]}).
        -define(ENV,
            case erlang:get(wx_env) of
                {wx_env,Environment,_,_} -> Environment;
                _ -> erlang:error({wx,unknown_env})
            end).
        -define(CLASS(Type, Class), ((Type) =:= Class) orelse (Type):parent_class(Class)).
        -define(CHECK(Type, Class), try ?CLASS(Type, Class) catch _:_ -> false end).

        indexed(#wx_ref{type=ReceiverType}=Receiver, Index, #wx_ref{type=ItemType}=Item)
          when is_integer(Index) ->
            ?CLASS(ReceiverType, fixture_receiver),
            IsFirst = ?CHECK(ItemType, fixture_first),
            IsSecond = ?CHECK(ItemType, fixture_second),
            Selected = if
                IsFirst -> fixture_first;
                IsSecond -> fixture_second;
                true -> error({badarg, ItemType})
            end,
            wxe_util:queue_cmd(Receiver,Index,wx:typeCast(Item,Selected),?ENV,1620),
            wxe_util:rec(1620).

        mapped(#wx_ref{type=ReceiverType}=Receiver, #wx_ref{type=ItemType}=Item, Options)
          when is_list(Options) ->
            ?CLASS(ReceiverType, fixture_receiver),
            IsFirst = ?CHECK(ItemType, fixture_first),
            IsSecond = ?CHECK(ItemType, fixture_second),
            Selected = if
                IsFirst -> fixture_first;
                IsSecond -> fixture_second;
                true -> error({badarg, ItemType})
            end,
            Mapper = fun({mode,_}=Entry) -> Entry;
                         (Bad) -> erlang:error({badoption,Bad})
                     end,
            Effective = lists:map(Mapper, Options),
            wxe_util:queue_cmd(Receiver,wx:typeCast(Item,Selected),Effective,?ENV,1621),
            wxe_util:rec(1621).
        """;

    internal static Task Run()
    {
        var bodies = ReadFunctions(Source);
        if (!HasProtectedFrame(bodies("indexed"), mapped: false) || !HasProtectedFrame(bodies("mapped"), mapped: true))
        {
            throw new InvalidOperationException(WrongFrame);
        }

        var boundEnvironment = ReadFunctions(Source.Replace("Environment", "Selected", StringComparison.Ordinal));
        var boundType = ReadFunctions(Source.Replace("ItemType", "ReceiverType", StringComparison.Ordinal));
        foreach (string name in new[] { "indexed", "mapped" })
        {
            object[] environment = boundEnvironment(name);
            object[] type = boundType(name);
            if (!environment.Any(instruction => Opcode(instruction) == ExactEqualTest)
                || !type.TakeWhile(instruction => Opcode(instruction) != TryEnter)
                    .Any(instruction => Opcode(instruction) == ExactEqualTest))
            {
                throw new InvalidOperationException(WrongBinding);
            }
        }

        return Task.CompletedTask;
    }

    private static bool HasProtectedFrame(object[] body, bool mapped)
    {
        object[] frames = body.Where(instruction => Opcode(instruction) == Allocate).ToArray();
        object[] catches = body.Where(instruction => Opcode(instruction) == TryEnter).ToArray();
        int[] external = Enumerable.Range(0, body.Length).Where(index => Opcode(body[index]) == ExternalCall).ToArray();
        int lastTry = Array.FindLastIndex(body, instruction => Opcode(instruction) == TryCase);
        int mapper = Array.FindIndex(body, instruction => Opcode(instruction) == MakeFun);

        return frames is [var frame] && Value(Operands(frame)[0]) == 6
            && catches.Length == 2 && catches.All(instruction => Value(Operands(instruction)[0]) == 5)
            && external.Length == (mapped ? 3 : 2) && external[0] > lastTry
            && (mapped ? mapper > lastTry && mapper < external[0] : mapper < 0);
    }

    private static Func<string, object[]> ReadFunctions(string source)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, Fixture));
        var instructions = Items(code, "Instructions").ToArray();
        var atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();

        return name =>
        {
            int entry = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo && atoms[Value(Operands(instruction)[1])] == name
            );
            if (entry < 0)
            {
                throw new InvalidOperationException(Missing);
            }

            return instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        };
    }

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
