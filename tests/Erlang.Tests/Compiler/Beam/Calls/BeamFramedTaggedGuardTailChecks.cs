using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamFramedTaggedGuardTailChecks
{
    internal const string TestName = "compiler/beam/keeps-tagged-guard-payload-inside-its-owning-tail";
    private const string Fixture = "beam_framed_tagged_guard.erl";
    private const string BadLayout = "The selecting guard did not own its late tuple extraction and closed argument.";
    private const string BadValue = "The tagged guard changed a payload, fallback or original rejection.";

    private const string Source = """
        -module(beam_framed_tagged_guard).
        -export([dispatch/4, retained/4]).

        dispatch(Keep, [], _, _) -> erlang:error({empty, Keep});
        dispatch(Keep, [Head | Tail], Flag, Prep) ->
            Old = prepare(Prep),
            Reply = produce(Head),
            _ = restore(Old),
            case Reply of
                {failed, Reason} when Flag == true -> erlang:error({Reason, Head});
                {failed, _} when Flag == false -> finish(Keep, Tail, Flag);
                Other -> finish(Keep, Tail, {Flag, Other})
            end.

        retained(Keep, [], _, _) -> erlang:error({empty, Keep});
        retained(Keep, [Head | Tail], Flag, Prep) ->
            Old = prepare(Prep),
            Reply = produce(Head),
            _ = restore(Old),
            case Reply of
                {failed, Reason} when Flag == true -> erlang:error({Reason, Head});
                {failed, Reason} when Flag == false -> finish(Keep, Tail, Reason);
                Other -> finish(Keep, Tail, {Flag, Other})
            end.

        prepare(ready) -> prepared;
        prepare(Other) -> Other.

        produce(good) -> positive;
        produce(Reason) -> {failed, Reason}.

        restore(prepared) -> restored;
        restore(Other) -> erlang:error({bad_restore, Other}).

        finish(Keep, Tail, Flag) -> {Keep, Tail, Flag}.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        object code = Code(image);
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] body = ReadBody("dispatch");
        if (!HasLateProjection(body) || !HasLiteralFlag(body) || HasLateProjection(ReadBody("retained")))
        {
            throw new InvalidOperationException(BadLayout);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term keep = Term.A("keep");
            Term tail = Term.A("tail");
            Term reason = Term.A("reason");
            Term prepared = Term.A("ready");
            Term enabled = Term.A("true");
            Term disabled = Term.A("false");
            Term unknown = Term.A("unknown");
            Term tagged = Term.Tuple(Term.A("failed"), reason);
            await Expect(
                "dispatch",
                reason,
                disabled,
                disabled
            );
            await Expect(
                "dispatch",
                reason,
                unknown,
                Term.Tuple(unknown, tagged)
            );
            await Expect(
                "dispatch",
                Term.A("good"),
                disabled,
                Term.Tuple(disabled, Term.A("positive"))
            );
            await Expect(
                "retained",
                reason,
                disabled,
                reason
            );

            try
            {
                await module.Invoke(
                    context,
                    "dispatch",
                    keep,
                    new Cons(reason, tail),
                    enabled,
                    prepared
                );
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(reason, reason)))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(BadValue);

            async Task Expect(
                string function,
                Term input,
                Term flag,
                Term result
            )
            {
                Term expected = Term.Tuple(keep, tail, result);
                if (!expected.Equals(await module.Invoke(
                    context,
                    function,
                    keep,
                    new Cons(input, tail),
                    flag,
                    prepared
                )))
                {
                    throw new InvalidOperationException(BadValue);
                }
            }
        });

        bool HasLiteralFlag(object[] operations) => operations.Any(instruction => Opcode(instruction) == Move
            && Items(instruction, "Operands") is [var literal, var register]
            && Equals(Property(literal, "Kind"), "atom") && atoms[Value(literal)] == "false"
            && Equals(Property(register, "Kind"), "x") && Value(register) == 2);

        object[] ReadBody(string function)
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && Items(instruction, "Operands") is [_, var name, var arity]
                    && atoms[Value(name)] == function && Value(arity) == 4
            );
            if (start < 0)
            {
                throw new InvalidOperationException(BadLayout);
            }

            return instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static bool HasLateProjection(object[] body)
    {
        int select = Array.FindIndex(body, instruction => Opcode(instruction) == SelectValue);

        return select > 0 && Opcode(body[select - 1]) == IsTaggedTuple
            && body.Skip(select + 1).Zip(body.Skip(select + 2)).Any(pair =>
                Opcode(pair.First) == TestHeap && Opcode(pair.Second) == GetTupleElement);
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
