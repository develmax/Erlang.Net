using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamBoundConstructorConstantChecks
{
    internal const string TestName = "compiler/beam/substitutes-closed-arguments-through-constructor-bindings";
    private const string Module = "beam_bound_constructor_constant";
    private const string ShapeError = "A bound constructor did not retain its proven atom and singleton-list facts.";

    private const string Source = """
        -module(beam_bound_constructor_constant).
        -export([known/3, first/3, second/3, escaped/0, direct/3]).
        known(A,B,C) -> child(worker,A,B,C).
        first(A,B,C) -> mixed(worker,A,B,C).
        second(A,B,C) -> mixed(alternate,A,B,C).
        escaped() -> fun through_fun/4.
        direct(A,B,C) -> through_fun(worker,A,B,C).

        child(Module,A,B,C) ->
            Id = {Module,A,B,C},
            Start = {Module,start_link,[A,B,C]},
            Modules = [Module],
            {Id,Start,permanent,5000,worker,Modules}.
        mixed(Module,A,B,C) ->
            Id = {Module,A,B,C},
            Start = {Module,start_link,[A,B,C]},
            Modules = [Module],
            {Id,Start,permanent,5000,worker,Modules}.
        through_fun(Module,A,B,C) ->
            Id = {Module,A,B,C},
            Start = {Module,start_link,[A,B,C]},
            Modules = [Module],
            {Id,Start,permanent,5000,worker,Modules}.
        """;

    internal static void Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Module + ".erl"));
        object[] instructions = Children(code, "Instructions");
        string[] atoms = Children(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        if (!Reserves("child", 22) || !Reserves("mixed", 24) || !Reserves("through_fun", 24))
        {
            throw new InvalidOperationException(ShapeError);
        }

        bool Reserves(string name, int words)
        {
            int start = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Children(instruction, "Operands")[1])] == name);

            return start >= 0 && instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
                .Any(instruction => Opcode(instruction) == TestHeap
                    && Children(instruction, "Operands").Select(Value).SequenceEqual([words, 4]));
        }
    }

    private static object[] Children(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");
}
