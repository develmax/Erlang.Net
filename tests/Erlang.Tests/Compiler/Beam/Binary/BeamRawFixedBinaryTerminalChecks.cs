using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamRawFixedBinaryTerminalChecks
{
    internal const string TestName = "compiler/beam/keeps-proven-fixed-binary-fields-in-entry-registers";
    private const string WrongLayout = "The fixed binary entry did not preserve its checked or proven match layout.";
    private const string WrongResult = "A fixed binary entry returned the wrong value or throw payload.";
    private const string Source = """
        -module(raw_fixed_binary_terminal).
        -export([predicate/1, from_field/2, from_case/1, from_open/1, escaped/0, data/0, short/0, unaligned/0]).

        predicate(Bin) when is_binary(Bin) -> flag(Bin).
        flag(<<_:8, 1:1, _/bits>>) -> yes;
        flag(_) -> no.

        from_field(<<Field:2/binary, _/bits>>, Detail) -> parse(Field, Detail);
        from_field(Bin, Detail) when is_bitstring(Bin) -> parse(Bin, Detail).
        parse(<<Value:16/little>>, _) -> {fixed, Value};
        parse(_, Detail) -> throw({invalid, Detail}).

        from_case(Bin) ->
            Field = case Bin of
                <<Bytes:2/binary, _/bits>> -> Bytes;
                _ -> throw(short_source)
            end,
            parse_case(Field).
        parse_case(<<First:8, Second:8>>) -> {pair, First, Second};
        parse_case(_) -> throw(wrong_width).

        from_open(<<_:8, Rest/bits>>) -> open_suffix(Rest).
        open_suffix(<<First:8, Second:8>>) -> {pair, First, Second};
        open_suffix(_) -> invalid.

        escaped() -> fun shared/1.
        shared(<<First:8, Second:8>>) -> {pair, First, Second};
        shared(_) -> invalid.

        data() -> <<16#23, 16#81, 16#45>>.
        short() -> <<1>>.
        unaligned() -> <<1:1>>.
        """;

    internal static async Task Run()
    {
        byte[] bytes = CodeGeneration.CompileBeamModule(Source, "raw_fixed_binary_terminal.erl");
        object code = Code(bytes);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions");

        foreach (string name in new[] { "flag", "parse", "parse_case" })
        {
            object[] body = ReadBody(name);
            if (body.Length < 2 || Opcode(body[0]) != StartBinaryMatchKnown || Opcode(body[1]) != BinaryMatch
                || body.Any(instruction => Opcode(instruction) is Allocate or AllocateHeap))
            {
                throw new InvalidOperationException(WrongLayout);
            }
        }

        foreach (string name in new[] { "open_suffix", "shared" })
        {
            object first = ReadBody(name)[0];
            if (Opcode(first) == StartBinaryMatchKnown
                && Items(first, "Operands") is [var mode, ..] && atoms[Value(mode)] == "no_fail")
            {
                throw new InvalidOperationException(WrongLayout);
            }
        }

        var module = BeamModule.Prepare(bytes);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term data = await module.Invoke(context, "data");
            Term shortInput = await module.Invoke(context, "short");
            Term unaligned = await module.Invoke(context, "unaligned");
            Term detail = Term.Tuple(Term.A("detail"), Term.I(3));
            if (!Term.A("yes").Equals(await module.Invoke(context, "predicate", data))
                || !Term.A("no").Equals(await module.Invoke(context, "predicate", shortInput))
                || !Term.Tuple(Term.A("fixed"), Term.I(0x8123)).Equals(
                    await module.Invoke(
                        context,
                        "from_field",
                        data,
                        detail
                    ))
                || !Term.Tuple(Term.A("pair"), Term.I(0x23), Term.I(0x81)).Equals(
                    await module.Invoke(context, "from_case", data))
                || !Term.Tuple(Term.A("pair"), Term.I(0x81), Term.I(0x45)).Equals(
                    await module.Invoke(context, "from_open", data)))
            {
                throw new InvalidOperationException(WrongResult);
            }

            await ExpectThrow(
                "from_field",
                Term.Tuple(Term.A("invalid"), detail),
                shortInput,
                detail
            );
            await ExpectThrow(
                "from_field",
                Term.Tuple(Term.A("invalid"), detail),
                unaligned,
                detail
            );
            await ExpectThrow("from_case", Term.A("short_source"), shortInput);

            return Nil.Value;

            async Task ExpectThrow(
                string entry,
                Term reason,
                params Term[] arguments
            )
            {
                try
                {
                    await module.Invoke(
                        context,
                        entry,
                        arguments
                    );
                }
                catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Throw
                    && error.Reason.Equals(reason))
                {
                    return;
                }

                throw new InvalidOperationException(WrongResult);
            }
        });

        object[] ReadBody(string name)
        {
            int info = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && Items(instruction, "Operands") is [_, var function, _]
                    && atoms[Value(function)] == name
            );
            if (info < 0)
            {
                throw new InvalidOperationException(WrongLayout);
            }

            return instructions.Skip(info + 2).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
