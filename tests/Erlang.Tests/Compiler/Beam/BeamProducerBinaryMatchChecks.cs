using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamProducerBinaryMatchChecks
{
    internal const string TestName = "compiler/beam/keeps-binary-producer-result-on-failure-edges";
    private const string Fixture = "beam_producer_binary_match_probe.erl";
    private const string UntypedConstruction = "The positioned binary constructor must retain all three proved operand types.";
    private const string LostTailContext = "The terminal binary size computation must keep its positioned tail.";
    private const string SpilledProducer = "The fixed binary match must use the call result directly and keep it for badmatch.";

    private const string Source = """
        -module(beam_producer_binary_match_probe).
        -export([pack/1, pack_sized/1, retained/1]).

        pack(Input) ->
            <<Head, Rest/binary>> = make(Input),
            case Head band 128 of
                128 -> <<0,Head,Rest/binary>>;
                _ -> <<Head,Rest/binary>>
            end.

        pack_sized(Input) ->
            <<Head, Rest/binary>> = make(Input),
            case Head band 128 of
                128 -> <<(byte_size(Rest)+2):32,0,Head,Rest/binary>>;
                _ -> <<(byte_size(Rest)+1):32,Head,Rest/binary>>
            end.
        retained(Input) ->
            Whole = make(Input),
            <<Head, Rest/binary>> = Whole,
            {Whole,Head,Rest}.

        make(high) -> <<128,7,3>>;
        make(low) -> <<3,5,8>>;
        make(empty) -> <<>>;
        make(unaligned) -> <<5:3>>;
        make(non_binary) -> rejected;
        make(Other) -> Other.
        """;

    internal static void Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        byte[] pack = ReadFunction("pack").Select(Opcode).ToArray();
        if (!pack.Zip(pack.Skip(1)).Any(pair =>
            pair.First is LocalCall or ExternalCall && pair.Second == StartBinaryMatch))
        {
            throw new InvalidOperationException(SpilledProducer);
        }

        object[] sized = ReadFunction("pack_sized");
        if (sized.Count(instruction => Opcode(instruction) == GetBinaryTail) != 2)
        {
            throw new InvalidOperationException(LostTailContext);
        }

        object[] constructors = sized.Where(instruction => Opcode(instruction) == CreateBinary).ToArray();
        if (constructors.Length != 2 || constructors.Any(construction => Items(construction, "Operands").Skip(6)
            .Count(operand => operand.GetType().GetProperty("TypeIndex")?.GetValue(operand) is int) != 3))
        {
            throw new InvalidOperationException(UntypedConstruction);
        }

        object[] ReadFunction(string name)
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && atoms[(int)(BigInteger)Property(Items(instruction, "Operands").ElementAt(1), "Value")] == name
            );

            return instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
