using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamOwnedRecordFrameSlotsChecks
{
    internal const string TestName = "compiler/beam/orders-overlapping-record-call-frame-slots";
    internal const string Fixture = "beam_owned_record_slots_probe.erl";
    private const string WrongSlots = "The record call frame has incorrect physical slots.";
    private const string WrongResult = "Reordering the record frame changed a call result or effect.";
    internal const string Source = """
        -module(beam_owned_record_slots_probe).
        -export([run/3, reordered/4, preserve/3, trap/3, seed/2]).
        -record(sock, {id, field}).

        run(Rec, Dest, Key) ->
            case fetch(Key) of
                {ok, Sock} ->
                    case erlang:put(Dest, Rec#sock{field = Sock}) of
                        {ok, Message} ->
                           case erlang:put(Sock, Message) of
                               ok -> ok;
                               {error, _} -> finish(Sock), erlang:erase(Sock)
                           end;
                        {error, _} -> finish(Sock), erlang:erase(Sock)
                    end;
                {error, _} -> ok
            end.

        reordered(Key, Rec, Dest, Extra) ->
            case fetch(Key) of
                {ok, Sock} ->
                    case erlang:put(Dest, Rec#sock{field = Sock}) of
                        {ok, Message} -> erlang:put(Sock, {Extra, Message});
                        {error, _} -> finish(Sock), erlang:erase(Sock)
                    end;
                {error, _} -> ok
            end.

        preserve(Rec, Dest, Key) ->
            case fetch(Key) of
                {ok, Sock} ->
                    case erlang:put(Dest, Rec#sock{field = Sock}) of
                        {ok, Message} -> erlang:put(Sock, {Rec, Message});
                        {error, _} -> erlang:erase(Sock)
                    end;
                {error, _} -> ok
            end.

        trap(Rec, Dest, Key) ->
            try
               run(Rec, Dest, Key)
            catch
               error:Reason -> {caught, Reason}
            end.

        fetch(Key) -> erlang:get(Key).
        finish(Key) -> erlang:get(Key).
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
            Term tag = Term.A("sock");
            Term id = Term.A("id");
            Term socket = Term.A("socket");
            Term key = Term.A("input");
            Term destination = Term.A("destination");
            Term record = Term.Tuple(tag, id, Term.A("unused"));
            Term ok = Term.A("ok");
            Term value = Term.A("message");
            Term accepted = Term.Tuple(ok, socket);
            Term supervised = Term.Tuple(ok, value);
            _ = await module.Invoke(
                context,
                "seed",
                key,
                accepted
            );
            _ = await module.Invoke(
                context,
                "seed",
                destination,
                supervised
            );
            _ = await module.Invoke(
                context,
                "seed",
                socket,
                ok
            );
            if (!ok.Equals(await module.Invoke(
                context,
                "run",
                record,
                destination,
                key
            ))
                || !value.Equals(await module.Invoke(
                    context,
                    "seed",
                    socket,
                    ok
                ))
                || !Term.Tuple(tag, id, socket).Equals(await module.Invoke(
                    context,
                    "seed",
                    destination,
                    supervised
                ))
                || !ok.Equals(await module.Invoke(
                    context,
                    "preserve",
                    record,
                    destination,
                    key
                ))
                || !Term.Tuple(record, value).Equals(await module.Invoke(
                    context,
                    "seed",
                    socket,
                    ok
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
        Check("run", 0, [(1, 1), (0, 2)]);
        Check("preserve", 0, [(1, 1), (0, 2)]);
        Check("reordered", 3, [(1, 0), (2, 1), (3, 2)]);

        void Check(string name, int initializedSlot, (int Arg, int Slot)[] expected)
        {
            int info = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
                && atoms[(int)Value(Operands(instruction)[1])] == name);
            object[] body = instructions.Skip(info + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
            int allocate = Array.FindIndex(body, instruction => Opcode(instruction) == Allocate);
            if (allocate < 0 || Opcode(body[allocate + 1]) != InitYRegisters
                || Value(Operands(body[allocate + 1])[1]) != initializedSlot)
            {
                throw new InvalidOperationException(WrongSlots);
            }

            for (int index = 0; index < expected.Length; index++)
            {
                object move = body[allocate + 2 + index];
                if (Opcode(move) != Move || Value(Operands(move)[0]) != expected[index].Arg
                    || Value(Operands(move)[1]) != expected[index].Slot)
                {
                    throw new InvalidOperationException(WrongSlots);
                }
            }
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static BigInteger Value(object operand) => (BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Children(instruction, "Operands").ToArray();

    private static IEnumerable<object> Children(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
