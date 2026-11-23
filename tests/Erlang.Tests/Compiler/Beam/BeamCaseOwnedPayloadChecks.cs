using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCaseOwnedPayloadChecks
{
    internal const string TestName = "compiler/beam/reuses-case-payloads-across-call-boundaries";
    private const string FrameRegisterKind = "y";
    private const string TemporaryRegisterKind = "x";
    private const string Fixture = "beam_case_owned_payload_probe.erl";
    private const string MissingSharedPayload = "Expected the guarded case projections to share live X and dead Y slots.";

    private const string Source = """
        -module(beam_case_owned_payload_probe).
        -export([route/1]).

        route(Input) ->
            case envelope(Input) of
                {value,Ready} -> {value,Ready};
                {error,known} -> {error,{known,format(known)}};
                {error,Reason} ->
                    Key = case options(Reason) of
                        {ready,Row} ->
                            case Row of
                                {key,K} -> K;
                                {file,F} -> resolve(F)
                            end;
                        _ -> {fault,missing}
                    end,
                    case Key of
                        {fault,missing} -> {error,{missing,format(Reason)}};
                        {fault,Other} -> {error,{fault,Other}};
                        _ when is_list(Key) ->
                            case lookup(Input, none, {key,Key}) of
                                {found,Data} ->
                                    case decode(Data) of
                                        {error,Malformed} -> {error,{decode,Malformed}};
                                        Parsed -> {ok,Parsed}
                                    end;
                                {missing,LookupError} -> {error,{lookup,LookupError}}
                            end;
                        _ -> {error,{bad_key,Key}}
                    end
            end.

        envelope({config,Kind}) -> {error,Kind};
        envelope({value,Value}) -> {value,Value};
        envelope(Other) -> Other.
        format(Reason) -> {formatted,Reason}.
        options({r,Data}) -> {ready,Data};
        options(_) -> none.
        resolve({literal,Input}) -> Input;
        resolve(Value) -> Value.
        lookup(_,none,{key,[]}) -> {missing,empty_key};
        lookup(_,none,{key,[invalid]}) -> {found,invalid};
        lookup(_,none,{key,[malformed]}) -> unexpected;
        lookup(Input,none,{key,K}) -> {found,{Input,K}}.
        decode(invalid) -> {error,invalid};
        decode(Value) -> [Value].
        """;

    internal static void Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && atoms[(int)(BigInteger)Property(Items(instruction, "Operands").ElementAt(1), "Value")] == "route"
        );
        object[] route = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        bool sharedAcrossTests = route.Zip(route.Skip(1)).Any(pair =>
            TuplePayloadIndex(pair.First) == 1 && Opcode(pair.Second) == ExactEqualTest
            && IsPayloadRegister(pair.First, FrameRegisterKind)
            && SameOperand(Items(pair.First, "Operands").Last(), Items(pair.Second, "Operands").ElementAt(1))
        );
        bool sharedAcrossArms = route.Zip(route.Skip(1)).Any(pair =>
            TuplePayloadIndex(pair.First) == 1 && Opcode(pair.Second) == SelectValue
            && IsPayloadRegister(pair.First, TemporaryRegisterKind)
            && (int)(BigInteger)Property(Items(pair.First, "Operands").Last(), "Value") == 2
        );
        object[] entryFrame = Items(route.Single(instruction => Opcode(instruction) == Allocate), "Operands").ToArray();
        int width = (int)(BigInteger)Property(entryFrame[0], "Value");
        object[] entryInitialization = Items(route.First(instruction => Opcode(instruction) == InitYRegisters), "Operands").ToArray();
        int[] slots = entryInitialization.Skip(1).Select(operand => (int)(BigInteger)Property(operand, "Value")).ToArray();
        if (!sharedAcrossTests || !sharedAcrossArms || width != 3 || !slots.SequenceEqual([0, 1]))
        {
            throw new InvalidOperationException(MissingSharedPayload);
        }
    }

    private static int TuplePayloadIndex(object instruction) => Opcode(instruction) == GetTupleElement
        ? (int)(BigInteger)Property(Items(instruction, "Operands").ElementAt(1), "Value")
        : -1;

    private static bool IsPayloadRegister(object instruction, string kind) =>
        Equals(Property(Items(instruction, "Operands").Last(), "Kind"), kind);

    private static bool SameOperand(object left, object right) =>
        Equals(Property(left, "Kind"), Property(right, "Kind"))
        && Equals(Property(left, "Value"), Property(right, "Value"));

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
