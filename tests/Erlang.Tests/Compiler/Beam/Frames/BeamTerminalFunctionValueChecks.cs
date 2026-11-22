using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTerminalFunctionValueChecks
{
    internal const string TestName = "compiler/beam/places-bound-function-value-calls-on-their-own-terminal-edges";
    private const string IncorrectLayout = "Only the selected function value call requires a zero-slot frame.";
    private const string IncorrectResult = "The selected callback, exact map key, or error term changed.";
    private const string Source = """
        -module(beam_terminal_function_value).
        -export([choose/3, read/3]).

        choose(Fun, Key, Branch) ->
            case Branch of
                run -> Fun(Key);
                keep -> Key
            end.

        read(Fun, Key, Map) ->
            case Map of
                #{Key := Found} -> {hit, Found};
                _ -> {miss, Fun(Key)}
            end.
        """;

    internal static async Task Run()
    {
        byte[] bytes = CodeGeneration.CompileBeamModule(Source, "beam_terminal_function_value.erl");
        object code = Code(bytes);
        string[] atoms = ((IEnumerable)Property(Property(code, "Structure"), "Atoms")).Cast<string>().ToArray();
        object[] instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();

        object[] choose = ReadFunction("choose");
        object[] read = ReadFunction("read");
        CheckBranchFrame(choose, SelectValue);
        CheckBranchFrame(read, IsMap);
        if (Opcode(read[1]) != GetMapElements || OperandValue(read[0], 0) != OperandValue(read[1], 0)
            || read.Any(operation => Opcode(operation) == CaseEnd))
        {
            throw new InvalidOperationException(IncorrectLayout);
        }

        var image = BeamModule.Prepare(bytes);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term key = Term.I(1);
            Term found = Term.A("found");
            Term invalid = Term.A("invalid");
            Term keep = Term.A("keep");
            Term run = Term.A("run");
            var good = new MapTerm([new(key, found)]);
            var empty = new MapTerm([]);
            int calls = 0;
            var callback = new FunctionTerm(1, (_, arguments) =>
            {
                calls++;

                return ValueTask.FromResult(arguments[0]);
            });
            var wrongArity = new FunctionTerm(0, (_, _) => ValueTask.FromResult(found));

            if (!key.Equals(await image.Invoke(
                context,
                "choose",
                invalid,
                key,
                keep
            ))
                || !key.Equals(await image.Invoke(
                    context,
                    "choose",
                    callback,
                    key,
                    run
                ))
                || !Term.Tuple(Term.A("hit"), found).Equals(await image.Invoke(
                    context,
                    "read",
                    invalid,
                    key,
                    good
                ))
                || calls != 1
                || !Term.Tuple(Term.A("miss"), key).Equals(await image.Invoke(
                    context,
                    "read",
                    callback,
                    key,
                    empty
                ))
                || calls != 2
                || !Term.Tuple(Term.A("miss"), key).Equals(await image.Invoke(
                    context,
                    "read",
                    callback,
                    key,
                    Nil.Value
                ))
                || calls != 3)
            {
                throw new InvalidOperationException(IncorrectResult);
            }

            await CheckFailure(invalid, Term.Tuple(Term.A("badfun"), invalid));
            await CheckFailure(wrongArity, Term.Tuple(Term.A("badarity"), Term.Tuple(wrongArity, Term.List(key))));

            return Nil.Value;

            async Task CheckFailure(Term function, Term reason)
            {
                try
                {
                    await image.Invoke(
                        context,
                        "read",
                        function,
                        key,
                        empty
                    );
                    throw new InvalidOperationException(IncorrectResult);
                }
                catch (ErlangException exception) when (reason.Equals(exception.Reason))
                {
                }
            }
        });

        object[] ReadFunction(string name)
        {
            int info = Array.FindIndex(instructions, operation => Opcode(operation) == FunctionInfo
                && atoms[(int)OperandValue(operation, 1)] == name);
            if (info < 0)
            {
                throw new InvalidOperationException(IncorrectLayout);
            }

            return instructions.Skip(info + 2).TakeWhile(operation => Opcode(operation) != FunctionInfo).ToArray();
        }
    }

    private static void CheckBranchFrame(object[] body, byte selector)
    {
        int call = Array.FindIndex(body, operation => Opcode(operation) == CallFun);
        object[] frames = body.Where(operation => Opcode(operation) is Allocate or AllocateHeap).ToArray();
        if (body.Length == 0 || Opcode(body[0]) != selector || call < 0 || frames is not [var frame]
            || OperandValue(frame, 0) != 0 || Array.IndexOf(body, frame) >= call)
        {
            throw new InvalidOperationException(IncorrectLayout);
        }
    }

    private static BigInteger OperandValue(object instruction, int position) =>
        (BigInteger)Property(((IEnumerable)Property(instruction, "Operands")).Cast<object>().ElementAt(position), "Value");

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");
}
