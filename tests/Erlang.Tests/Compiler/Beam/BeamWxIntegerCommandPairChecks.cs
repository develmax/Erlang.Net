using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamWxIntegerCommandPairChecks
{
    internal const string TestName = "compiler/beam/shares-checked-wx-receivers-across-integer-command-branches";
    private const string Fixture = "beam_wx_integer_command_pair.erl";
    private const string WrongFrames = "The wx overload no longer shares receiver projections and uses frames local to each branch.";
    private const string WrongColourSlot = "The colour call no longer frees the saved receiver class and colour slots together.";
    private const string WrongEnvironment = "The wx overload lost exact equality against the already-bound receiver.";
    private const string Missing = "A compiled fixture function is missing.";

    private const string Source = """
        -module(beam_wx_integer_command_pair).
        -export([choose/2,coloured/3]).

        -record(wx_ref,{ref,type,state=[]}).

        -define(ENV,
            case erlang:get(wx_env) of
                {wx_env,Environment,_,_} -> Environment;
                _ -> erlang:error({wx,unknown_env})
            end).
        -define(CLASS(Type,Class), ((Type) =:= Class) orelse (Type):parent_class(Class)).
        -define(COLOUR(C), (tuple_size(C) =:= 3 orelse tuple_size(C) =:= 4) andalso is_integer(element(1,C))).

        choose(#wx_ref{type=ReceiverT}=Receiver,Index) when is_integer(Index) ->
            ?CLASS(ReceiverT,fixture_sink),
            wxe_util:queue_cmd(Receiver,Index,?ENV,810),
            wxe_util:rec(810);
        choose(#wx_ref{type=ReceiverT}=Receiver,#wx_ref{type=ItemT}=Item) ->
            ?CLASS(ReceiverT,fixture_sink),
            ?CLASS(ItemT,fixture_item),
            wxe_util:queue_cmd(Receiver,Item,?ENV,811),
            wxe_util:rec(811).

        coloured(#wx_ref{type=ReceiverT}=Receiver,#wx_ref{type=ItemT}=Item,Index) when is_integer(Index) ->
            ?CLASS(ReceiverT,fixture_sink),
            ?CLASS(ItemT,fixture_item),
            wxe_util:queue_cmd(Receiver,Item,Index,?ENV,820),
            wxe_util:rec(820);
        coloured(#wx_ref{type=ReceiverT}=Receiver,#wx_ref{type=ItemT}=Item,Colour) when ?COLOUR(Colour) ->
            ?CLASS(ReceiverT,fixture_sink),
            ?CLASS(ItemT,fixture_item),
            wxe_util:queue_cmd(Receiver,Item,wxe_util:color(Colour),?ENV,821),
            wxe_util:rec(821).
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        var instructions = Items(code, "Instructions").ToArray();
        var atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] choose = Body("choose");
        object[] colour = Body("coloured");
        if (!HasBranchFrames(choose, 2) || !HasBranchFrames(colour, 4))
        {
            throw new InvalidOperationException(WrongFrames);
        }

        int trim = Array.FindIndex(
            colour,
            instruction => Opcode(instruction) == TrimFrame
            && Value(Operands(instruction)[0]) == 2 && Value(Operands(instruction)[1]) == 2
        );
        if (trim <= 0 || Opcode(colour[trim - 1]) != Move || Value(Operands(colour[trim - 1])[0]) != 1)
        {
            throw new InvalidOperationException(WrongColourSlot);
        }

        string bound = Source.Replace("Environment", "Receiver", StringComparison.Ordinal);
        code = Code(CodeGeneration.CompileBeamModule(bound, Fixture));
        instructions = Items(code, "Instructions").ToArray();
        atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        if (!Body("choose").Any(instruction => Opcode(instruction) == ExactEqualTest)
            || !Body("coloured").Any(instruction => Opcode(instruction) == ExactEqualTest))
        {
            throw new InvalidOperationException(WrongEnvironment);
        }

        return Task.CompletedTask;

        object[] Body(string name)
        {
            int index = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Operands(instruction)[1])] == name
            );
            if (index < 0)
            {
                throw new InvalidOperationException(Missing);
            }

            return instructions.Skip(index + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static bool HasBranchFrames(object[] body, int size)
    {
        int projection = Array.FindIndex(body, instruction => Opcode(instruction) == GetTupleElement);
        int guard = Array.FindIndex(body, instruction => Opcode(instruction) == IsInteger);
        int frame = Array.FindIndex(body, instruction => Opcode(instruction) == Allocate);
        object[] allocations = body.Where(instruction => Opcode(instruction) == Allocate).ToArray();

        return projection >= 0 && projection < guard && guard < frame && allocations.Length == 2
            && allocations.All(instruction => Value(Operands(instruction)[0]) == size);
    }

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
