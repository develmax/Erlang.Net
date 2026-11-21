using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamProvenConsHeadChecks
{
    internal const string TestName = "compiler/beam/projects-head-after-private-cons-proof";
    private const string Fixture = "beam_proven_cons_head_probe.erl";
    private const string MissingProjection = "The private function retained a fallible head despite the successful caller proof.";
    private const string LostCheck = "The independent stack check or a fallible head was removed.";
    private const string InvalidValue = "The head projection changed the returned value or error.";

    internal const string Source = """
        -module(beam_proven_cons_head_probe).
        -export([start/2, seeded/3, partial/3, open/2, escaping/0, escaped_entry/2, remote_input/2]).

        start(Input, Stack) -> forward(hd(Input), Input, Stack).
        forward(Head, Input, Stack) -> closed(Input, Stack, Head).
        seeded(H, Tail, Stack) -> closed([H|Tail], Stack, H).

        closed(Input, Stack, Head) ->
            Checked = begin [_|_] = Stack, Stack end,
            result(hd(Input), Checked, Head).

        partial(empty, _, Stack) -> uncertain([], Stack);
        partial(nonempty, H, Stack) -> uncertain([H], Stack).
        uncertain(Input, Stack) ->
            Checked = begin [_|_] = Stack, Stack end,
            result(hd(Input), Checked, fallback).

        open(Input, Stack) ->
            Checked = begin [_|_] = Stack, Stack end,
            result(hd(Input), Checked, open).

        escaping() -> fun escaped/2.
        escaped_entry(H, Stack) -> escaped([H], Stack).
        escaped(Input, Stack) ->
            Checked = begin [_|_] = Stack, Stack end,
            result(hd(Input), Checked, escaped).
        remote_input(Value, Stack) -> remote_closed(beam_proven_cons_foreign:hd(Value), Value, Stack).
        remote_closed(Other, Input, Stack) ->
            Checked = begin [_|_] = Stack, Stack end,
            result(hd(Input), Checked, Other).
        result(H, Stack, Actual) -> {H, Stack, Actual}.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        byte[] closed = Body("closed");
        if (!closed.Contains(GetHead) || closed.Contains(BifOne))
        {
            throw new InvalidOperationException(MissingProjection);
        }

        if (!closed.Contains(IsNonemptyList) || !closed.Contains(BadMatch)
            || !Body("uncertain").Contains(BifOne) || !Body("open").Contains(BifOne) || !Body("escaped").Contains(BifOne)
            || !Body("remote_closed").Contains(BifOne))
        {
            throw new InvalidOperationException(LostCheck);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term value = Term.A("first");
            Term nonList = Term.A("tail");
            Term stack = Term.List(Term.A("data"));
            Term expected = Term.Tuple(value, stack, value);
            if (!expected.Equals(await module.Invoke(
                context,
                "start",
                Term.List(value),
                stack
            ))
                || !expected.Equals(await module.Invoke(
                    context,
                    "seeded",
                    value,
                    nonList,
                    stack
                )))
            {
                throw new InvalidOperationException(InvalidValue);
            }

            try
            {
                _ = await module.Invoke(
                    context,
                    "start",
                    Nil.Value,
                    stack
                );
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                && error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(InvalidValue);
        });

        byte[] Body(string name)
        {
            int info = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && Operands(instruction) is [_, var atom, _] && atoms[Value(atom)] == name
            );

            return instructions.Skip(info + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).Select(Opcode).ToArray();
        }
    }

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
