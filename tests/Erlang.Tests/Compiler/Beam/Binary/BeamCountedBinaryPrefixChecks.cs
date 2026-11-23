using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCountedBinaryPrefixChecks
{
    internal const string TestName = "compiler/beam/shares-position-in-counted-binary-prefix";
    private const string WrongSchedule = "The counted binary prefix changed position or frame ownership.";
    private const string WrongResult = "The counted binary prefix changed its result.";
    private const string Source = """
        -module(beam_counted_binary_prefix).
        -export([guarded/1, try_guarded/1, unproven/2]).

        guarded(Binary) when is_binary(Binary) -> scan(Binary, 0).

        try_guarded(Binary) when is_binary(Binary) -> guarded_wrapper(Binary).

        guarded_wrapper(Binary) ->
            try throwing(Binary, 0) of
                Count -> Count
            catch
                _:_ -> invalid
            end.

        scan(<<>>, Count) -> Count;
        scan(<<16#aa, _/binary>>, Count) -> Count;
        scan(<<_, Rest/binary>>, Count) -> scan(Rest, Count + 1).

        throwing(<<>>, Count) -> Count;
        throwing(<<16#aa, _Rest/binary>>, _Count) -> throw(seen);
        throwing(<<_, Rest/binary>>, Count) -> throwing(Rest, Count + 1).

        unproven(<<>>, Count) -> Count;
        unproven(<<16#aa, _/binary>>, Count) -> Count;
        unproven(<<_, Rest/binary>>, Count) -> unproven(Rest, Count + 1).
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, "beam_counted_binary_prefix.erl");
        CheckLayout(image);
        BeamModule module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            var empty = new BitString([]);
            var stopped = new BitString([1, 2, 0xaa, 3]);
            var ordinary = new BitString([1, 2, 3]);
            if (!Term.I(0).Equals(await module.Invoke(context, "guarded", empty))
                || !Term.I(2).Equals(await module.Invoke(context, "guarded", stopped))
                || !Term.I(3).Equals(await module.Invoke(context, "guarded", ordinary))
                || !Term.A("invalid").Equals(await module.Invoke(context, "try_guarded", stopped))
                || !Term.I(3).Equals(await module.Invoke(context, "try_guarded", ordinary))
                || !Term.I(-3).Equals(await module.Invoke(
                    context,
                    "unproven",
                    stopped,
                    Term.I(-5)
                )))
            {
                throw new InvalidOperationException(WrongResult);
            }

            return Nil.Value;
        });
    }

    private static void CheckLayout(byte[] image)
    {
        object code = Code(image);
        object[] instructions = Items(code, "Instructions");
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        RequirePositioned("scan");
        RequirePositioned("throwing");
        if (Body("unproven").Contains(StartBinaryMatchKnown))
        {
            throw new InvalidOperationException(WrongSchedule);
        }

        void RequirePositioned(string function)
        {
            byte[] body = Body(function);
            if (body[0] != StartBinaryMatchKnown
                || body.Count(opcode => opcode == StartBinaryMatchKnown) != 1
                || body.Count(opcode => opcode == GetBinaryPosition) != 1
                || body.Count(opcode => opcode == SetBinaryPosition) != 1
                || body.Count(opcode => opcode == TailLocalCall) != 1
                || body.Any(opcode => opcode is StartBinaryMatch or Allocate or AllocateHeap or Deallocate))
            {
                throw new InvalidOperationException(WrongSchedule);
            }
        }

        byte[] Body(string function)
        {
            int entry = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[(int)(BigInteger)Property(Items(instruction, "Operands")[1], "Value")] == function
            );
            if (entry < 0)
            {
                throw new InvalidOperationException(WrongSchedule);
            }

            return instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
                .Select(Opcode).Where(opcode => opcode is not (Label or Line)).ToArray();
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
