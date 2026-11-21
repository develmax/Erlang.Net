using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCheckedRecordCallFrameChecks
{
    internal const string TestName = "compiler/beam/localizes-a-checked-record-frame-to-its-call";
    private const string Fixture = "beam_call_owned_record_frame_probe.erl";
    private const string WrongFrame = "A checked record branch used the wrong frame or heap roots.";
    private const string WrongResult = "Moving the call frame changed the record result or an arithmetic error.";

    private const string Source = """
        -module(beam_call_owned_record_frame_probe).
        -export([split/1, joined/1, caught/1, late_check/1]).
        -record(row, {n, p, l, keep}).

        split(#row{n=N,p=P0,l=L}=R) ->
            P = P0 - 1,
            if
                P < 1 ->
                    L1 = L - 1,
                    M = helper(L1),
                    New = R#row{n=N - 1,p=M,l=L1},
                    {New,[N],[M]};
                true ->
                    New = R#row{n=N - 1,p=P},
                    {New,[N],[P]}
            end;
        split(Old) ->
            split(convert(Old)).

        convert({row,N,P,L}) -> #row{n=N,p=P,l=L}.

        joined(#row{n=N,p=P0,l=L}=R) ->
            P = P0 - 1,
            M = if P < 1 -> helper(L - 1); true -> P end,
            New = R#row{n=N - 1,p=M},
            {New,[N],[M]};
        joined(Bad) ->
            error({badrecord,Bad}).

        caught(#row{n=N,p=P0,l=L}=R) ->
            try P0 - 1 of
                P when P < 1 ->
                    L1 = L - 1,
                    M = helper(L1),
                    {R#row{n=N - 1,p=M,l=L1},[N],[M]};
                P ->
                    {R#row{n=N - 1,p=P},[N],[P]}
            catch
                error:badarith -> caught
            end;
        caught(Bad) ->
            error({badrecord,Bad}).

        late_check({A,B}) when B =:= 32 ->
            helper(A);
        late_check({A,B}) ->
            Shift = A bsl B,
            Shift bor helper(B);
        late_check(Bad) ->
            error({badrecord,Bad}).

        helper(N) -> 1 bsl N.
        """;

    private const string ApplyFixture = "beam_checked_record_apply.erl";
    private const string WrongApply = "A checked record apply changed the argument order, failure payload, or owned frame.";
    private const string ApplySource = """
        -module(beam_checked_record_apply).
        -compile({no_auto_import,[apply/3]}).
        -export([run/2,empty/1,improper/2,shadowed/2,aliased/2]).
        -record(outer,{receiver}).
        -record(inner,{module}).

        run(Input, Value) ->
            Receiver = Input#outer.receiver,
            erlang:apply(Receiver#inner.module, min, [Receiver | [Value]]).

        empty(Input) ->
            Rest = [],
            Receiver = Input#outer.receiver,
            erlang:apply(Receiver#inner.module, is_tuple, [Receiver | Rest]).

        improper(Input, Rest) ->
            Receiver = Input#outer.receiver,
            erlang:apply(Receiver#inner.module, min, [Receiver | Rest]).

        shadowed(Input, Value) ->
            Receiver = Input#outer.receiver,
            apply(Receiver#inner.module, min, [Receiver | [Value]]).

        aliased(Input, Value) ->
            Rest = [Value],
            Receiver = Input#outer.receiver,
            erlang:apply(Receiver#inner.module, min, [Receiver | Rest]).

        apply(_, _, L) -> L.
        """;

    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, Fixture);
        CheckInstructions(beam);
        var module = BeamModule.Prepare(beam);
        byte[] applyBeam = CodeGeneration.CompileBeamModule(ApplySource, ApplyFixture);
        CheckCheckedApplyInstructions(applyBeam);
        var applyModule = BeamModule.Prepare(applyBeam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term receiver = Term.Tuple(Term.A("inner"), Term.A(ErlangModuleNames.Module));
            Term request = Term.Tuple(Term.A("outer"), receiver);
            Term marker = Term.I(7);
            if (!marker.Equals(await applyModule.Invoke(
                context,
                "run",
                request,
                marker
            ))
                || !Term.A("true").Equals(await applyModule.Invoke(context, "empty", request)))
            {
                throw new InvalidOperationException(WrongApply);
            }

            Term malformed = Term.A("malformed");
            await CheckRecordError(malformed, malformed);
            await CheckRecordError(Term.Tuple(Term.A("outer"), malformed), malformed);

            async Task CheckRecordError(Term input, Term expected)
            {
                try
                {
                    _ = await applyModule.Invoke(
                        context,
                        "run",
                        input,
                        marker
                    );
                }
                catch (ErlangException failure) when (failure.Reason.Equals(Term.Tuple(Term.A("badrecord"), expected)))
                {
                    return;
                }

                throw new InvalidOperationException(WrongApply);
            }

            Term row = Term.A("row");
            Term bad = Term.A("bad");
            Term keep = Term.A("keep");
            Term regular = Term.Tuple(
                row,
                Term.I(5),
                Term.I(4),
                bad,
                keep
            );
            Term merging = Term.Tuple(
                row,
                Term.I(5),
                Term.I(1),
                Term.I(2),
                keep
            );
            Term invalid = Term.Tuple(
                row,
                Term.I(5),
                bad,
                Term.I(2),
                keep
            );
            Term usual = Term.Tuple(Term.Tuple(
                row,
                Term.I(4),
                Term.I(3),
                bad,
                keep
            ), Term.List(Term.I(5)), Term.List(Term.I(3)));
            Term merged = Term.Tuple(Term.Tuple(
                row,
                Term.I(4),
                Term.I(2),
                Term.I(1),
                keep
            ), Term.List(Term.I(5)), Term.List(Term.I(2)));
            if (!usual.Equals(await module.Invoke(context, "split", regular))
                || !merged.Equals(await module.Invoke(context, "split", merging)))
            {
                throw new InvalidOperationException(WrongResult);
            }

            try
            {
                _ = await module.Invoke(context, "split", invalid);
            }
            catch (ErlangException failure) when (failure.Reason.Equals(Term.A("badarith")))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(WrongResult);
        });
    }

    private static void CheckCheckedApplyInstructions(byte[] beam)
    {
        object code = Code(beam);
        object[] instructions = Children(code, "Instructions").ToArray();
        string[] atoms = Children(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        foreach (var (name, arity) in new[] { ("run", 2), ("empty", 1) })
        {
            object[] body = Body(name).Where(instruction => Opcode(instruction) is not (Label or Line)).ToArray();
            byte[] prefix =
            [
                IsTaggedTuple, GetTupleElement, IsTaggedTuple, Allocate, GetTupleElement, Move,
                TailDynamicApply, BadRecord, BadRecord
            ];
            if (!body.Take(prefix.Length).Select(Opcode).SequenceEqual(prefix)
                || !Operands(body[3]).Select(Value).SequenceEqual([BigInteger.Zero, new BigInteger(arity)])
                || !Operands(body[6]).Select(Value).SequenceEqual([new BigInteger(arity), BigInteger.Zero]))
            {
                throw new InvalidOperationException(WrongApply);
            }
        }

        foreach (string control in new[] { "improper", "shadowed", "aliased" })
        {
            if (Body(control).Any(instruction => Opcode(instruction) == TailDynamicApply))
            {
                throw new InvalidOperationException(WrongApply);
            }
        }

        IEnumerable<object> Body(string name)
        {
            int info = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
                && atoms[(int)Value(Operands(instruction)[1])] == name);

            return instructions.Skip(info + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo);
        }
    }

    private static void CheckInstructions(byte[] beam)
    {
        object code = Code(beam);
        object[] instructions = Children(code, "Instructions").ToArray();
        string[] atoms = Children(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] split = Body("split");
        int frame = Array.FindIndex(split, instruction => Opcode(instruction) == Allocate);
        int comparison = Array.FindIndex(split, instruction => Opcode(instruction) == IsLess);
        int call = Array.FindIndex(split, instruction => Opcode(instruction) == LocalCall);
        if (comparison < 0 || frame <= comparison || frame >= call
            || !Operands(split[frame]).Select(Value).SequenceEqual([new BigInteger(3), new BigInteger(3)]))
        {
            throw new InvalidOperationException(WrongFrame);
        }

        int[] beforeCall = split.Take(frame).Where(instruction => Opcode(instruction) == GcBifTwo)
            .Select(instruction => (int)Value(Operands(instruction)[1])).ToArray();
        if (!beforeCall.SequenceEqual([2, 3])
            || !Body("joined").Any(instruction => Opcode(instruction) == Allocate)
            || !Body("caught").Any(instruction => Opcode(instruction) == TryEnter))
        {
            throw new InvalidOperationException(WrongFrame);
        }

        int failure = (int)Value(Operands(split[comparison])[0]);
        int cold = Array.FindIndex(split, instruction => Opcode(instruction) == Label
            && Value(Operands(instruction)[0]) == failure);
        if (cold < call || split.Skip(cold + 1).TakeWhile(instruction => Opcode(instruction) != Return)
            .Any(instruction => Opcode(instruction) is Allocate or AllocateHeap or Deallocate or LocalCall))
        {
            throw new InvalidOperationException(WrongFrame);
        }

        object[] Body(string function)
        {
            int entry = Array.FindIndex(instructions, instruction => Opcode(instruction) == FunctionInfo
                && atoms[(int)Value(Operands(instruction)[1])] == function);

            return instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static BigInteger Value(object operand) => (BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Children(instruction, "Operands").ToArray();

    private static IEnumerable<object> Children(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
