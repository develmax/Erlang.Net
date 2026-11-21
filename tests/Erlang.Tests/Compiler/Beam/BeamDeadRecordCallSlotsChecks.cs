using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamDeadRecordCallSlotsChecks
{
    internal const string TestName = "compiler/beam/clears-dead-record-arguments-after-call-setup";
    private const string Fixture = "beam_dead_record_call_probe.erl";
    private const string WrongSlots = "The record call cleared the wrong frame slots.";
    private const string WrongResult = "Clearing a record operand changed the returned value.";
    private const string Source = """
        -module(beam_dead_record_call_probe).
        -export([discarded/3, retained/3, trapped/3, seed/2]).
        -record(sock, {id, socket = none}).

        discarded(Rec, Dest, Key) ->
            case fetch(Key) of
                {ok, Sock} ->
                    Prev = erlang:put(Dest, Rec#sock{socket = Sock}),
                    {Prev, Sock};
                _ -> skipped
            end.

        retained(Rec, Dest, Key) ->
            case fetch(Key) of
                {ok, Sock} ->
                    Prev = erlang:put(Dest, Rec#sock{socket = Sock}),
                    {Prev, Rec, Sock};
                _ -> skipped
            end.

        trapped(Rec, Dest, Key) ->
            try
                case fetch(Key) of
                    {ok, Sock} ->
                        Prev = erlang:put(Dest, Rec#sock{socket = Sock}),
                        {Prev, Sock};
                    _ -> skipped
                end
            catch
                error:Reason -> {caught, Reason}
            end.

        fetch(Key) -> erlang:get(Key).
        seed(Key, Value) -> erlang:put(Key, Value).
        """;

    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, Fixture);
        CheckLayout(beam);
        var module = BeamModule.Prepare(beam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term recordTag = Term.A("sock");
            Term pair = Term.Tuple(recordTag, Term.A("id"), Term.A("none"));
            Term accepted = Term.A("accepted");
            Term missing = Term.A("missing");
            Term dest = Term.A("dest");
            Term key = Term.A("key");
            Term updated = Term.Tuple(recordTag, Term.A("id"), accepted);
            _ = await module.Invoke(
                context,
                "seed",
                key,
                Term.Tuple(Term.A("ok"), accepted)
            );
            _ = await module.Invoke(
                context,
                "seed",
                dest,
                Term.A("old")
            );
            if (!Term.Tuple(Term.A("old"), accepted).Equals(await module.Invoke(
                context,
                "discarded",
                pair,
                dest,
                key
            ))
                || !Term.Tuple(updated, pair, accepted).Equals(await module.Invoke(
                    context,
                    "retained",
                    pair,
                    dest,
                    key
                ))
                || !Term.A("skipped").Equals(await module.Invoke(
                    context,
                    "discarded",
                    pair,
                    dest,
                    missing
                )))
            {
                throw new InvalidOperationException(WrongResult);
            }

            return Nil.Value;
        });
    }

    private static void CheckLayout(byte[] beam)
    {
        object code = Code(beam);
        object[] instructions = Children(code, "Instructions").ToArray();
        string[] atoms = Children(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        Check("discarded", clearRecord: true, clearTarget: true);
        Check("retained", clearRecord: false, clearTarget: true);
        Check("trapped", clearRecord: false, clearTarget: false);

        void Check(string name, bool clearRecord, bool clearTarget)
        {
            int info = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
                && atoms[(int)Value(Operands(instruction)[1])] == name);
            object[] body = instructions.Skip(info + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
            int update = Array.FindIndex(body, instruction => Opcode(instruction) == UpdateRecord);
            if (update < 0 || Opcode(body[update + 1]) != Move)
            {
                throw new InvalidOperationException(WrongSlots);
            }

            BigInteger record = Value(Operands(body[update])[2]);
            BigInteger target = Value(Operands(body[update + 1])[0]);
            int call = Array.FindIndex(body, update + 2, instruction => Opcode(instruction) == ExternalCall);
            if (call < 0)
            {
                throw new InvalidOperationException(WrongSlots);
            }

            var cleared = body.Skip(update + 2).Take(call - update - 2)
                .Where(instruction => Opcode(instruction) == InitYRegisters)
                .SelectMany(instruction => Operands(instruction).Skip(1)).Select(Value).ToHashSet();
            if (cleared.Contains(record) != clearRecord || cleared.Contains(target) != clearTarget)
            {
                throw new InvalidOperationException(WrongSlots);
            }
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static BigInteger Value(object operand) => (BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Children(instruction, "Operands").ToArray();

    private static IEnumerable<object> Children(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
