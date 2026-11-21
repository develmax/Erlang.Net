using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamInitialEntryProjectionChecks
{
    internal const string TestName = "compiler/beam/project-first-call-argument-from-owned-entry";
    private const string MissingForward = "The first call copied a single-use field through the branch stack.";
    private const string LostBinding = "The early projection dropped a field that the continuation still needs.";
    private const string Source = """
        -module(beam_initial_entry_projection).
        -export([run/2]).
        -record(state,{first,second}).
        -record(part,{device,timer}).
        run(tick, State=#state{first=Part=#part{device=Device}}) ->
            service:emit(Device),
            Timer = service:timer(),
            {ok,State#state{first=Part#part{timer=Timer}}};
        run(keep, State=#state{second=Part=#part{device=Device}}) ->
            service:emit(Device),
            service:again(Device),
            {ok,State#state{second=Part#part{timer=none}}}.
        """;

    internal static void Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, "beam_initial_entry_projection.erl"));
        var instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        var allocations = instructions.Select((instruction, index) => (instruction, index))
            .Where(pair => Opcode(pair.instruction) == Allocate).Select(pair => pair.index).ToArray();
        if (allocations.Length != 2 || ReadFirstCallProjection(allocations[0]) is not ("x", 0))
        {
            throw new InvalidOperationException(MissingForward);
        }

        if (ReadFirstCallProjection(allocations[1]) is not ("y", _))
        {
            throw new InvalidOperationException(LostBinding);
        }

        (string Kind, int Value) ReadFirstCallProjection(int frame)
        {
            object getter = instructions.Skip(frame + 1).TakeWhile(instruction => Opcode(instruction) != ExternalCall)
                .Last(instruction => Opcode(instruction) == GetTupleElement);
            object destination = ((IEnumerable)Property(getter, "Operands")).Cast<object>().Last();

            return ((string)Property(destination, "Kind"), (int)(BigInteger)Property(destination, "Value"));
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");
}
