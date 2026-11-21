using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamClosedAtomPairChecks
{
    internal const string TestName = "compiler/beam/groups-closed-atom-arms-with-owned-source-lines";
    private const string WrongLayout = "Expected one ordered atom selector with two distinct terminal bodies.";
    private const string WrongValue = "A selected branch or its original failure changed behavior.";
    private const string Source = """
        -module(beam_closed_atom_pair).
        -export([dispatch/2, checked/2, previous/0]).

        dispatch(Payload, print) ->
            _ = erlang:put(recorded, {printed, Payload}),
            {printed, Payload};
        dispatch(Payload, get) ->
            _ = erlang:put(recorded, {read, Payload}),
            {read, Payload}.

        checked({Payload}, print) -> dispatch(Payload, print);
        checked(Payload, get) when is_atom(Payload) -> dispatch(Payload, get).

        previous() -> erlang:get(recorded).
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, "beam_closed_atom_pair.erl");
        CheckLayout(image);
        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term payload = Term.A("payload");
            Term printed = Term.Tuple(Term.A("printed"), payload);
            Term read = Term.Tuple(Term.A("read"), payload);
            if (!printed.Equals(
                await module.Invoke(
                    context,
                    "dispatch",
                    payload,
                    Term.A("print")
                )) || !printed.Equals(await module.Invoke(context, "previous"))
                || !read.Equals(
                    await module.Invoke(
                        context,
                        "checked",
                        payload,
                        Term.A("get")
                    )) || !read.Equals(await module.Invoke(context, "previous")))
            {
                throw new InvalidOperationException(WrongValue);
            }

            await Reject("dispatch", payload, Term.A("neither"));
            await Reject("checked", payload, Term.A("print"));
            await Reject("checked", Term.I(2), Term.A("get"));
            if (!read.Equals(await module.Invoke(context, "previous")))
            {
                throw new InvalidOperationException(WrongValue);
            }

            return Nil.Value;

            async Task Reject(string function, Term input, Term selector)
            {
                try
                {
                    await module.Invoke(
                        context,
                        function,
                        input,
                        selector
                    );
                }
                catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.FunctionClause)))
                {
                    return;
                }

                throw new InvalidOperationException(WrongValue);
            }
        });
    }

    private static void CheckLayout(byte[] image)
    {
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions");
        int info = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Items(instruction, "Operands") is [_, var name, var arity]
                && atoms[Value(name)] == "dispatch" && Value(arity) == 2
        );
        if (info < 0 || Opcode(instructions[info + 2]) != SelectValue
            || Items(instructions[info + 2], "Operands") is not
            [var selector, var failure, _, var low, var lowLabel, var high, var highLabel]
            || Value(selector) != 1 || atoms[Value(low)] != "get" || atoms[Value(high)] != "print"
            || Value(lowLabel) <= Value(highLabel) || Value(failure) >= Value(highLabel))
        {
            throw new InvalidOperationException(WrongLayout);
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
