using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamGroupedRecordCarrierEntryChecks
{
    internal const string TestName = "compiler/beam/checks-a-grouped-entry-record-before-allocating";
    private const string Module = "beam_grouped_record_carrier";
    private const string WrongEntry = "The grouped event must check its record before allocating and preserve the unmatched carrier.";
    private const string Source = """
        -module(beam_grouped_record_carrier).
        -record(state, {counts, mapped}).
        -export([dispatch/2]).
        dispatch({opened, _Prefix, Name, _QName, Value}, #state{counts=Counts} = State) ->
            Mapped = convert(Name, Value),
            Next = case Counts of [] -> 1; [N|_] -> N+1 end,
            State#state{counts=[Next], mapped=Mapped};
        dispatch({ignored, _}, State) -> State;
        dispatch(_, State) -> State.
        convert(Name, Value) -> {list_to_atom(Name), Value}.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Module + ".erl");
        RequireChecksBeforeAllocate(Code(image));
        BeamModule module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term state = Term.A("state");
            Term missing = Term.A("missing");
            Term value = Term.A("payload");
            Term name = Term.List(Term.I('a'));
            Term empty = Term.Tuple(state, Nil.Value, missing);
            Term nonempty = Term.Tuple(state, Term.List(Term.I(9)), missing);
            Term malformed = Term.Tuple(state, Nil.Value);
            Term opened = Term.Tuple(
                Term.A("opened"),
                missing,
                name,
                missing,
                value
            );
            Term wrongEvent = Term.Tuple(
                Term.A("unknown"),
                missing,
                name,
                missing,
                value
            );
            Term mapped = Term.Tuple(Term.A("a"), value);
            if (!Term.Tuple(state, Term.List(Term.I(1)), mapped).Equals(
                    await module.Invoke(
                        context,
                        "dispatch",
                        opened,
                        empty
                    ))
                || !Term.Tuple(state, Term.List(Term.I(10)), mapped).Equals(
                    await module.Invoke(
                        context,
                        "dispatch",
                        opened,
                        nonempty
                    ))
                || !malformed.Equals(await module.Invoke(
                    context,
                    "dispatch",
                    opened,
                    malformed
                ))
                || !empty.Equals(await module.Invoke(
                    context,
                    "dispatch",
                    wrongEvent,
                    empty
                )))
            {
                throw new InvalidOperationException(WrongEntry);
            }

            return Nil.Value;
        });
    }

    private static void RequireChecksBeforeAllocate(object code)
    {
        object function = Items(code, "Functions").Single(item => (string)Property(item, "Name") == "dispatch");
        int start = (int)Property(function, "FirstInstructionIndex");
        int end = (int)Property(function, "EndInstructionIndex");
        object[] body = Items(code, "Instructions").Skip(start).Take(end - start).ToArray();
        object dispatch = body.Single(item => Opcode(item) == SelectTupleArity);
        object[] operands = Items(dispatch, "Operands").ToArray();
        int target = 0;
        for (int index = 3; index + 1 < operands.Length; index += 2)
        {
            if (Value(operands[index]) == 5)
            {
                target = Value(operands[index + 1]);
            }
        }

        int first = Array.FindIndex(body, item =>
            Opcode(item) == Label && Items(item, "Operands").Select(Value).Single() == target) + 1;
        if (target == 0 || first == 0 || first + 3 >= body.Length
            || Opcode(body[first]) != GetTupleElement || Opcode(body[first + 1]) != ExactEqualTest
            || Opcode(body[first + 2]) != IsTaggedTuple || Opcode(body[first + 3]) is not (Allocate or AllocateHeap)
            || Items(body[first + 2], "Operands").ToArray() is not [_, var record, _, _]
            || (string)Property(record, "Kind") != "x" || Value(record) != 1)
        {
            throw new InvalidOperationException(WrongEntry);
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string property) => ((IEnumerable)Property(owner, property)).Cast<object>();
}
