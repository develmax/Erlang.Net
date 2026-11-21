using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamForwardedChoiceAndProtectedProjectionChecks
{
    internal const string TestName = "compiler/beam/forwards-whole-optionals-and-keeps-projection-failures-protected";
    private const string Fixture = "beam_forwarded_choice_and_projection_probe.erl";
    private const string WrongInstructions = "The projection or selected fields lost the frame proven by their source.";
    private const string WrongValue = "The projection or selected fields produced an unexpected result.";

    private const string Source = """
        -module(beam_forwarded_choice_and_projection_probe).
        -export([encode/2, decode/2, run/1, measured/2]).

        encode(Key, Tags) ->
            try iolist_to_binary(element(1, produce(Key,Tags))) of
                Bytes -> {ok,Bytes}
            catch
                Class:Exception:Stk when Class =:= error; Class =:= exit ->
                    case Exception of
                        {error,{asn1,Reason}} -> {error,{asn1,{Reason,Stk}}};
                        Reason -> {error,{asn1,{Reason,Stk}}}
                    end
            end.

        produce(good, _) -> {<<11,12>>,2};
        produce(_, _) -> invalid.

        decode(Value, Tags) ->
            Initial = prepare(Value,Tags),
            [Head|Rest] = Initial,
            Required = field(Head,[]),
            {Whole,Tail} = case Rest of
                [First = {3,_}|R1] -> {field(First,[]),R1};
                [Other = {7,_}|R2] -> {field(Other,[]),R2};
                _ -> {absent,Rest}
            end,
            {Last,Remaining} = case Tail of
                [{9,V}|R3] -> {field(V,[]),R3};
                _ -> {absent,Tail}
            end,
            case Remaining of [] -> true; _ -> exit({error,{asn1,{unexpected,Remaining}}}) end,
            Result = {decoded,Required,Whole,Last},
            Result.

        measured(Value, Tags) when is_binary(Value) -> pair(Tags,Value,byte_size(Value));
        measured(Value, Tags) -> pair(Tags,Value,length(Value)).

        run(Key) -> (factory(Key))().
        factory(Key) when Key =:= {1,0} -> fun first/0;
        factory(Key) when Key =:= {1,1} -> fun second/0;
        factory(Key) -> fun() -> {missing,Key} end.
        first() -> first.
        second() -> second.

        prepare(Value, _) -> Value.
        field(Value, _) -> Value.
        pair(Tags, Value, Count) -> {Tags,Value,Count}.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        Check("encode", [1]);
        Check("decode", [4]);
        Check("factory", []);
        Check("measured", []);

        await using var runtime = new ProcessRuntime();
        var module = BeamModule.Prepare(image);
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term nil = Nil.Value;
            Term first = Term.A("first");
            Term last = Term.A("last");
            Term whole = Term.Tuple(Term.I(7), Term.A("whole"));
            Term key = Term.Tuple(Term.I(1), Term.I(2));
            Term tagged = Term.Tuple(Term.I(9), last);
            Term bytes = new BitString([11, 12]);
            Term encoded = await module.Invoke(
                context,
                "encode",
                Term.A("good"),
                nil
            );
            Term invalid = await module.Invoke(
                context,
                "encode",
                Term.A("bad"),
                nil
            );
            Term decoded = await module.Invoke(
                context,
                "decode",
                Term.List(first, whole, tagged),
                nil
            );
            Term fallback = await module.Invoke(context, "run", key);
            Term measured = await module.Invoke(
                context,
                "measured",
                bytes,
                first
            );
            if (!Term.Tuple(Term.A("ok"), bytes).Equals(encoded)
                || invalid is not TupleTerm { Items: [Atom { Name: "error" }, TupleTerm] }
                || !Term.Tuple(
                    Term.A("decoded"),
                    first,
                    whole,
                    last
                ).Equals(decoded)
                || !Term.Tuple(Term.A("missing"), key).Equals(fallback)
                || !Term.Tuple(first, bytes, Term.I(2)).Equals(measured))
            {
                throw new InvalidOperationException(WrongValue);
            }

            return Nil.Value;
        });

        void Check(string name, int[] expectedFrames)
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && Items(instruction, "Operands").ToArray() is [_, var atom, _]
                    && atoms[Value(atom)] == name
            );
            if (start < 0)
            {
                throw new InvalidOperationException(WrongInstructions);
            }

            var frames = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
                .Where(instruction => Opcode(instruction) == Allocate)
                .Select(instruction => Value(Items(instruction, "Operands").First())).ToArray();
            if (!expectedFrames.SequenceEqual(frames))
            {
                throw new InvalidOperationException(WrongInstructions);
            }
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
