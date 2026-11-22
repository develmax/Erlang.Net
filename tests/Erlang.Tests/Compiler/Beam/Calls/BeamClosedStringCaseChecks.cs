using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamClosedStringCaseChecks
{
    internal const string TestName = "compiler/beam/partitions-closed-string-case-by-first-cons";
    private const string Fixture = "beam_closed_string_case.erl";
    private const string WrongLayout = "Expected a first-cons selector only for distinct closed string keys.";
    private const string WrongValue = "Closed string selection changed an accepted arm or its original fallback value.";

    private const string Source = """
        -module(beam_closed_string_case).
        -export([route/2, overlap/2]).

        route(Key, Carry) ->
            case Key of
                "location" -> {location, Key, Carry};
                "status" -> {status, Key, Carry};
                _ -> {fallback, Key, Carry}
            end.

        overlap(Key, Carry) ->
            case Key of
                "location" -> {location, Key, Carry};
                [$l | _] -> {prefix, Key, Carry};
                "status" -> {status, Key, Carry};
                _ -> {fallback, Key, Carry}
            end.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions");
        object[] body = ReadBody("route").ToArray();
        object? selection = body.SingleOrDefault(instruction => Opcode(instruction) == SelectValue);
        if (selection is null
            || Items(selection, "Operands") is not
                [var register, var failure, _, var firstKey, var firstEntry, var secondKey, var secondEntry]
            || Value(register) != 1 || Value(firstKey) != 'l' || Value(secondKey) != 's'
            || LabelPosition(secondEntry) < 0 || LabelPosition(firstEntry) <= LabelPosition(secondEntry)
            || body.Count(instruction => Opcode(instruction) == GetList) != 1
            || body.Count(instruction => Opcode(instruction) == ExactEqualTest
                && Value(Items(instruction, "Operands")[0]) == Value(failure)) != 2
            || ReadBody("overlap").Any(instruction => Opcode(instruction) == SelectValue))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term carry = Term.A("carry");
            Term status = Term.String("status");
            Term location = Term.String("location");
            Term badSuffix = Term.String("local");
            Term improper = new Cons(Term.I('s'), Term.A("suffix"));
            foreach (var (entry, tag, key) in new (string, string, Term)[]
                {
                    ("route", "status", status),
                    ("route", "location", location),
                    ("route", "fallback", badSuffix),
                    ("route", "fallback", improper),
                    ("route", "fallback", Nil.Value),
                    ("route", "fallback", Term.A("status")),
                    ("overlap", "location", location),
                    ("overlap", "prefix", badSuffix),
                    ("overlap", "status", status)
                })
            {
                Term expected = Term.Tuple(Term.A(tag), key, carry);
                if (!expected.Equals(await module.Invoke(
                    context,
                    entry,
                    key,
                    carry
                )))
                {
                    throw new InvalidOperationException(WrongValue);
                }
            }

            return Nil.Value;
        });

        int LabelPosition(object entry) => Array.FindIndex(
            body,
            instruction => Opcode(instruction) == Label && Value(Items(instruction, "Operands")[0]) == Value(entry)
        );

        IEnumerable<object> ReadBody(string function)
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && Items(instruction, "Operands") is [_, var name, var arity]
                    && atoms[Value(name)] == function && Value(arity) == 2
            );
            if (start < 0)
            {
                throw new InvalidOperationException(WrongLayout);
            }

            return instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo);
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
