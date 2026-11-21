using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamSizeCheckedChoiceChecks
{
    internal const string TestName = "compiler/beam/size-checked-tagged-choice";
    private const string Fixture = "beam_size_checked_choice_probe.erl";
    private const string WrongFrame = "The checked choice retained an unneeded data slot or lost its selector root.";
    private const string WrongResult = "The checked choice produced the wrong result or failure.";
    private const string Source = """
        -module(beam_size_checked_choice_probe).
        -export([decode/2, custom/2]).
        decode(Input, Tag) ->
            Item = read(Input, Tag),
            case (case Item of [Single] -> Single; _ -> Item end) of
                {7,Value} ->
                    {short, begin
                        Data = field(Value, []),
                        Size = byte_size(Data),
                        if Size =:= 1 -> Data; true -> exit(wrong_short) end
                    end};
                {8,Value} -> {plain, field(Value, [])};
                {9,Value} ->
                    {long, begin
                        Data = field(Value, []),
                        Size = byte_size(Data),
                        if Size =:= 2 -> Data; true -> exit(wrong_long) end
                    end};
                Other -> exit({invalid,{selector,Other}})
            end.

        custom(Input, Tag) ->
            Item = read(Input, Tag),
            case (case Item of [Single] -> Single; _ -> Item end) of
                {7,Value} ->
                    {short, begin
                        Data = field(Value, []),
                        Size = byte_size(Data),
                        if Size =:= 1 -> Data; true -> exit(wrong_short) end
                    end};
                {8,Value} -> {plain, field(Value, [])};
                Other -> keep(Other)
            end.

        read(Input, _) -> Input.
        field(Value, _) -> Value.
        keep(Value) -> {retained,Value}.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] beam = CompileFixture();
        CheckInstructions(beam);
        var module = BeamModule.Prepare(beam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term oneByte = new BitString(new byte[] { 1 });
            Term twoBytes = new BitString(new byte[] { 1, 2 });
            Term invalid = Term.A("bad_selector");
            Term unrelated = Term.A("untouched");
            Term argument = Term.A("unused");
            Term shortValue = Term.List(Term.Tuple(Term.I(7), oneByte));
            Term longValue = Term.Tuple(Term.I(9), twoBytes);
            Term plainValue = Term.Tuple(Term.I(8), unrelated);
            if (!Term.Tuple(Term.A("short"), oneByte).Equals(await module.Invoke(
                context,
                "decode",
                shortValue,
                argument
            ))
                || !Term.Tuple(Term.A("long"), twoBytes).Equals(await module.Invoke(
                    context,
                    "decode",
                    longValue,
                    argument
                ))
                || !Term.Tuple(Term.A("plain"), unrelated).Equals(await module.Invoke(
                    context,
                    "decode",
                    plainValue,
                    argument
                ))
                || !Term.Tuple(Term.A("retained"), invalid).Equals(await module.Invoke(
                    context,
                    "custom",
                    invalid,
                    argument
                )))
            {
                throw new InvalidOperationException(WrongResult);
            }

            await VerifyFailure(Term.Tuple(Term.I(7), twoBytes), Term.A("wrong_short"));
            await VerifyFailure(Term.Tuple(Term.I(9), oneByte), Term.A("wrong_long"));
            await VerifyFailure(invalid, Term.Tuple(Term.A("invalid"), Term.Tuple(Term.A("selector"), invalid)));

            return Nil.Value;

            async Task VerifyFailure(Term input, Term reason)
            {
                try
                {
                    _ = await module.Invoke(
                        context,
                        "decode",
                        input,
                        argument
                    );
                }
                catch (ErlangException failure) when (failure.ExceptionClass == ErlangExceptionClasses.Exit
                    && failure.Reason.Equals(reason))
                {
                    return;
                }

                throw new InvalidOperationException(WrongResult);
            }
        });
    }

    private static void CheckInstructions(byte[] beam)
    {
        object code = Code(beam);
        object[] instructions = Children(code, "Instructions").ToArray();
        string[] atoms = Children(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int index = Array.FindIndex(instructions, item => Opcode(item) == FunctionInfo
            && atoms[(int)Value(Children(item, "Operands").ElementAt(1))] == "decode");
        object[] body = instructions.Skip(index + 1).TakeWhile(item => Opcode(item) != FunctionInfo).ToArray();
        object[] sizes = body.Where(item => Opcode(item) == GcBifOne).ToArray();
        object[] exits = body.Where(item => Opcode(item) == TailExternalCallWithFrame).ToArray();
        if (Value(Children(body.First(item => Opcode(item) == Allocate), "Operands").First()) != 0
            || sizes.Length != 2
            || sizes.Any(item => Value(Children(item, "Operands").ElementAt(1)) != 1
                || Value(Children(item, "Operands").Last()) != 1)
            || exits.Length != 3
            || exits.Any(item => Value(Children(item, "Operands").Last()) != 0)
            || !body.Any(item => Opcode(item) == TestHeap
                && Value(Children(item, "Operands").First()) == 6
                && Value(Children(item, "Operands").Last()) == 1))
        {
            throw new InvalidOperationException(WrongFrame);
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static BigInteger Value(object operand) => (BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Children(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
