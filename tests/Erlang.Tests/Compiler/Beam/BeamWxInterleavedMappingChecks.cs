using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamWxInterleavedMappingChecks
{
    internal const string TestName = "compiler/beam/preserves-class-checks-around-wx-text-and-option-mapping";
    private const string Fixture = "beam_wx_interleaved_option_mapping.erl";
    private const string WrongSchedule = "The wx command no longer schedules text conversion between its two class checks.";
    private const string WrongBinding = "The wx command lost a check for an existing environment or descriptor binding.";
    private const string Missing = "A compiled fixture function is missing.";

    private const string Source = """
        -module(beam_wx_interleaved_option_mapping).
        -export([attach/5,attach/6]).
        -record(wx_ref,{ref,type,state=[]}).

        -define(ENV,
            case erlang:get(wx_env) of
                {wx_env,Environment,_,_} -> Environment;
                _ -> erlang:error({wx,unknown_env})
            end).
        -define(CLASS(Type, Class), ((Type) =:= Class) orelse (Type):parent_class(Class)).
        -define(TEXT(T), (is_list(T) orelse is_binary(T))).

        attach(#wx_ref{type=ReceiverType}=Receiver,Index,Text,#wx_ref{type=PeerType}=Peer,Options)
          when is_integer(Index),?TEXT(Text),is_list(Options) ->
            ?CLASS(ReceiverType,fixture_receiver),
            Bytes = unicode:characters_to_binary(Text),
            ?CLASS(PeerType,fixture_peer),
            Mapper = fun({label,Label}) ->
                             Converted = unicode:characters_to_binary(Label),
                             {label,Converted};
                         ({mode,_}=Entry) -> Entry;
                         (Bad) -> erlang:error({badoption,Bad})
                     end,
            Effective = lists:map(Mapper,Options),
            wxe_util:queue_cmd(Receiver,Index,Bytes,Peer,Effective,?ENV,1510),
            wxe_util:rec(1510).

        attach(#wx_ref{type=ReceiverType}=Receiver,Offset,Index,Text,#wx_ref{type=PeerType}=Peer,Options)
          when is_integer(Offset),is_integer(Index),?TEXT(Text),is_list(Options) ->
            ?CLASS(ReceiverType,fixture_receiver),
            Bytes = unicode:characters_to_binary(Text),
            ?CLASS(PeerType,fixture_peer),
            Mapper = fun({label,Label}) ->
                             Converted = unicode:characters_to_binary(Label),
                             {label,Converted};
                         ({mode,_}=Entry) -> Entry;
                         (Bad) -> erlang:error({badoption,Bad})
                     end,
            Effective = lists:map(Mapper,Options),
            wxe_util:queue_cmd(Receiver,Offset,Index,Bytes,Peer,Effective,?ENV,1511),
            wxe_util:rec(1511).
        """;

    internal static Task Run()
    {
        var functions = ReadFunctions(Code(CodeGeneration.CompileBeamModule(Source, Fixture)));
        foreach (int arity in new[] { 5, 6 })
        {
            object[] body = functions(arity);
            object[] allocations = body.Where(instruction => Opcode(instruction) == Allocate).ToArray();
            int[] checks = Indices(body, DynamicApply);
            int[] calls = Indices(body, ExternalCall);
            int[] maps = Indices(body, MakeFun);
            if (allocations.Length != 1 || Value(Operands(allocations[0])[0]) != arity
                || checks is not [var firstCheck, var secondCheck]
                || calls is not [var conversion, var mapping, _] || maps is not [var mapper]
                || firstCheck >= conversion || conversion >= secondCheck || secondCheck >= mapper || mapper >= mapping)
            {
                throw new InvalidOperationException(WrongSchedule);
            }
        }

        foreach (string bound in new[]
            {
                Source.Replace("Environment", "Receiver", StringComparison.Ordinal),
                Source.Replace("PeerType", "ReceiverType", StringComparison.Ordinal)
            })
        {
            var constrained = ReadFunctions(Code(CodeGeneration.CompileBeamModule(bound, Fixture)));
            if (!constrained(5).Any(instruction => Opcode(instruction) == ExactEqualTest)
                || !constrained(6).Any(instruction => Opcode(instruction) == ExactEqualTest))
            {
                throw new InvalidOperationException(WrongBinding);
            }
        }

        return Task.CompletedTask;
    }

    private static Func<int, object[]> ReadFunctions(object code)
    {
        var instructions = Items(code, "Instructions").ToArray();
        var atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();

        return arity =>
        {
            int entry = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Operands(instruction)[1])] == "attach" && Value(Operands(instruction)[2]) == arity
            );
            if (entry < 0)
            {
                throw new InvalidOperationException(Missing);
            }

            return instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        };
    }

    private static int[] Indices(object[] body, byte opcode) => Enumerable.Range(0, body.Length)
        .Where(index => Opcode(body[index]) == opcode).ToArray();

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
