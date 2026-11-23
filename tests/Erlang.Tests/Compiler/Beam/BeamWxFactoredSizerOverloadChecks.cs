using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamWxFactoredSizerOverloadChecks
{
    internal const string TestName = "compiler/beam/preserves-wx-sizer-class-effects-and-shares-setter-receivers";
    private const string Fixture = "beam_wx_classed_constructor.erl";
    private const string WrongSetter = "Expected one common receiver projection before the scalar guard.";
    private const string WrongConstructor = "The record and options arms changed protected class effect boundaries.";
    private const string WrongBinding = "The specialized emission replaced a source-level exact match.";
    private const string Missing = "A compiled fixture function is missing.";

    private const string Source = """
        -module(beam_wx_classed_constructor).
        -export([construct/2, ratio/2]).
        -record(wx_ref,{ref,type,state=[]}).
        -define(ENV,
            case erlang:get(wx_env) of
                {wx_env,Environment,_,_} -> Environment;
                _ -> erlang:error({wx,unknown_env})
            end).
        -define(CLASS(Type, Class), ((Type) =:= Class) orelse (Type):parent_class(Class)).
        -define(CLASS_T(Type, Class), (try ?CLASS(Type,Class) catch _:_ -> false end)).

        construct(Width,Height) when is_integer(Width),is_integer(Height) ->
            construct(Width,Height,[]);
        construct(#wx_ref{type=InputType}=Item,#wx_ref{type=FlagType}=Flags) ->
            First = ?CLASS_T(InputType,wxWindow),
            Second = ?CLASS_T(InputType,wxSizer),
            Selected = if First -> ?CLASS(FlagType,wxSizerFlags), wxWindow;
                          Second -> ?CLASS(FlagType,wxSizerFlags), wxSizer;
                          true -> erlang:error({badarg,InputType}) end,
            ?CLASS(FlagType,wxSizerFlags),
            wxe_util:queue_cmd(wx:typeCast(Item,Selected),Flags,?ENV,1840),
            wxe_util:rec(1840);
        construct(#wx_ref{type=InputType}=Item,Options) when is_list(Options) ->
            First = ?CLASS_T(InputType,wxWindow),
            Second = ?CLASS_T(InputType,wxSizer),
            Selected = if First -> wxWindow;
                          Second -> wxSizer;
                          true -> erlang:error({badarg,InputType}) end,
            Mapper = fun({slot,_}=Entry) -> Entry; (Bad) -> erlang:error({badoption,Bad}) end,
            Effective = lists:map(Mapper,Options),
            wxe_util:queue_cmd(wx:typeCast(Item,Selected),Effective,?ENV,1841),
            wxe_util:rec(1841).

        construct(Width,Height,Options) -> {Width,Height,Options}.

        ratio(#wx_ref{type=Type}=Receiver,Ratio) when is_number(Ratio) ->
            ?CLASS(Type,wxSizerItem),
            wxe_util:queue_cmd(Receiver,Ratio,?ENV,1842);
        ratio(#wx_ref{type=Type}=Receiver,{W,H}=Shape) when is_integer(W),is_integer(H) ->
            ?CLASS(Type,wxSizerItem),
            wxe_util:queue_cmd(Receiver,Shape,?ENV,1843).
        """;

    internal static Task Run()
    {
        var get = ReadFunctions(Source);
        object[] setter = get("ratio", 2).Body;
        int guard = Array.FindIndex(setter, instruction => Opcode(instruction) == IsNumber);
        int projection = Array.FindIndex(setter, instruction => Opcode(instruction) == GetTupleElement);
        if (guard <= projection || projection < 0
            || setter.Count(instruction => Opcode(instruction) == IsTaggedTuple) != 3
            || setter.Count(instruction => Opcode(instruction) == Allocate) != 2)
        {
            throw new InvalidOperationException(WrongSetter);
        }

        object[] constructor = get("construct", 2).Body;
        if (constructor.Count(instruction => Opcode(instruction) == TryEnter) != 4
            || constructor.Count(instruction => Opcode(instruction) == Allocate) != 2
            || constructor.Count(instruction => Opcode(instruction) == DynamicApply) != 7
            || constructor.Count(instruction => Opcode(instruction) == MakeFun) != 1)
        {
            throw new InvalidOperationException(WrongConstructor);
        }

        string repeated = Source.Replace(
            "{W,H}=Shape) when is_integer(W),is_integer(H)",
            "{W,W}=Shape) when is_integer(W)",
            StringComparison.Ordinal
        );
        if (!ReadFunctions(repeated)("ratio", 2).Body
            .Where(instruction => Opcode(instruction) == ExactEqualTest)
            .Select(Operands)
            .Any(operands => operands.Skip(1).All(value => (string)Property(value, "Kind") is "x" or "y")))
        {
            throw new InvalidOperationException(WrongBinding);
        }

        string bound = Source.Replace(
            "Effective,?ENV,1841",
            "Effective,case erlang:get(wx_env) of {wx_env,InputType,_,_} -> InputType; _ -> erlang:error({wx,unknown_env}) end,1841",
            StringComparison.Ordinal
        );
        if (ReadFunctions(bound)("construct", 2).Body.Count(instruction => Opcode(instruction) == Allocate) == 2)
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
