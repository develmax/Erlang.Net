using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamWxGuardedBitmapCommandChecks
{
    internal const string TestName = "compiler/beam/shares-wx-guard-failures-with-static-option-commands";
    private const string Fixture = "beam_wx_guarded_bitmap_commands.erl";
    private const string WrongForward = "A guard shared by both constructor alternatives branches to the mapped command.";
    private const string WrongOptionPair = "The object and mapped option branches no longer share the receiver and scalar checks.";
    private const string WrongBinding = "The command lost its wider fallback guard or an existing environment comparison.";
    private const string Missing = "A compiled fixture function is missing.";

    private const string Source = """
        -module(beam_wx_guarded_bitmap_commands).
        -export([construct/3,construct/4,attach/4]).
        -record(wx_ref,{ref,type,state=[]}).
        -define(ENV,
            case erlang:get(wx_env) of
                {wx_env,Environment,_,_} -> Environment;
                _ -> erlang:error({wx,unknown_env})
            end).
        -define(CLASS(Type, Class), ((Type) =:= Class) orelse (Type):parent_class(Class)).

        construct(Data,Width,Height) when is_binary(Data),is_integer(Width),is_integer(Height) ->
            construct(Data,Width,Height,[]);
        construct(Width,Height,Options) when is_integer(Width),is_integer(Height),is_list(Options) ->
            Mapper = fun({depth,_}=Entry) -> Entry;
                         (Bad) -> erlang:error({badoption,Bad}) end,
            Effective = lists:map(Mapper,Options),
            wxe_util:queue_cmd(Width,Height,Effective,?ENV,1790),
            wxe_util:rec(1790).

        construct(Data,Width,Height,Options) when is_binary(Data),is_integer(Width),is_integer(Height),is_list(Options) ->
            Mapper = fun({depth,_}=Entry) -> Entry;
                         (Bad) -> erlang:error({badoption,Bad}) end,
            Effective = lists:map(Mapper,Options),
            wxe_util:queue_cmd(Data,Width,Height,Effective,?ENV,1791),
            wxe_util:rec(1791).

        attach(#wx_ref{type=ReceiverType}=Receiver,Width,Height,Options)
          when is_integer(Width),is_integer(Height),is_list(Options) ->
            ?CLASS(ReceiverType, fixture_receiver),
            Mapper = fun({depth,_}=Entry) -> Entry;
                         (Bad) -> erlang:error({badoption,Bad}) end,
            Effective = lists:map(Mapper,Options),
            wxe_util:queue_cmd(Receiver,Width,Height,Effective,?ENV,1792),
            wxe_util:rec(1792);
        attach(#wx_ref{type=ReceiverType}=Receiver,Width,Height,#wx_ref{type=PeerType}=Peer)
          when is_integer(Width),is_integer(Height) ->
            ?CLASS(ReceiverType, fixture_receiver),
            ?CLASS(PeerType, fixture_peer),
            wxe_util:queue_cmd(Receiver,Width,Height,Peer,?ENV,1793),
            wxe_util:rec(1793).
        """;

    internal static Task Run()
    {
        var bodies = ReadFunctions(Source);
        var (construct, failure) = bodies("construct", 3);
        object[] forward = construct.TakeWhile(instruction => Opcode(instruction) != TailLocalCall).ToArray();
        object[] common = forward.Where(instruction => Opcode(instruction) == IsInteger)
            .Where(instruction => Value(Operands(instruction)[1]) == 1).ToArray();
        if (common is not [var width] || Value(Operands(width)[0]) != failure
            || Array.FindIndex(construct, instruction => Opcode(instruction) == MakeFun) < forward.Length)
        {
            throw new InvalidOperationException(WrongForward);
        }

        var (attach, _) = bodies("attach", 4);
        int projection = Array.FindIndex(attach, instruction => Opcode(instruction) == GetTupleElement);
        int list = Array.FindIndex(attach, instruction => Opcode(instruction) == IsList);
        int frame = Array.FindIndex(attach, instruction => Opcode(instruction) == Allocate);
        if (projection < 0 || projection >= list || list >= frame
            || attach.Count(instruction => Opcode(instruction) == IsInteger) != 2
            || attach.Take(projection).Count(instruction => Opcode(instruction) == IsInteger) != 2)
        {
            throw new InvalidOperationException(WrongOptionPair);
        }

        var bound = ReadFunctions(Source.Replace("Environment", "Width", StringComparison.Ordinal));
        string wider = Source.Replace("Peer)\n  when is_integer(Width)", "Peer)\n  when is_number(Width)", StringComparison.Ordinal);
        if (!bound("construct", 3).Body.Any(instruction => Opcode(instruction) == ExactEqualTest)
            || !bound("attach", 4).Body.Any(instruction => Opcode(instruction) == ExactEqualTest)
            || !ReadFunctions(wider)("attach", 4).Body.Any(instruction => Opcode(instruction) == IsNumber))
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
