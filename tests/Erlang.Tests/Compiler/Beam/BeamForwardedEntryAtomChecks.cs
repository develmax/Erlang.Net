using System.Collections;
using System.Numerics;
using System.Reflection;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamForwardedEntryAtomChecks
{
    internal const string TestName = "compiler/beam/retains-atom-already-in-first-call-register";
    private const string Module = "beam_forwarded_entry_atom";
    private const string Fixture = Module + ".erl";
    private const string WrongAllocation = "The first atom argument did not retain its incoming register.";
    private const string WrongRematerialization = "The later or relocated atom lost its rematerialization.";

    internal const string Source = """
        -module(beam_forwarded_entry_atom).
        -export([run/0,later_run/1,moved_run/1,pass/1,relay/1,side/1,pick/2]).
        run() -> first(val).
        later_run(Input) -> later(Input, val).
        moved_run(Input) -> moved(val, Input).
        first(Atom) -> {var,atom_to_list(?MODULE:relay(?MODULE:pass(Atom)))}.
        later(Input, Atom) -> ?MODULE:side(Input), ?MODULE:pass(Atom).
        moved(Atom, Input) -> {var,?MODULE:pick(Input, Atom)}.
        pass(Value) -> Value.
        relay(Value) -> Value.
        side(Value) -> self() ! {side,Value}, Value.
        pick(First, Second) -> {First,Second}.
        """;

    internal static Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        var module = BeamModule.Prepare(image, new BeamDependencySelection([Module]));
        object code = typeof(BeamModule).GetField(BeamBinaryHostNames.CodeField, BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(module)!;
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] functions = Items(code, "Instructions");
        object[] first = ReadBody("first");
        object frame = first.Single(instruction => Opcode(instruction) == Allocate);
        if (Value(Items(frame, "Operands")[1]) != 1 || HasAtomMove(first))
        {
            throw new InvalidOperationException(WrongAllocation);
        }

        if (!HasAtomMove(ReadBody("later")) || !HasAtomMove(ReadBody("moved")))
        {
            throw new InvalidOperationException(WrongRematerialization);
        }

        return Task.CompletedTask;

        object[] ReadBody(string name)
        {
            int index = Array.FindIndex(functions, item => Opcode(item) == FunctionInfo
                && Items(item, "Operands") is [_, var function, _] && atoms[Value(function)] == name);

            return functions.Skip(index + 1).TakeWhile(item => Opcode(item) != FunctionInfo).ToArray();
        }

        bool HasAtomMove(IEnumerable<object> function) => function.Any(instruction => Opcode(instruction) == Move
            && Items(instruction, "Operands") is [var source, _] && Equals(Property(source, "Kind"), "atom")
            && atoms[Value(source)] == "val");
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
