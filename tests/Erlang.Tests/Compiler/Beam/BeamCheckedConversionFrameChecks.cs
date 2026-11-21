using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCheckedConversionFrameChecks
{
    internal const string TestName = "compiler/beam/splits-checked-entry-conversion-frames";
    private const string Fixture = "beam_checked_conversion_frame_probe.erl";
    private const string WrongFrame = "The checked state and its conversion did not retain separate frame ownership.";
    private const string WrongResult = "The checked state conversion changed the result or error reason.";

    private const string Source = """
        -module(beam_checked_conversion_frame_probe).
        -export([grow/1,shrink/1,retained/2,guarded/1]).
        -record(counter,{n,p,l,mode}).

        grow(#counter{p=Index,n=Count,l=Level}=Original) ->
            Next = Index + 1,
            Larger = Count + 1,
            Result = case power2(Level) + 1 of
                Early when Early == Next ->
                    Original#counter{n=Larger,p=1,l=Level+1};
                _ ->
                    Original#counter{n=Larger,p=Next}
            end,
            {Result,[Index],[Larger]};
        grow(Old) ->
            New = convert(Old),
            grow(New).

        shrink(#counter{p=Index,n=Count,l=Level}=Original) ->
            Next = Index - 1,
            if
                Next < 1 ->
                    LowerLevel = Level - 1,
                    Pivot = power2(LowerLevel),
                    {Original#counter{n=Count-1,p=Pivot,l=LowerLevel},[Count],[Pivot]};
                true ->
                    {Original#counter{n=Count-1,p=Next},[Count],[Next]}
            end;
        shrink(Old) ->
            New = convert(Old),
            shrink(New).

        retained(#counter{p=Index,n=Count}=Original, Extra) ->
            {Original#counter{p=Index+1},power2(Count),Extra};
        retained(Old, Extra) ->
            New = convert(Old),
            retained(New, Extra).

        guarded(#counter{p=Index,n=Count}=Original) when is_number(Index) ->
            {Original#counter{p=Index+1},power2(Count)};
        guarded(Old) ->
            New = convert(Old),
            guarded(New).

        power2(Level) -> 1 bsl Level.
        convert({counter,N,P,L}) -> #counter{n=N,p=P,l=L,mode=normal}.
        """;

    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, Fixture);
        CheckFrames(beam);
        var module = BeamModule.Prepare(beam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term counter = Term.A("counter");
            Term normal = Term.A("normal");
            Term small = Term.I(1);
            Term old = Term.Tuple(
                counter,
                small,
                Term.I(2),
                small
            );
            Term invalid = Term.Tuple(
                counter,
                small,
                Term.A("bad"),
                small,
                normal
            );
            Term grown = Term.Tuple(
                Term.Tuple(
                    counter,
                    Term.I(2),
                    small,
                    Term.I(2),
                    normal
                ),
                Term.List(Term.I(2)),
                Term.List(Term.I(2))
            );
            if (!grown.Equals(await module.Invoke(context, "grow", old)))
            {
                throw new InvalidOperationException(WrongResult);
            }

            try
            {
                _ = await module.Invoke(context, "shrink", invalid);
            }
            catch (ErlangException failure) when (failure.Reason.Equals(Term.A("badarith")))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(WrongResult);
        });
    }

    private static void CheckFrames(byte[] beam)
    {
        object code = Code(beam);
        object[] instructions = Children(code, "Instructions").ToArray();
        string[] atoms = Children(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        Check("grow", split: true);
        Check("shrink", split: true);
        Check("retained", split: false);
        Check("guarded", split: false);

        void Check(string function, bool split)
        {
            int entry = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[(int)Value(Children(instruction, "Operands").ElementAt(1))] == function
            );
            object[] body = instructions.Skip(entry + 1).TakeWhile(item => Opcode(item) != FunctionInfo).ToArray();
            object first = body.First(instruction => Opcode(instruction) is not (Label or Line));
            object tag = body.First(item => Opcode(item) == IsTaggedTuple);
            BigInteger fallback = Value(Children(tag, "Operands").First());
            int rejected = Array.FindIndex(body, instruction => Opcode(instruction) == Label
                && Value(Children(instruction, "Operands").First()) == fallback);
            object tail = body.Skip(rejected).First(instruction => Opcode(instruction) == TailLocalCallWithFrame);
            BigInteger removed = Value(Children(tail, "Operands").Last());
            if (split
                ? !ReferenceEquals(first, tag) || removed != 0
                    || Opcode(body[rejected + 1]) != Allocate
                    || Value(Children(body[rejected + 1], "Operands").First()) != 0
                : removed <= 0)
            {
                throw new InvalidOperationException(WrongFrame);
            }
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static BigInteger Value(object operand) => (BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Children(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
