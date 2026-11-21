using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamWxSparseOptionForwardChecks
{
    internal const string TestName = "compiler/beam/shares-sparse-wx-record-tests-before-default-option-forwarding";
    private const string Fixture = "beam_wx_sparse_option_forward.erl";
    private const string WrongGuardOrder = "The record checks are no longer shared ahead of scalar forwarding guards.";
    private const string WrongFallback = "The options fallback no longer preserves its own guard or environment equality.";
    private const string Missing = "A compiled fixture function is missing.";

    private const string Source = """
        -module(beam_wx_sparse_option_forward).
        -export([relay/4,dispatch/4,dispatch/5]).
        -record(wx_ref,{ref,type,state=[]}).

        -define(ENV,
            case erlang:get(wx_env) of
                {wx_env,Environment,_,_} -> Environment;
                _ -> erlang:error({wx,unknown_env})
            end).
        -define(CLASS(Type, Class), ((Type) =:= Class) orelse (Type):parent_class(Class)).
        -define(TEXT(T), (is_list(T) orelse is_binary(T))).

        relay(Receiver, Index, Text, Peer)
          when is_record(Receiver, wx_ref), is_integer(Index), ?TEXT(Text), is_record(Peer, wx_ref) ->
            dispatch(Receiver, Index, Text, Peer, []).

        dispatch(Receiver, Index, Text, Peer)
          when is_record(Receiver, wx_ref), is_integer(Index), ?TEXT(Text), is_record(Peer, wx_ref) ->
            dispatch(Receiver, Index, Text, Peer, []);
        dispatch(#wx_ref{type=ReceiverType}=Receiver, Index, Text, Options)
          when is_integer(Index), ?TEXT(Text), is_list(Options) ->
            ?CLASS(ReceiverType, fixture_receiver),
            Bytes = unicode:characters_to_binary(Text),
            Mapper = fun({mode,_}=Entry) -> Entry;
                         (Bad) -> erlang:error({badoption,Bad})
                     end,
            Effective = lists:map(Mapper, Options),
            wxe_util:queue_cmd(Receiver,Index,Bytes,Effective,?ENV,1520),
            wxe_util:rec(1520).

        dispatch(#wx_ref{type=ReceiverType}=Receiver, Index, Text, #wx_ref{type=PeerType}=Peer, Options)
          when is_integer(Index), ?TEXT(Text), is_list(Options) ->
            ?CLASS(ReceiverType, fixture_receiver),
            Bytes = unicode:characters_to_binary(Text),
            ?CLASS(PeerType, fixture_peer),
            Mapper = fun({mode,_}=Entry) -> Entry;
                         (Bad) -> erlang:error({badoption,Bad})
                     end,
            Effective = lists:map(Mapper, Options),
            wxe_util:queue_cmd(Receiver,Index,Bytes,Peer,Effective,?ENV,1521),
            wxe_util:rec(1521).
        """;

    internal static Task Run()
    {
        var bodies = ReadFunctions(Source);
        if (!HasOrderedRecordGuards(bodies("relay", 4), sharedFailure: true)
            || !HasOrderedRecordGuards(bodies("dispatch", 4), sharedFailure: false))
        {
            throw new InvalidOperationException(WrongGuardOrder);
        }

        string bound = Source.Replace("Environment", "Receiver", StringComparison.Ordinal);
        string widened = Source.Replace(
            "Text, Options)\n  when is_integer(Index)",
            "Text, Options)\n  when is_number(Index)",
            StringComparison.Ordinal
        );
        if (!ReadFunctions(bound)("dispatch", 4).Any(instruction => Opcode(instruction) == ExactEqualTest)
            || !ReadFunctions(widened)("dispatch", 4).Any(instruction => Opcode(instruction) == IsNumber))
        {
            throw new InvalidOperationException(WrongFallback);
        }

        return Task.CompletedTask;
    }

    private static bool HasOrderedRecordGuards(object[] body, bool sharedFailure)
    {
        int firstInteger = Array.FindIndex(body, instruction => Opcode(instruction) == IsInteger);
        object[][] recordOperands = body.Take(firstInteger)
            .Where(instruction => Opcode(instruction) == IsTaggedTuple).Select(Operands).ToArray();
        if (recordOperands is not [var receiver, var peer])
        {
            return false;
        }

        return Value(receiver[1]) == 0 && Value(peer[1]) == 3
            && (Value(receiver[0]) == Value(peer[0])) == sharedFailure;
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
