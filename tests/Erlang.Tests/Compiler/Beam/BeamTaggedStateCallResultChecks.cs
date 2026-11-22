using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTaggedStateCallResultChecks
{
    internal const string TestName = "compiler/beam/wraps-tagged-state-call-results-without-saving-inputs";
    private const string Fixture = "beam_tagged_state_call_result_probe.erl";
    private const string WrongLayout = "Two incoming call arguments must be forwarded in registers before constructing the result.";
    private const string WrongOutcome = "The direct tagged-state call changed an argument, a matched output, or fallback.";

    private const string Source = """
        -module(beam_tagged_state_call_result_probe).
        -export([dispatch/4]).
        dispatch(event, {notice, _}=Message, {first, client}, Data) ->
            Result = notify(Message, Data),
            {kept_first, Result};
        dispatch(event, Message={notice, _}, {second, client}, Data) ->
            Result = notify(Message, Data),
            {kept_second, Result};
        dispatch(event, {single}, _, Data) -> {single, Data};
        dispatch(Event, Message, State, Data) -> {fallback, Event, Message, State, Data}.
        notify(Message, Data) -> {handled, Message, Data}.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        byte[] constrained = CodeGeneration.CompileBeamModule(
            Source.Replace(
                "Result = notify(Message, Data),\n    {kept_second, Result}",
                "Data = notify(Message, Data),\n    {kept_second, Data}",
                StringComparison.Ordinal
            ),
            Fixture
        );
        if (DirectWrappers(image) != 2 || DirectWrappers(constrained) != 1)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        var module = BeamModule.Prepare(image);
        var bound = BeamModule.Prepare(constrained);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term input = Term.A("event");
            Term message = Term.Tuple(Term.A("notice"), Term.A("payload"));
            Term data = Term.A("data");
            Term first = Term.Tuple(Term.A("first"), Term.A("client"));
            Term second = Term.Tuple(Term.A("second"), Term.A("client"));
            Term wrongRole = Term.Tuple(Term.A("first"), Term.A("server"));
            Term handled = Term.Tuple(Term.A("handled"), message, data);
            if (!(await Invoke(module, first)).Equals(Term.Tuple(Term.A("kept_first"), handled))
                || !(await Invoke(module, second)).Equals(Term.Tuple(Term.A("kept_second"), handled))
                || !(await Invoke(module, wrongRole)).Equals(Term.Tuple(
                    Term.A("fallback"),
                    input,
                    message,
                    wrongRole,
                    data
                )))
            {
                throw new InvalidOperationException(WrongOutcome);
            }

            try
            {
                _ = await Invoke(bound, second);
            }
            catch (ErlangException error) when (
                error.Reason.Equals(Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), handled))
            )
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(WrongOutcome);

            async Task<Term> Invoke(BeamModule selected, Term state) => await selected.Invoke(
                context,
                "dispatch",
                input,
                message,
                state,
                data
            );
        });
    }

    private static int DirectWrappers(byte[] image)
    {
        object[] body = Items(Code(image), "Instructions").ToArray();
        int count = 0;
        for (int index = 0; index + 3 < body.Length; index++)
        {
            if (Opcode(body[index]) != Allocate || !Operands(body[index]).Select(Value).SequenceEqual([0, 4])
                || !IsMove(body[index + 1], 1, 0) || !IsMove(body[index + 2], 3, 1))
            {
                continue;
            }

            int call = Opcode(body[index + 3]) == Line ? index + 4 : index + 3;
            if (body.Skip(call).Take(5).Select(Opcode).SequenceEqual([LocalCall, TestHeap, PutTuple, Deallocate, Return]))
            {
                count++;
            }
        }

        return count;
    }

    private static bool IsMove(object instruction, int input, int output) =>
        Opcode(instruction) == Move && Operands(instruction) is [var source, var target]
        && IsX(source, input) && IsX(target, output);

    private static bool IsX(object operand, int index) => Property(operand, "Kind") is "x" && Value(operand) == index;

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
