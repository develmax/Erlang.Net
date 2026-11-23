using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamWxInterleavedTextGuardsChecks
{
    internal const string TestName = "compiler/beam/lower-wx-chardata-between-two-record-arguments";
    private const string ShapeError = "The chardata wrapper lost its record guard order or its interleaved class frame.";
    private const string Source = """
        -module(beam_wx_interleaved_text).
        -export([mount/3,mount/4]).
        -record(wx_ref,{ref,type,state=[]}).
        -define(ENV,
            case erlang:get(wx_env) of
                {wx_env,Environment,_,_} -> Environment;
                _ -> erlang:error({wx,unknown_env})
            end).
        -define(CLASS(Type, Class), ((Type) =:= Class) orelse (Type):parent_class(Class)).
        -define(TEXT(Value), (is_list(Value) orelse is_binary(Value))).

        mount(Owner, Text, Item) when is_record(Owner,wx_ref),?TEXT(Text),is_record(Item,wx_ref) ->
            mount(Owner,Text,Item,[]).
        mount(#wx_ref{type=OwnerType}=Owner,Text,#wx_ref{type=ItemType}=Item,Options)
         when ?TEXT(Text),is_list(Options) ->
            ?CLASS(OwnerType,fixture_owner),
            Converted = unicode:characters_to_binary(Text),
            ?CLASS(ItemType,fixture_item),
            Mapper = fun({parent,#wx_ref{type=ParentType}}=Option) -> ?CLASS(ParentType,fixture_item),Option;
                        (Invalid) -> erlang:error({badoption,Invalid})
                     end,
            Mapped = lists:map(Mapper,Options),
            wxe_util:queue_cmd(Owner,Converted,Item,Mapped,?ENV,1810),
            wxe_util:rec(1810).
        """;

    internal static void Run()
    {
        string otherGuard = Source.Replace("is_record(Item,wx_ref)", "is_tuple(Item)", StringComparison.Ordinal);
        string boundEnvironment = Source.Replace("Environment", "Owner", StringComparison.Ordinal);
        if (!HasInterleavedFrame(Source) || HasInterleavedFrame(otherGuard) || HasInterleavedFrame(boundEnvironment))
        {
            throw new InvalidOperationException(ShapeError);
        }
    }

    private static bool HasInterleavedFrame(string source)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, "beam_wx_interleaved_text.erl"));
        object[] operations = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        int first = Array.FindIndex(operations, instruction => Opcode(instruction) == FunctionInfo);
        object[] forward = operations.Skip(first + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        object[] command = operations.Skip(first + forward.Length + 2)
            .TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        int guard = Array.FindIndex(forward, instruction => Opcode(instruction) == IsList);
        if (guard < 0 || forward.Take(guard).Count(instruction => Opcode(instruction) == IsTaggedTuple) != 2)
        {
            return false;
        }

        var allocation = command.SingleOrDefault(instruction => Opcode(instruction) == Allocate);

        return allocation is not null && Value(Operands(allocation)[0]) == 4
            && command.Count(instruction => Opcode(instruction) == DynamicApply) == 2
            && command.Where(instruction => Opcode(instruction) == TrimFrame)
                .Select(instruction => Value(Operands(instruction)[1])).SequenceEqual([3, 0]);
    }

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => ((IEnumerable)Property(instruction, "Operands")).Cast<object>().ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");
}
