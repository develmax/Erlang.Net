using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamProvenListHeadInputChecks
{
    internal const string TestName = "compiler/beam/annotates-head-input-from-all-private-callers";
    private const string Fixture = "beam_proven_list_head_probe.erl";
    private const string MissingType = "The private head operand lacked its proven list type.";
    private const string InvalidControl = "An escaped or exported head operand acquired an unproven type.";
    private const string InvalidValue = "A head operand type changed the returned value or exception.";

    internal const string Source = """
        -module(beam_proven_list_head_probe).
        -export([start/4, open/3, escaping/0, escaped_entry/3]).

        start(empty, _, _, Stack) -> relay([], Stack, empty);
        start(Mode, First, Second, Stack) -> relay([First, Second], Stack, Mode).
        relay(List, Stack, Mode) -> closed(List, Stack, Mode).

        closed(List, Stack, Mode) ->
            Checked = begin [_|_] = Stack, Stack end,
            result(hd(List), Checked, Mode).

        open(List, Stack, Mode) ->
            Checked = begin [_|_] = Stack, Stack end,
            result(hd(List), Checked, Mode).

        escaping() -> fun escaped/3.
        escaped_entry(Value, Stack, Mode) -> escaped([Value], Stack, Mode).
        escaped(List, Stack, Mode) ->
            Checked = begin [_|_] = Stack, Stack end,
            result(hd(List), Checked, Mode).
        result(Head, Stack, Mode) -> {Head, Stack, Mode}.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        if (!HasListType("closed"))
        {
            throw new InvalidOperationException(MissingType);
        }

        if (HasListType("escaped") || HasListType("open"))
        {
            throw new InvalidOperationException(InvalidControl);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term mode = Term.A("ready");
            Term value = Term.A("first");
            Term stack = Term.List(Term.A("data"));
            Term expected = Term.Tuple(value, stack, mode);
            Term other = Term.A("second");
            if (!expected.Equals(await module.Invoke(
                context,
                "start",
                mode,
                value,
                other,
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
                    Term.A("empty"),
                    value,
                    other,
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

        bool HasListType(string name)
        {
            int info = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && Operands(instruction) is [_, var atom, _] && atoms[Value(atom)] == name
            );
            object head = instructions.Skip(info + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
                .Single(instruction => Opcode(instruction) == BifOne);

            object operand = Operands(head)[2];

            return operand.GetType().GetProperty("TypeIndex")!.GetValue(operand) is int index && index > 0;
        }
    }

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
