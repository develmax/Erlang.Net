using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamOptionalTableEncoderChecks
{
    internal const string TestName = "compiler/beam/preserves-a-required-field-across-an-optional-table-call";
    private const string Fixture = "beam_optional_table_encoder_probe.erl";
    private const string Module = "beam_optional_table_encoder_probe";
    private const string IncorrectFrame = "The mandatory pair and the optional table callback did not share the checked frame.";
    private const string IncorrectOutcome = "The optional field, callback exception, or bad-match operand changed.";
    private const string Record = "value";
    private const string Options = "options";
    private const string Identifier = "id";
    private const string Absent = "absent";
    private const string Invalid = "invalid";
    private const string Present = "present";
    private const string CallbackFailure = "selected_failure";
    private const string Source = """
        -module(beam_optional_table_encoder_probe).
        -export([pack/2, unstable/2, factory/1]).
        pack(Value, Tags) ->
            {_, Id, Extra} = Value,
            Encoder = beam_optional_table_encoder_probe:factory(Id),
            {Head, HeadSize} = required(Id, [identifier]),
            {Tail, TailSize} = case Extra of
                absent -> {<<>>, 0};
                _ ->
                    {Bytes, _} = Encoder(field, Extra, []),
                    optional(Bytes, [])
            end,
            Parts = [Head, Tail],
            Length = HeadSize + TailSize,
            combine(Tags, Parts, Length).

        unstable(Value, Tags) ->
            {_, Id, Extra} = Value,
            Encoder = beam_optional_table_encoder_probe:factory(Id),
            {Head, HeadSize} = required(Id, [identifier]),
            {Tail, TailSize} = case Extra of
                absent -> {<<>>, 0};
                _ ->
                    {Bytes, _} = Encoder(field, Extra, []),
                    invalid_optional(Bytes, [])
            end,
            Parts = [Head, Tail],
            Length = HeadSize + TailSize,
            combine(Tags, Parts, Length).

        factory(_Id) ->
            fun field/3.
        field(field, invalid, []) ->
            invalid;
        field(field, fail, []) ->
            erlang:error(selected_failure);
        field(field, Extra, []) ->
            {Extra, ignored}.

        required(Id, [identifier]) ->
            {Id, 1}.
        optional(Bytes, []) ->
            {Bytes, 2}.
        invalid_optional(Bytes, []) ->
            {Bytes, invalid}.
        combine(Tags, [Head, Tail], Length) ->
            {Tags, Head, Tail, Length}.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        object[] pack = Function("pack");
        int tuple = Array.FindIndex(pack, item => Opcode(item) == IsTuple);
        int frame = Array.FindIndex(pack, item => Opcode(item) == Allocate);
        int mandatoryCall = Array.FindIndex(pack, item => Opcode(item) == LocalCall);
        int invoke = Array.FindIndex(pack, item => Opcode(item) == CallFun);
        if (tuple < 0 || frame <= tuple || mandatoryCall <= frame || invoke <= mandatoryCall
            || Items(pack[frame], "Operands").ToArray() is not [var slots, var live]
            || Value(slots) != 4 || Value(live) != 2
            || pack.Count(item => Opcode(item) == BadMatch) != 2
            || pack.Count(item => Opcode(item) == LocalCall) != 2
            || !pack.Any(item => Opcode(item) == TailLocalCallWithFrame))
        {
            throw new InvalidOperationException(IncorrectFrame);
        }


        await using var runtime = new ProcessRuntime();
        PreparedBeamImage prepared = runtime.Modules.PrepareBeam(image, new BeamDependencySelection([Module]));
        if (runtime.Modules.PublishPreparedBeam(prepared).Status != BeamPublicationStatus.Published)
        {
            throw new InvalidOperationException(IncorrectOutcome);
        }
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term key = Term.A(Identifier);
            Term tags = Term.A(Options);
            Term present = Term.A(Present);
            Term fieldAbsent = Term.Tuple(Term.A(Record), key, Term.A(Absent));
            Term fieldPresent = Term.Tuple(Term.A(Record), key, present);
            var absent = Term.Tuple(
                tags,
                key,
                new BitString([]),
                Term.I(1)
            );
            var included = Term.Tuple(
                tags,
                key,
                present,
                Term.I(3)
            );
            if (!absent.Equals(await runtime.Modules.Call(
                context,
                Module,
                "pack",
                [fieldAbsent, tags]
            ))
                || !included.Equals(await runtime.Modules.Call(
                    context,
                    Module,
                    "pack",
                    [fieldPresent, tags]
                )))
            {
                throw new InvalidOperationException(IncorrectOutcome);
            }

            await ExpectError("pack", key, Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), key));
            await ExpectError("pack", Term.Tuple(Term.A(Record), key, Term.A(Invalid)),
                Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), Term.A(Invalid)));
            await ExpectError("pack", Term.Tuple(Term.A(Record), key, Term.A("fail")), Term.A(CallbackFailure));
            await ExpectError("unstable", fieldPresent, Term.A(ErlangErrorReasons.BadArithmetic));

            return Nil.Value;

            async Task ExpectError(string name, Term record, Term reason)
            {
                try
                {
                    _ = await runtime.Modules.Call(
                        context,
                        Module,
                        name,
                        [record, tags]
                    );
                }
                catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error && error.Reason.Equals(reason))
                {
                    return;
                }

                throw new InvalidOperationException(IncorrectOutcome);
            }
        });

        object[] Function(string name)
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && Items(instruction, "Operands").ToArray() is [_, var atom, var arity]
                    && atoms[Value(atom)] == name && Value(arity) == 2
            );
            if (start < 0)
            {
                throw new InvalidOperationException(IncorrectFrame);
            }

            return instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();
}
