using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamCallConstructionProbe;

namespace Erlang.Tests;

internal static class BeamSingletonListEntryFrameChecks
{
    internal const string TestName = "compiler/beam/projects-a-shared-cons-before-its-singleton-and-recursive-branches";
    private const string Failure = "The shared cons prefix changed the rejection edge or allocated a frame in the singleton leaf.";

    internal static Task Run()
    {
        const string source = """
            -module(singleton_list_entry_frame).
            -export([replace/2, guarded/2]).
            replace([_], Value) -> [Value];
            replace([Head | Tail], Value) -> [Head | replace(Tail, Value)].
            guarded([Head], Value) when is_atom(Head) -> [Value];
            guarded([Head | Tail], Value) -> [Head | guarded(Tail, Value)].
            """;
        var code = new BeamCallConstructionProbe(CodeGeneration.CompileBeamModule(source, "singleton_list_entry_frame.erl"));
        object[] body = WithoutLines(code.Body("replace"));
        int check = Array.FindIndex(body, instruction => Opcode(instruction) == IsNonemptyList);
        int split = Array.FindIndex(body, instruction => Opcode(instruction) == GetList);
        int nil = Array.FindIndex(body, instruction => Opcode(instruction) == IsNil);
        int singletonReturn = Array.FindIndex(body, instruction => Opcode(instruction) == Return);
        int allocate = Array.FindIndex(body, instruction => Opcode(instruction) is Allocate or AllocateHeap);
        int recursion = Array.FindIndex(body, instruction => Opcode(instruction) == LocalCall);
        if (check < 0 || split <= check || nil <= split || singletonReturn <= nil
            || allocate <= singletonReturn || recursion <= allocate || Value(Operand(body[allocate], 0)) != 1
            || !IsX(Operand(body[check], 1), 0) || !IsX(Operand(body[split], 0), 0)
            || !IsX(Operand(body[split], 1), 2) || !IsX(Operand(body[split], 2), 0)
            || !IsX(Operand(body[nil], 1), 0) || FindLabel(body, Value(Operand(body[check], 0))) >= 0
            || FindLabel(body, Value(Operand(body[nil], 0))) <= singletonReturn
            || body.Skip(nil + 1).Take(recursion - nil - 1).Any(instruction => Opcode(instruction) == IsNonemptyList)
            || !code.Body("guarded").Any(instruction => Opcode(instruction) == IsAtom))
        {
            throw new InvalidOperationException(Failure);
        }

        return Task.CompletedTask;
    }

    private static int FindLabel(object[] body, int label) => Array.FindIndex(body, instruction =>
        Opcode(instruction) == Label && Value(Operand(instruction, 0)) == label);
}
