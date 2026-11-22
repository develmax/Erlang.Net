using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamMixedPatternMaskTailChecks
{
    internal const string TestName = "compiler/beam/keeps-disjoint-map-and-tuple-guard-tails-frameless";
    private const string WrongLayout = "The mixed pattern entry did not share its projections and keep the guard roots in X.";
    private const string WrongValue = "The mixed pattern entry changed a selected result or a function clause failure.";
    private const string Source = """
        -module(beam_mixed_pattern_mask_tail).
        -export([run/4]).
        run(S, {A,B,C} = Flat, Port, Data)
          when ((A bor B bor C) band (bnot 15)) =:= 0, (Port band (bnot 63)) =:= 0 ->
            forward(S, {Flat, Port}, [], Data);
        run(S, #{group := {A,B,C}, code := Port} = Whole, Extra, Data)
          when ((A bor B bor C) band (bnot 15)) =:= 0, (Port band (bnot 63)) =:= 0, is_list(Extra) ->
            forward(S, Whole, Extra, Data);
        run(S, {{A,B,C}, Port} = Whole, Extra, Data)
          when ((A bor B bor C) band (bnot 15)) =:= 0, (Port band (bnot 63)) =:= 0, is_list(Extra) ->
            forward(S, Whole, Extra, Data);
        run(S, {wrapped, {{A,B,C}, Port}} = Whole, Extra, Data)
          when ((A bor B bor C) band (bnot 15)) =:= 0, (Port band (bnot 63)) =:= 0, is_list(Extra) ->
            forward(S, Whole, Extra, Data);
        run(S, {wrapped, {local_key, Port}} = Whole, Extra, Data)
          when (Port band (bnot 63)) =:= 0, is_list(Extra) ->
            forward(S, Whole, Extra, Data).
        forward(A, B, C, D) -> {A,B,C,D}.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, "beam_mixed_pattern_mask_tail.erl");
        object code = Code(image);
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int info = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
            && atoms[Value(Operands(instruction)[1])] == "run");
        if (info < 0)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        object[] body = instructions.Skip(info + 1).TakeWhile(operation => Opcode(operation) != FunctionInfo).ToArray();
        if (body.Any(operation => Opcode(operation) is Allocate or AllocateHeap)
            || body.Count(operation => Opcode(operation) == SelectTupleArity) != 1
            || body.Count(operation => Opcode(operation) == GetMapElements) != 1
            || !body.Where(operation => Opcode(operation) == GcBifTwo)
                .Select(operation => Value(Operands(operation)[1]))
                .SequenceEqual([6, 6, 5, 4, 8, 7, 6, 6, 8, 7, 6, 6, 6, 8, 7, 6, 6]))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term socket = Term.A("input");
            Term data = Term.A("data");
            Term tag = Term.A("wrapped");
            Term bad = Term.A("bad");
            Term port = Term.I(62);
            Term members = Term.Tuple(Term.I(1), Term.I(4), Term.I(9));
            Term pair = Term.Tuple(members, port);
            Term extras = Term.List(Term.A("extra"));
            Term map = new MapTerm([new(Term.A("group"), members), new(Term.A("code"), port)]);
            await Accept(
                members,
                port,
                pair,
                Nil.Value
            );
            await Accept(
                map,
                extras,
                map,
                extras
            );
            await Accept(
                pair,
                extras,
                pair,
                extras
            );
            Term wrapped = Term.Tuple(tag, pair);
            await Accept(
                wrapped,
                extras,
                wrapped,
                extras
            );
            Term wrappedAtom = Term.Tuple(tag, Term.Tuple(Term.A("local_key"), port));
            await Accept(
                wrappedAtom,
                extras,
                wrappedAtom,
                extras
            );

            await Reject(Term.Tuple(Term.I(1), bad, Term.I(4)), port);
            await Reject(Term.Tuple(Term.I(1), Term.I(16), Term.I(4)), port);
            await Reject(members, Term.I(-1));
            await Reject(Term.Tuple(members, bad), extras);
            await Reject(pair, bad);
            await Reject(Term.Tuple(tag, Term.Tuple(bad, port)), extras);
            await Reject(new MapTerm([new(Term.A("group"), members)]), extras);
            await Reject(Term.Tuple(Term.A("other"), pair), extras);

            return Nil.Value;

            async Task Accept(
                Term input,
                Term ancillary,
                Term forwarded,
                Term forwardedAncillary
            )
            {
                Term expected = Term.Tuple(
                    socket,
                    forwarded,
                    forwardedAncillary,
                    data
                );
                if (!expected.Equals(await Invoke(input, ancillary)))
                {
                    throw new InvalidOperationException(WrongValue);
                }
            }

            async Task Reject(Term input, Term ancillary)
            {
                try
                {
                    _ = await Invoke(input, ancillary);
                }
                catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.FunctionClause)))
                {
                    return;
                }

                throw new InvalidOperationException(WrongValue);
            }

            Task<Term> Invoke(Term input, Term ancillary) => module.Invoke(
                context,
                "run",
                socket,
                input,
                ancillary,
                data
            ).AsTask();
        });
    }

    private static object[] Items(object value, string name) =>
        ((IEnumerable)Property(value, name)).Cast<object>().ToArray();

    private static object[] Operands(object value) => Items(value, "Operands");

    private static byte Opcode(object operation) => (byte)Property(Property(operation, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");
}
