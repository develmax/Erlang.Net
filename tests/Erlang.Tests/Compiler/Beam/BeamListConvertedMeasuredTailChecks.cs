using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamListConvertedMeasuredTailChecks
{
    internal const string TestName = "compiler/beam/list-converted-measured-tail";
    private const string Fixture = "beam_list_converted_measure_probe.erl";
    private const string IncorrectFrame = "The measured recursion did not preserve the exact tag, frame or BIF roots.";
    private const string IncorrectResult = "The measured recursion changed its return or failure.";
    private const string Source = """
        -module(beam_list_converted_measure_probe).
        -export([empty_tag/1, mixed_tag/2, fixed_nonempty/1, changed_tag/1, capture/0]).
        empty_tag(Value) -> nil_measure(Value, []).
        mixed_tag(Value, Tag) -> mixed_measure(Value, Tag).
        fixed_nonempty(Value) -> nonempty_measure(Value, [<<1>>]).
        changed_tag(Value) -> change_measure(Value, []).
        capture() -> fun mixed_measure/2.
        nil_measure(Value, Tag) when is_list(Value) ->
            nil_measure(list_to_binary(Value), Tag);
        nil_measure(Value, Tag) ->
            packet(Tag, Value, byte_size(Value)).
        mixed_measure(Value, Tag) when is_list(Value) ->
            mixed_measure(list_to_binary(Value), Tag);
        mixed_measure(Value, Tag) ->
            packet(Tag, Value, byte_size(Value)).
        nonempty_measure(Value, Tag) when is_list(Value) ->
            nonempty_measure(list_to_binary(Value), Tag);
        nonempty_measure(Value, Tag) ->
            packet(Tag, Value, byte_size(Value)).
        change_measure(Value, Tag) when is_list(Value) ->
            change_measure(list_to_binary(Value), [changed|Tag]);
        change_measure(Value, Tag) ->
            packet(Tag, Value, byte_size(Value)).
        packet(Tag, Value, Length) -> {Tag, Value, Length}.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        CheckFrames(image);
        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term bytes = new BitString(new byte[] { 1, 2 });
            Term input = Term.List(Term.I(1), Term.I(2));
            Term length = Term.I(2);
            Term tag = Term.A("tag");
            Term nonempty = Term.List(new BitString(new byte[] { 1 }));
            if (!Term.Tuple(Nil.Value, bytes, length).Equals(await module.Invoke(context, "empty_tag", input))
                || !Term.Tuple(Nil.Value, bytes, length).Equals(await module.Invoke(context, "empty_tag", bytes))
                || !Term.Tuple(tag, bytes, length).Equals(await module.Invoke(
                    context,
                    "mixed_tag",
                    input,
                    tag
                ))
                || !Term.Tuple(nonempty, bytes, length).Equals(await module.Invoke(context, "fixed_nonempty", input))
                || !Term.Tuple(Term.List(Term.A("changed")), bytes, length).Equals(await module.Invoke(context, "changed_tag", input)))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            await CheckBadArgument(Term.A("not_data"));
            await CheckBadArgument(Term.List(Term.I(256)));

            return Nil.Value;

            async Task CheckBadArgument(Term value)
            {
                try
                {
                    _ = await module.Invoke(context, "empty_tag", value);
                }
                catch (ErlangException failure) when (failure.ExceptionClass == ErlangExceptionClasses.Error
                    && failure.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
                {
                    return;
                }

                throw new InvalidOperationException(IncorrectResult);
            }
        });
    }

    private static void CheckFrames(byte[] image)
    {
        object code = Code(image);
        object[] instructions = Children(code, "Instructions");
        string[] atoms = Children(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        CheckFrame("nil_measure", 0, 1);
        CheckFrame("mixed_measure", 1, 2);
        CheckFrame("nonempty_measure", 1, 2);

        void CheckFrame(string name, int slots, int live)
        {
            int start = Array.FindIndex(
                instructions,
                item => Opcode(item) == FunctionInfo && atoms[(int)Value(Children(item, "Operands")[1])] == name
            );
            object[] body = instructions.Skip(start + 1).TakeWhile(item => Opcode(item) != FunctionInfo).ToArray();
            object[] allocations = body.Where(item => Opcode(item) == Allocate).ToArray();
            object[] measured = Children(body.Single(item => Opcode(item) == GcBifOne), "Operands");
            if (allocations.Length != 1 || !Children(allocations[0], "Operands").Select(Value).SequenceEqual([slots, live])
                || Value(measured[1]) != live || Property(measured[3], "TypeIndex") is not null
                || Value(measured[^1]) != 2
                || body.Count(item => Opcode(item) == Swap) != slots)
            {
                throw new InvalidOperationException(IncorrectFrame);
            }
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static BigInteger Value(object operand) => (BigInteger)Property(operand, "Value");

    private static object[] Children(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
