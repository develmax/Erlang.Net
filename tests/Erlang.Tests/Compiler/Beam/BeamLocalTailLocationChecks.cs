using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamLocalTailLocationChecks
{
    internal const string TestName = "compiler/beam/elides-source-lines-before-local-tail-calls";
    private const string Fixture = "beam_local_tail_locations.erl";
    private const string WrongLocation = "A call had the wrong source marker before its transfer.";

    private const string Source = """
        -module(beam_local_tail_locations).
        -export([direct/1,route/1,route_frame/2,body/1,guarded/1,external/1]).
        direct(Value) ->
            tail_target(Value).
        route(Value) ->
            Chosen =
                case observe(Value) of
                    yes -> ok;
                    Other -> {other, Other}
                end,
            tail_target(Chosen).
        route_frame(Value, Keep) ->
            Chosen =
                case observe(Value) of
                    yes -> ok;
                    Other -> {other, Other}
                end,
            tail_target({Chosen, Keep}).
        body(Value) ->
            Result = tail_target(Value),
            {kept, Result}.
        guarded(Value) ->
            try tail_target(Value) of Number -> {ok, Number}
            catch error:Reason:Stack -> {caught, Reason, Stack}
            end.
        external(Value) ->
            erlang:error(Value).
        observe(Value) -> erlang:put(tail_location_observed, Value), Value.
        tail_target(ok) -> ok;
        tail_target({ok, 2}) -> ok;
        tail_target(Value) ->
            erlang:error({tail_failure, Value}).
        """;

    internal static Task Run()
    {
        object code = Code(CodeGeneration.CompileBeamModule(Source, Fixture));
        string[] atoms = Children(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Children(code, "Instructions").ToArray();
        Check("direct", false, TailLocalCall);
        Check(
            "route",
            false,
            TailLocalCall,
            TailLocalCallWithFrame
        );
        Check(
            "route_frame",
            false,
            TailLocalCall,
            TailLocalCallWithFrame
        );
        Check("body", true, LocalCall);
        Check("guarded", true, LocalCall);
        Check(
            "external",
            true,
            TailExternalCall,
            TailExternalCallWithFrame
        );

        return Task.CompletedTask;

        void Check(string name, bool hasLine, params byte[] calls)
        {
            int entry = Array.FindIndex(
                instructions,
                instruction =>
                Opcode(instruction) == FunctionInfo
                && atoms[(int)(BigInteger)Property(Children(instruction, "Operands").ElementAt(1), "Value")] == name
            );
            if (entry < 0)
            {
                throw new InvalidOperationException(WrongLocation);
            }

            byte[] operations = instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
                .Select(Opcode).ToArray();
            int[] locations = operations.Select((opcode, index) => (opcode, index))
                .Where(item => calls.Contains(item.opcode))
                .Select(item => item.index).ToArray();
            if (locations.Length == 0 || locations.Any(index => (index > 0 && operations[index - 1] == Line) != hasLine))
            {
                throw new InvalidOperationException(WrongLocation);
            }
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Children(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
