using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamGuardedCallTupleProjectionChecks
{
    internal const string TestName = "compiler/beam/guarded-call-tuple-projection";
    private const string Fixture = "beam_guarded_call_projection_probe.erl";
    private const string IncorrectFrame = "The guarded call projection has incorrect ownership or failure placement.";
    private const string IncorrectResult = "The guarded call projection changed its value or failure term.";
    private const string Source = """
        -module(beam_guarded_call_projection_probe).
        -export([select/2, split_line/2, keep_second/2, selector_again/2]).
        select(Value, Mode) ->
            case normalize(Value, Mode) of
                Binary when is_binary(Binary) ->
                    {Field, _} = parse(Binary),
                    Field;
                Original -> Original
            end.
        split_line(Value, Mode) ->
            case normalize(Value, Mode) of
                Binary when is_binary(Binary) ->
                    {Field, _} =
                        parse(Binary),
                    Field;
                Original -> Original
            end.
        keep_second(Value, Mode) ->
            case normalize(Value, Mode) of
                Binary when is_binary(Binary) ->
                    {_, Field} = parse(Binary),
                    Field;
                Original -> Original
            end.
        selector_again(Value, Mode) ->
            case normalize(Value, Mode) of
                Binary when is_binary(Binary) ->
                    {Field, _} = parse(Binary),
                    {Field,Binary};
                Original -> Original
            end.
        normalize(Value, keep) -> Value;
        normalize(_, drop) -> skipped.
        parse(Binary) -> binary_to_term(Binary).
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        CheckFrame(image);
        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term value = Term.A("projected");
            Term keep = Term.A("keep");
            Term drop = Term.A("drop");
            Term encoded = new BitString(ExternalTermFormat.Encode(Term.Tuple(value, Term.I(7))));
            if (!value.Equals(await module.Invoke(
                context,
                "select",
                encoded,
                keep
            ))
                || !value.Equals(await module.Invoke(
                    context,
                    "select",
                    value,
                    keep
                ))
                || !Term.A("skipped").Equals(await module.Invoke(
                    context,
                    "select",
                    encoded,
                    drop
                ))
                || !Term.I(7).Equals(await module.Invoke(
                    context,
                    "keep_second",
                    encoded,
                    keep
                ))
                || !Term.Tuple(value, encoded).Equals(await module.Invoke(
                    context,
                    "selector_again",
                    encoded,
                    keep
                )))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            await CheckFailure("select", value);
            await CheckFailure("split_line", Term.Tuple(value, value, value));

            return Nil.Value;

            async Task CheckFailure(string function, Term result)
            {
                try
                {
                    _ = await module.Invoke(
                        context,
                        function,
                        new BitString(ExternalTermFormat.Encode(result)),
                        keep
                    );
                }
                catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                    && error.Reason.Equals(Term.Tuple(Term.A("badmatch"), result)))
                {
                    return;
                }

                throw new InvalidOperationException(IncorrectResult);
            }
        });
    }

    private static void CheckFrame(byte[] image)
    {
        object code = Code(image);
        object[] instructions = Children(code, "Instructions");
        string[] atoms = Children(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        CheckProjection("select");
        CheckProjection("split_line");

        void CheckProjection(string name)
        {
            int start = Array.FindIndex(
                instructions,
                item => Opcode(item) == FunctionInfo && atoms[(int)Value(Children(item, "Operands")[1])] == name
            );
            object[] body = instructions.Skip(start + 1).TakeWhile(item => Opcode(item) != FunctionInfo).ToArray();
            object[] frame = Children(body.Single(item => Opcode(item) == Allocate), "Operands");
            object[] part = Children(body.Single(item => Opcode(item) == GetTupleElement), "Operands");
            object[] match = Children(body.Single(item => Opcode(item) == BadMatch), "Operands");
            if (!frame.Select(Value).SequenceEqual([0, 2])
                || !part.Select(Value).SequenceEqual([0, 0, 0])
                || Value(match[0]) != 0
                || body.Count(item => Opcode(item) == Return) != 2
                || body.Count(item => Opcode(item) == Deallocate && Value(Children(item, "Operands")[0]) == 0) != 2)
            {
                throw new InvalidOperationException(IncorrectFrame);
            }
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static BigInteger Value(object operand) => (BigInteger)Property(operand, "Value");

    private static object[] Children(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
