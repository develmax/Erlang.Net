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
        -export([dispatch/2, checked/2, previous/0, route/3, guarded/3, forward/3,
                 private_route/2, finite_route/1, multi_route/2]).

        dispatch(Payload, print) ->
            _ = erlang:put(recorded, {printed, Payload}),
            {printed, Payload};
        dispatch(Payload, get) ->
            _ = erlang:put(recorded, {read, Payload}),
            {read, Payload}.

        private_route(Payload, Mode) -> private_dispatch(Payload, Mode).

        private_dispatch(Payload, print) ->
            _ = erlang:put(private_recorded, {printed, Payload}),
            {printed, Payload};
        private_dispatch(Payload, get) ->
            _ = erlang:put(private_recorded, {read, Payload}),
            {read, Payload}.

        multi_route(Payload, Mode) -> multi_dispatch(Payload, Mode).

        multi_dispatch(Payload, print) ->
            _ = erlang:put(multi_recorded, {printed, Payload}),
            {printed, Payload};
        multi_dispatch(Payload, inspect) ->
            _ = erlang:put(multi_recorded, {inspected, Payload}),
            {inspected, Payload};
        multi_dispatch(Payload, get) ->
            _ = erlang:put(multi_recorded, {read, Payload}),
            {read, Payload}.

        finite_route(Payload) -> finite_dispatch(Payload, get).

        finite_dispatch(Payload, print) ->
            _ = erlang:put(finite_recorded, {printed, Payload}),
            {printed, Payload};
        finite_dispatch(Payload, get) ->
            _ = erlang:put(finite_recorded, {read, Payload}),
            {read, Payload}.

        checked({Payload}, print) -> dispatch(Payload, print);
        checked(Payload, get) when is_atom(Payload) -> dispatch(Payload, get).

        previous() -> erlang:get(recorded).

        route(Left, Right, Mode) when Mode =:= true -> pass(Left, Right, [selected]);
        route(Left, Right, Mode) when Mode =:= false -> pass(Left, Right, []).

        guarded(Left, Right, Mode) when Mode =:= true, is_atom(Left) -> pass(Left, Right, [selected]);
        guarded(Left, Right, Mode) when Mode =:= false -> pass(Left, Right, []).

        forward(Left, Right, Mode) when Mode =:= true -> pass(Left, Right, Mode);
        forward(Left, Right, Mode) when Mode =:= false -> pass(Left, Right, []).

        pass(Left, Right, Value) -> {Left, Right, Value}.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, "beam_closed_atom_pair.erl");
        CheckLayout(image, "dispatch", ["get", "print"]);
        CheckLayout(image, "private_dispatch", ["get", "print"]);
        CheckLayout(image, "multi_dispatch", ["get", "inspect", "print"]);
        CheckFiniteLayout(image);
        CheckGuardTailLayout(image);
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

            Term right = Term.I(19);
            Term yes = Term.A("true");
            Term no = Term.A("false");
            if (!Term.Tuple(payload, right, Term.List(Term.A("selected"))).Equals(await module.Invoke(
                context,
                "route",
                payload,
                right,
                yes
            )) || !Term.Tuple(payload, right, Nil.Value).Equals(await module.Invoke(
                context,
                "route",
                payload,
                right,
                no
            )) || !Term.Tuple(payload, right, yes).Equals(await module.Invoke(
                context,
                "forward",
                payload,
                right,
                yes
            )))
            {
                throw new InvalidOperationException(WrongValue);
            }

            await RejectGuardTail(
                "route",
                payload,
                right,
                Term.A("neither")
            );
            await RejectGuardTail(
                "guarded",
                Term.I(2),
                right,
                yes
            );
            if (!printed.Equals(await module.Invoke(
                context,
                "private_route",
                payload,
                Term.A("print")
            ))
                || !read.Equals(await module.Invoke(
                    context,
                    "private_route",
                    payload,
                    Term.A("get")
                ))
                || !read.Equals(await module.Invoke(context, "finite_route", payload)))
            {
                throw new InvalidOperationException(WrongValue);
            }

            Term inspected = Term.Tuple(Term.A("inspected"), payload);
            if (!inspected.Equals(await module.Invoke(
                context,
                "multi_route",
                payload,
                Term.A("inspect")
            )))
            {
                throw new InvalidOperationException(WrongValue);
            }

            await Reject("multi_route", payload, Term.A("neither"));
            await Reject("private_route", payload, Term.A("neither"));
            await Reject("dispatch", payload, Term.A("neither"));
            await Reject("checked", payload, Term.A("print"));
            await Reject("checked", Term.I(2), Term.A("get"));
            if (!read.Equals(await module.Invoke(context, "previous")))
            {
                throw new InvalidOperationException(WrongValue);
            }

            return Nil.Value;

            async Task RejectGuardTail(
                string function,
                Term input,
                Term other,
                Term selector
            )
            {
                try
                {
                    await module.Invoke(
                        context,
                        function,
                        input,
                        other,
                        selector
                    );
                }
                catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.FunctionClause))
                    && error.StackTraceTerm is Cons { Head: TupleTerm { Items: [_, Atom name, var arguments, _] } }
                    && name.Name == function && Cons.Items(arguments).SequenceEqual(new[] { input, other, selector }))
                {
                    return;
                }

                throw new InvalidOperationException(WrongValue);
            }

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

    private static void CheckLayout(byte[] image, string function, string[] expected)
    {
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions");
        int info = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Items(instruction, "Operands") is [_, var name, var arity]
                && atoms[Value(name)] == function && Value(arity) == 2
        );
        if (info < 0 || Opcode(instructions[info + 2]) != SelectValue)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        object[] operands = Items(instructions[info + 2], "Operands");
        if (operands.Length != 3 + 2 * expected.Length || Value(operands[0]) != 1)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        int previous = int.MaxValue;
        for (int index = 0; index < expected.Length; index++)
        {
            int key = 3 + 2 * index;
            int label = Value(operands[key + 1]);
            if (atoms[Value(operands[key])] != expected[index] || label >= previous || label <= Value(operands[1]))
            {
                throw new InvalidOperationException(WrongLayout);
            }

            previous = label;
        }
    }

    private static void CheckFiniteLayout(byte[] image)
    {
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions");
        if (instructions
            .SkipWhile(instruction => Opcode(instruction) != FunctionInfo
                || Items(instruction, "Operands") is not [_, var name, _]
                || atoms[Value(name)] != "finite_dispatch")
            .Skip(1)
            .TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
            .Any(instruction => Opcode(instruction) == SelectValue))
        {
            throw new InvalidOperationException(WrongLayout);
        }
    }

    private static void CheckGuardTailLayout(byte[] image)
    {
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        byte[] body = Body("route").Select(Opcode).ToArray();
        object selectorInstruction = Body("route")[1];
        if (body[1] != SelectValue || body.Contains(ExactEqualTest)
            || body.Contains(Allocate) || body.Contains(AllocateHeap)
            || body.Count(opcode => opcode == TailLocalCall) != 2
            || Items(selectorInstruction, "Operands") is not
                [var selector, var failure, _, var low, var lowLabel, var high, var highLabel]
            || Value(selector) != 2 || atoms[Value(low)] != "false" || atoms[Value(high)] != "true"
            || Value(lowLabel) <= Value(highLabel) || Value(failure) >= Value(highLabel)
            || Body("guarded").Any(instruction => Opcode(instruction) == SelectValue)
            || Body("forward").Any(instruction => Opcode(instruction) == SelectValue))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        object[] Body(string function) => Items(code, "Instructions")
            .SkipWhile(instruction => Opcode(instruction) != FunctionInfo
                || Items(instruction, "Operands") is not [_, var name, var arity]
                || atoms[Value(name)] != function || Value(arity) != 3)
            .Skip(1)
            .TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
            .ToArray();
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
