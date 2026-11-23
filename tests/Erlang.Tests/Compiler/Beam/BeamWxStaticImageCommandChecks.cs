using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamWxStaticImageCommandChecks
{
    internal const string TestName = "compiler/beam/frames-static-wx-option-mappers-and-colour-overloads";
    private const string Fixture = "beam_wx_static_image_commands.erl";
    private const string WrongFrame = "The image command no longer reuses the receiver projections or retains only the live mapper inputs.";
    private const string WrongBinding = "The image command lost equality with an existing environment variable.";
    private const string Missing = "A compiled fixture function is missing.";

    private const string Source = """
        -module(beam_wx_static_image_commands).
        -export([combine/3,construct/3,bits/4]).
        -record(wx_ref,{ref,type,state=[]}).
        -define(ENV,
            case erlang:get(wx_env) of
                {wx_env,Environment,_,_} -> Environment;
                _ -> erlang:error({wx,unknown_env})
            end).
        -define(CLASS(Type, Class), ((Type) =:= Class) orelse (Type):parent_class(Class)).
        -define(COLOUR(C), (tuple_size(C) =:= 3 orelse tuple_size(C) =:= 4) andalso is_integer(element(1,C))).

        combine(#wx_ref{type=ReceiverType}=Receiver,#wx_ref{type=PeerType}=Peer,#wx_ref{type=MaskType}=Mask) ->
            ?CLASS(ReceiverType, fixture_receiver),
            ?CLASS(PeerType, fixture_peer),
            ?CLASS(MaskType, fixture_mask),
            wxe_util:queue_cmd(Receiver,Peer,Mask,?ENV,1780),
            wxe_util:rec(1780);
        combine(#wx_ref{type=ReceiverType}=Receiver,#wx_ref{type=PeerType}=Peer,Colour) when ?COLOUR(Colour) ->
            ?CLASS(ReceiverType, fixture_receiver),
            ?CLASS(PeerType, fixture_peer),
            wxe_util:queue_cmd(Receiver,Peer,wxe_util:color(Colour),?ENV,1781),
            wxe_util:rec(1781).

        construct(Width, Height, Options) when is_integer(Width), is_integer(Height), is_list(Options) ->
            Mapper = fun({mode,_}=Entry) -> Entry;
                         (Bad) -> erlang:error({badoption,Bad})
                     end,
            Effective = lists:map(Mapper,Options),
            wxe_util:queue_cmd(Width,Height,Effective,?ENV,1782),
            wxe_util:rec(1782).

        bits(Bits, Width, Height, Options) when is_binary(Bits), is_integer(Width), is_integer(Height), is_list(Options) ->
            Mapper = fun({depth,_}=Entry) -> Entry;
                         (Bad) -> erlang:error({badoption,Bad})
                     end,
            Effective = lists:map(Mapper,Options),
            wxe_util:queue_cmd(Bits,Width,Height,Effective,?ENV,1783),
            wxe_util:rec(1783).
        """;

    internal static Task Run()
    {
        var bodies = ReadFunctions(Source);
        object[] twoObjects = bodies("combine");
        object[] frames = twoObjects.Where(instruction => Opcode(instruction) == Allocate).ToArray();
        int projection = Array.FindIndex(twoObjects, instruction => Opcode(instruction) == GetTupleElement);
        int firstFrame = Array.FindIndex(twoObjects, instruction => Opcode(instruction) == Allocate);
        if (projection < 0 || projection >= firstFrame || frames.Length != 2
            || frames.Any(instruction => Value(Operands(instruction)[0]) != 4)
            || !HasMapperFrame(bodies("construct"), size: 2)
            || !HasMapperFrame(bodies("bits"), size: 3))
        {
            throw new InvalidOperationException(WrongFrame);
        }

        var boundObject = ReadFunctions(Source.Replace("Environment", "Receiver", StringComparison.Ordinal));
        var boundScalar = ReadFunctions(Source.Replace("Environment", "Width", StringComparison.Ordinal));
        if (!HasEquality(boundObject("combine")) || !HasEquality(boundScalar("construct")) || !HasEquality(boundScalar("bits")))
        {
            throw new InvalidOperationException(WrongBinding);
        }

        return Task.CompletedTask;
    }

    private static bool HasMapperFrame(object[] body, int size)
    {
        object[] frames = body.Where(instruction => Opcode(instruction) is Allocate or AllocateHeap).ToArray();

        return frames is [var frame] && Opcode(frame) == AllocateHeap && Value(Operands(frame)[0]) == size
            && body.Count(instruction => Opcode(instruction) == MakeFun) == 1
            && body.Count(instruction => Opcode(instruction) == TrimFrame) == 1;
    }

    private static bool HasEquality(object[] body) => body.Any(instruction => Opcode(instruction) == ExactEqualTest);

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
