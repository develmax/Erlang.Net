using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamWxMappedTupleUnionChecks
{
    internal const string TestName = "compiler/beam/retains-wx-pair-across-protected-union-option-map";
    private const string ShapeError = "The mapped class-union branch did not retain its guarded pair through the compact frame.";
    private const string Source = """
        -module(beam_wx_mapped_tuple_union).
        -export([attach/4,attach/5]).
        -record(wx_ref,{ref,type,state=[]}).
        -define(ENV,
            case erlang:get(wx_env) of
                {wx_env,Environment,_,_} -> Environment;
                _ -> erlang:error({wx,unknown_env})
            end).
        -define(CLASS(Type, Class), ((Type) =:= Class) orelse (Type):parent_class(Class)).
        -define(PROBE(Type, Class), try ?CLASS(Type,Class) catch _:_ -> false end).

        attach(Receiver,Width,Height,{R,C}=Position)
         when is_record(Receiver,wx_ref),is_integer(Width),is_integer(Height),is_integer(R),is_integer(C) ->
            attach(Receiver,Width,Height,Position,[]);
        attach(#wx_ref{type=ReceiverType}=Receiver,#wx_ref{type=ItemType}=Item,{R,C}=Position,Options)
         when is_integer(R),is_integer(C),is_list(Options) ->
            ?CLASS(ReceiverType,fixture_receiver),
            IsWindow = ?PROBE(ItemType,fixture_window),
            IsSizer = ?PROBE(ItemType,fixture_sizer),
            Selected = if IsWindow -> fixture_window; IsSizer -> fixture_sizer; true -> error({badarg,ItemType}) end,
            Mapper = fun({tag,_}=Option) -> Option;
                        (Invalid) -> erlang:error({badoption,Invalid})
                     end,
            Mapped = lists:map(Mapper,Options),
            wxe_util:queue_cmd(Receiver,wx:typeCast(Item,Selected),Position,Mapped,?ENV,1800),
            wxe_util:rec(1800).

        attach(Receiver,Width,Height,Position,Options) -> {Receiver,Width,Height,Position,Options}.
        """;

    internal static void Run()
    {
        string largerGuard = Source.Replace(
            "when is_integer(R),is_integer(C),is_list(Options)",
            "when is_integer(R),is_number(C),is_list(Options)",
            StringComparison.Ordinal
        );
        string boundEnvironment = Source.Replace("Environment", "Receiver", StringComparison.Ordinal);
        if (!UsesMappedPairFrame(Source) || UsesMappedPairFrame(largerGuard) || UsesMappedPairFrame(boundEnvironment))
        {
            throw new InvalidOperationException(ShapeError);
        }
    }

    private static bool UsesMappedPairFrame(string source)
    {
        object code = Code(CodeGeneration.CompileBeamModule(source, "beam_wx_mapped_tuple_union.erl"));
        object[] body = ((IEnumerable)Property(code, "Instructions")).Cast<object>()
            .SkipWhile(instruction => Opcode(instruction) != FunctionInfo).Skip(1)
            .TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        int forward = Array.FindIndex(body, instruction => Opcode(instruction) == TailLocalCall);
        int frame = Array.FindIndex(body, instruction => Opcode(instruction) == Allocate);
        if (forward < 0 || frame <= forward || Value(Operands(body[frame])[0]) != 7)
        {
            return false;
        }

        int[] retained = body.Where(instruction => Opcode(instruction) == TrimFrame)
            .Select(instruction => Value(Operands(instruction)[1])).ToArray();

        return retained.SequenceEqual([4, 3, 0])
            && body.Count(instruction => Opcode(instruction) == TryEnter) == 2;
    }

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => ((IEnumerable)Property(instruction, "Operands")).Cast<object>().ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");
}
