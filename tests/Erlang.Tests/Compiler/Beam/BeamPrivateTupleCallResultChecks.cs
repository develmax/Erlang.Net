using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamPrivateTupleCallResultChecks
{
    internal const string TestName = "compiler/beam/private-tuple-call-result";
    private const string Fixture = "beam_private_tuple_call_result_probe.erl";
    private const string IncorrectShape = "Private tuple shape tests were omitted without a complete local call proof.";
    private const string IncorrectResult = "Private tuple summary changed a retained value or failure.";
    private const string Source = """
        -module(beam_private_tuple_call_result_probe).
        -export([use/2, use_mixed/2, use_direct/2, use_escape/2, capture/0, public_pair/2]).
        use(Tag, Input) ->
            consume(Tag, producer(Input)).
        use_direct(Tag, Input) ->
            direct(Tag, {Input, token}).
        use_mixed(Tag, Input) ->
            mixed(Tag, unknown_producer(Input)).
        use_escape(Tag, Input) ->
            escaped(Tag, producer(Input)).
        capture() -> fun escaped/2.
        public_pair(Tag, {First, Second}) ->
            finish(Tag, First, Second).
        producer({wrapped, First, Second}) ->
            producer({pair, First, Second});
        producer({pair, First, Second}) ->
            {First, Second};
        producer(failure) ->
            erlang:error(no_value).
        unknown_producer(wrapped) ->
            producer({pair, result, 7});
        unknown_producer(other) ->
            not_tuple.
        consume(Tag, {First, Second}) ->
            finish(Tag, First, Second).
        direct(Tag, {First, Second}) ->
            finish(Tag, First, Second).
        mixed(Tag, {First, Second}) ->
            finish(Tag, First, Second).
        escaped(Tag, {First, Second}) ->
            finish(Tag, First, Second).
        finish(Tag, First, Second) ->
            {Tag, First, Second}.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        CheckTupleEntry(image);
        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term tag = Term.A("tag");
            Term left = Term.A("result");
            Term right = Term.I(7);
            Term input = Term.Tuple(Term.A("wrapped"), left, right);
            Term expected = Term.Tuple(tag, left, right);
            if (!expected.Equals(await module.Invoke(
                context,
                "use",
                tag,
                input
            ))
                || !expected.Equals(await module.Invoke(
                    context,
                    "use_escape",
                    tag,
                    input
                ))
                || !expected.Equals(await module.Invoke(
                    context,
                    "use_mixed",
                    tag,
                    Term.A("wrapped")
                ))
                || !Term.Tuple(tag, left, Term.A("token")).Equals(await module.Invoke(
                    context,
                    "use_direct",
                    tag,
                    left
                )))
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            await CheckFailure("use", Term.A("failure"), "no_value");
            await CheckFailure("use_mixed", Term.A("other"), "function_clause");
            await CheckFailure("public_pair", Term.A("other"), "function_clause");

            return Nil.Value;

            async Task CheckFailure(string function, Term argument, string reason)
            {
                try
                {
                    _ = await module.Invoke(
                        context,
                        function,
                        tag,
                        argument
                    );
                }
                catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                    && error.Reason.Equals(Term.A(reason)))
                {
                    return;
                }

                throw new InvalidOperationException(IncorrectResult);
            }
        });
    }

    private static void CheckTupleEntry(byte[] image)
    {
        object code = Code(image);
        object[] instructions = Children(code, "Instructions");
        string[] atoms = Children(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        byte[] consume = Body("consume");
        if (consume.Length < 4
            || !consume.Take(4).SequenceEqual([GetTupleElement, GetTupleElement, Swap, TailLocalCall])
            || !HasTupleTests(Body("mixed"))
            || !HasTupleTests(Body("escaped"))
            || !HasTupleTests(Body("public_pair")))
        {
            throw new InvalidOperationException(IncorrectShape);
        }

        static bool HasTupleTests(byte[] body) => body.Contains(IsTuple) && body.Contains(TestArity);

        byte[] Body(string name)
        {
            int start = Array.FindIndex(
                instructions,
                item => Opcode(item) == FunctionInfo && atoms[(int)Value(Children(item, "Operands")[1])] == name
            );

            return instructions.Skip(start + 1).SkipWhile(item => Opcode(item) == Label)
                .TakeWhile(item => Opcode(item) != FunctionInfo).Select(Opcode).ToArray();
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static BigInteger Value(object operand) => (BigInteger)Property(operand, "Value");

    private static object[] Children(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
