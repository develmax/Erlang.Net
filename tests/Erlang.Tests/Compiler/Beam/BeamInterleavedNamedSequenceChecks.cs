using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamInterleavedNamedSequenceChecks
{
    internal const string TestName = "compiler/beam/keeps-interleaved-named-sequence-frames-compact";
    private const string Fixture = "beam_interleaved_named_sequence_probe.erl";
    private const string UnexpectedLayout = "The named sequence did not preserve its compact continuation layout.";
    private const string UnexpectedResult = "The named sequence changed a checked or discarded continuation.";

    private const string Source = """
        -module(beam_interleaved_named_sequence_probe).
        -export([unchecked/2, checked/2]).

        unchecked(Initial, Tags) ->
            Parsed = parse(Initial, Tags),
            [Version|Next] = Parsed,
            Named = begin N = field(Version, [2]), name(N, [{v0,0}]) end,
            [Required|Tail] = Next,
            Main = field(Required,[4]),
            {Maybe,Rest} = case Tail of
                [{131072,Optional}|Extra] -> {field(Optional,[]),Extra};
                _ -> {absent,Tail}
            end,
            case Rest of [] -> true; _ -> true end,
            Result = {unchecked,Named,Main,Maybe},
            Result.

        checked(Initial, Tags) ->
            Parsed = parse(Initial, Tags),
            [Version|Next] = Parsed,
            Named = begin N = field(Version,[2]), name(N,[{v0,0}]) end,
            {Maybe,Rest} = case Next of
                [{131072,Optional}|More] -> {field(Optional,[]),More};
                _ -> {absent,Next}
            end,
            [Left|Tail] = Rest,
            First = field(Left,[16]),
            [Right|Trailing] = Tail,
            Last = field(Right,[4]),
            case Trailing of
                [] -> true;
                _ -> exit({error,{asn1,{unexpected,Trailing}}})
            end,
            Result = {checked,Named,Maybe,First,Last},
            Result.

        parse(Value,_) -> Value.
        field(Value,_) -> Value.
        name(0,_) -> v0;
        name(Value,_) -> Value.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        Check(
            "unchecked",
            3,
            1,
            0
        );
        Check(
            "checked",
            5,
            0,
            1
        );

        await using var runtime = new ProcessRuntime();
        var module = BeamModule.Prepare(image, new BeamDependencySelection(["erlang"]));
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term left = Term.A("left");
            Term right = Term.A("right");
            Term value = Term.A("value");
            Term version = Term.A("v0");
            Term absent = Term.A("absent");
            Term checkedTag = Term.A("checked");
            Term uncheckedTag = Term.A("unchecked");
            Term extra = Term.A("extra");
            Term optional = Term.Tuple(Term.I(131072), value);
            Term input = Term.List(
                Term.I(0),
                optional,
                left,
                right
            );
            Term marker = Term.A("tag");
            Term actualChecked = await module.Invoke(
                context,
                "checked",
                input,
                marker
            );
            Term missing = await module.Invoke(
                context,
                "checked",
                Term.List(Term.I(0), left, right),
                marker
            );
            Term ignored = await module.Invoke(
                context,
                "unchecked",
                Term.List(
                    Term.I(0),
                    left,
                    optional,
                    extra
                ),
                marker
            );
            if (!actualChecked.Equals(Term.Tuple(
                checkedTag,
                version,
                value,
                left,
                right
            ))
                || !missing.Equals(Term.Tuple(
                    checkedTag,
                    version,
                    absent,
                    left,
                    right
                ))
                || !ignored.Equals(Term.Tuple(
                    uncheckedTag,
                    version,
                    left,
                    value
                )))
            {
                throw new InvalidOperationException(UnexpectedResult);
            }

            try
            {
                await module.Invoke(
                    context,
                    "checked",
                    Term.List(
                        Term.I(0),
                        left,
                        right,
                        extra
                    ),
                    marker
                );
                throw new InvalidOperationException(UnexpectedResult);
            }
            catch (ErlangException error) when (error.Reason.Equals(
                Term.Tuple(
                    Term.A("error"),
                    Term.Tuple(Term.A("asn1"), Term.Tuple(Term.A("unexpected"), Term.List(extra)))
                )
            ))
            {
            }

            return Nil.Value;
        });

        void Check(
            string name,
            int frame,
            int getHeadCount,
            int exitTailCount
        )
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && Items(instruction, "Operands").ToArray() is [_, var atom, _]
                    && atoms[(int)(BigInteger)Property(atom, "Value")] == name
            );
            if (start < 0)
            {
                throw new InvalidOperationException(UnexpectedLayout);
            }

            object[] body = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
            int[] frames = body.Where(instruction => Opcode(instruction) == Allocate)
                .Select(instruction => (int)(BigInteger)Property(Items(instruction, "Operands").First(), "Value"))
                .ToArray();
            if (frames is not [var size] || size != frame
                || body.Count(instruction => Opcode(instruction) == GetHead) != getHeadCount
                || body.Count(instruction => Opcode(instruction) == TailExternalCallWithFrame) != exitTailCount)
            {
                throw new InvalidOperationException(UnexpectedLayout);
            }
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
