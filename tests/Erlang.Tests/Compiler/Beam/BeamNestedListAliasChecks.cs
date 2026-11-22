using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamNestedListAliasChecks
{
    internal const string TestName = "compiler/beam/lifts-nested-tuple-alias-list-generators";

    internal static async Task Run()
    {
        const string source = """
            -module(beam_nested_list_alias_probe).
            -export([collect/1, select/1, filtered/1]).
            collect(Rows) ->
                [{Name, Info, Key} || {Key, Info = {_, {Name, _}}} <- Rows].
            select(Rows) ->
                [Whole || {{_, _} = Whole, _} <- Rows].
            filtered(Rows) ->
                [{Name, Info, Key} || {Key, Info = {_, {Name, _}}} <- Rows, Key =/= skip].
            """;
        byte[] image = CodeGeneration.CompileBeamModule(source, "beam_nested_list_alias_probe.erl");
        object code = Code(image);
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        if (!CallsHelper("collect") || !CallsHelper("select") || FindFunction(Helper("filtered")) >= 0)
        {
            throw new InvalidOperationException("Expected helpers for the two unfiltered nested alias generators.");
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term first = Term.A("first");
            Term second = Term.A("second");
            Term skip = Term.A("skip");
            Term left = Term.Tuple(second, Term.Tuple(first, second));
            Term right = Term.Tuple(first, Term.Tuple(second, first));
            Term rows = Term.List(
                Term.Tuple(first, left),
                Term.Tuple(skip, right),
                Term.Tuple(second, Term.Tuple(first, second)),
                Term.Tuple(second, right),
                first
            );
            Term selected = Term.List(
                Term.Tuple(first, left, first),
                Term.Tuple(second, right, skip),
                Term.Tuple(second, right, second)
            );
            Term accepted = Term.List(Term.Tuple(first, left, first), Term.Tuple(second, right, second));
            Term originals = Term.List(left, right);
            Term pairs = Term.List(Term.Tuple(left, first), second, Term.Tuple(right, second));
            if (!selected.Equals(await module.Invoke(context, "collect", [rows]))
                || !accepted.Equals(await module.Invoke(context, "filtered", [rows]))
                || !originals.Equals(await module.Invoke(context, "select", [pairs]))
                || !Nil.Value.Equals(await module.Invoke(context, "collect", [Nil.Value])))
            {
                throw new InvalidOperationException("Nested alias list generators changed matching, order, or retained values.");
            }

            return Nil.Value;
        });

        bool CallsHelper(string name)
        {
            int parent = FindFunction(name);
            int helper = FindFunction(Helper(name));
            if (parent < 0 || helper < 0 || Opcode(instructions[helper + 1]) != Label
                || Value(Operands(instructions[helper])[2]) != 1)
            {
                return false;
            }

            int entry = Value(Operands(instructions[helper + 1])[0]);

            return instructions.Skip(parent + 1).TakeWhile(operation => Opcode(operation) != FunctionInfo)
                .Any(operation => Opcode(operation) is LocalCall or TailLocalCall or TailLocalCallWithFrame
                    && Value(Operands(operation)[1]) == entry);
        }

        int FindFunction(string name) => Array.FindIndex(
            instructions,
            instruction =>
            Opcode(instruction) == FunctionInfo
            && Operands(instruction) is [_, var atom, _] && atoms[Value(atom)] == name
        );
    }

    private static string Helper(string owner) => $"-{owner}/1-lc$^0/1-0-";

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
