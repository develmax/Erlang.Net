using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamWxSharedReceiverForwardChecks
{
    internal const string TestName = "compiler/beam/shares-wx-receiver-and-index-checks-before-object-forwarding";
    private const string Fixture = "beam_wx_shared_receiver_forward.erl";
    private const string WrongReceiver = "The two branches repeat the common wx receiver or index checks.";
    private const string WrongFallback = "The fallback lost its source-level equality or wider guard.";
    private const string Missing = "A compiled fixture function is missing.";

    private const string Source = """
        -module(beam_wx_shared_receiver_forward).
        -export([put/2,put/3,mount/3,mount/4]).
        -record(wx_ref,{ref,type,state=[]}).
        -define(ENV,
            case erlang:get(wx_env) of
                {wx_env,Environment,_,_} -> Environment;
                _ -> erlang:error({wx,unknown_env})
            end).
        -define(CLASS(Type, Class), ((Type) =:= Class) orelse (Type):parent_class(Class)).
        -define(TEXT(T), (is_list(T) orelse is_binary(T))).

        put(Receiver, Value) when is_record(Receiver, wx_ref), is_integer(Value) ->
            put(Receiver, Value, []);
        put(#wx_ref{type=ReceiverType}=Receiver, #wx_ref{type=PeerType}=Peer) ->
            ?CLASS(ReceiverType, fixture_receiver),
            ?CLASS(PeerType, fixture_peer),
            wxe_util:queue_cmd(Receiver,Peer,?ENV,1600),
            wxe_util:rec(1600).

        put(Receiver, Value, Options) when is_list(Options) ->
            wxe_util:queue_cmd(Receiver,Value,Options,?ENV,1601),
            wxe_util:rec(1601).

        mount(Receiver, Index, Text) when is_record(Receiver, wx_ref), is_integer(Index), ?TEXT(Text) ->
            mount(Receiver, Index, Text, []);
        mount(#wx_ref{type=ReceiverType}=Receiver, Index, #wx_ref{type=PeerType}=Peer) when is_integer(Index) ->
            ?CLASS(ReceiverType, fixture_receiver),
            ?CLASS(PeerType, fixture_peer),
            wxe_util:queue_cmd(Receiver,Index,Peer,?ENV,1610),
            wxe_util:rec(1610).

        mount(Receiver, Index, Text, Options) when is_list(Options) ->
            wxe_util:queue_cmd(Receiver,Index,Text,Options,?ENV,1611),
            wxe_util:rec(1611).
        """;

    internal static Task Run()
    {
        var bodies = ReadFunctions(Source);
        if (!SharesReceiver(bodies("put", 2), last: 1) || !SharesReceiver(bodies("mount", 3), last: 2))
        {
            throw new InvalidOperationException(WrongReceiver);
        }

        string bound = Source.Replace("Environment", "Receiver", StringComparison.Ordinal);
        string widened = Source.Replace("Peer) when is_integer(Index)", "Peer) when is_number(Index)", StringComparison.Ordinal);
        if (!ReadFunctions(bound)("put", 2).Any(instruction => Opcode(instruction) == ExactEqualTest)
            || !ReadFunctions(bound)("mount", 3).Any(instruction => Opcode(instruction) == ExactEqualTest)
            || !ReadFunctions(widened)("mount", 3).Any(instruction => Opcode(instruction) == IsNumber))
        {
            throw new InvalidOperationException(WrongFallback);
        }

        return Task.CompletedTask;
    }

    private static bool SharesReceiver(object[] body, int last)
    {
        int frame = Array.FindIndex(body, instruction => Opcode(instruction) == Allocate);
        object[] prefix = body.Take(frame).ToArray();
        object[][] tagged = prefix.Where(instruction => Opcode(instruction) == IsTaggedTuple).Select(Operands).ToArray();
        if (tagged is not [var receiver, var peer])
        {
            return false;
        }

        return Value(receiver[1]) == 0 && Value(peer[1]) == last
            && Value(receiver[0]) == Value(peer[0])
            && prefix.Count(instruction => Opcode(instruction) == IsInteger) == 1;
    }

    private static Func<string, int, object[]> ReadFunctions(string source)
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
