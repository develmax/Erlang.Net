using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamEnrollmentRequiredEnumerationChecks
{
    internal const string TestName = "compiler/beam/keeps-enumerated-pairs-in-the-required-record-schedule";
    private const string Fixture = "beam_required_enumeration.erl";
    private const string Peer = "beam_required_enumeration_peer";
    private const string WrongLayout = "A required enumeration split the retained record schedule.";
    private const string WrongValue = "A required enumeration changed a field or the order of failures.";

    private const string Source = """
        -module(beam_required_enumeration).
        -export([encode/2]).
        encode(Record, Tags) ->
            {_,A,B,C,D,E,F} = Record,
            {AB,AL} = beam_required_enumeration_peer:pack(A,[]),
            {BB,BL} = pack(B,[2]),
            {CB,CL} = case C of
                zero -> literal_pair(0,[3],1);
                one -> literal_pair(1,[3],1);
                Other -> exit({error,{format,{invalid,Other}}})
            end,
            {DB,DL} = case D of absent -> {<<>>,0}; _ -> pack(D,[4]) end,
            {EB,EL} = case E of absent -> {<<>>,0}; _ -> pack(E,[5]) end,
            {FB,FL} = case F of absent -> {<<>>,0}; _ -> pack(F,[6]) end,
            Parts = [AB,BB,CB,DB,EB,FB],
            Length = AL + BL + CL + DL + EL + FL,
            finish(Tags,Parts,Length).
        pack(Value, _) -> {Value,1}.
        literal_pair(Value, _, Length) -> {Value,Length}.
        finish(Tags, Parts, Length) -> {Tags,Parts,Length}.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        byte[] dependent = CodeGeneration.CompileBeamModule(
            Source.Replace("literal_pair(1,[3],1)", "literal_pair(1,[3],B)", StringComparison.Ordinal),
            Fixture
        );
        var module = BeamModule.Prepare(image, new BeamDependencySelection([Peer]));
        await using var runtime = new ProcessRuntime();
        Term malformed = Term.A("malformed");
        runtime.Modules.Register(
            Peer,
            "pack",
            2,
            (_, args) => ValueTask.FromResult(args[0].Equals(malformed) ? malformed : Term.Tuple(args[0], Term.I(2)))
        );
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term tag = Term.A("record");
            Term first = Term.A("first");
            Term second = Term.A("second");
            Term field = Term.A("field");
            Term absent = Term.A("absent");
            Term unknown = Term.A("unknown");
            Term empty = new BitString([]);
            await Expect(Record(first, Term.A("zero"), field), Term.List(
                first,
                second,
                Term.I(0),
                field,
                field,
                field
            ), 7);
            await Expect(Record(first, Term.A("one"), absent), Term.List(
                first,
                second,
                Term.I(1),
                empty,
                empty,
                empty
            ), 4);
            await Reject(
                Record(first, unknown, field),
                ErlangExceptionClasses.Exit,
                Term.Tuple(Term.A("error"), Term.Tuple(Term.A("format"), Term.Tuple(Term.A("invalid"), unknown)))
            );
            await Reject(
                Record(malformed, unknown, field),
                ErlangExceptionClasses.Error,
                Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), malformed)
            );

            return Nil.Value;

            Term Record(Term a, Term c, Term optional) => Term.Tuple(
                tag,
                a,
                second,
                c,
                optional,
                optional,
                optional
            );

            async Task Expect(Term input, Term bytes, int length)
            {
                if (!Term.Tuple(tag, bytes, Term.I(length)).Equals(await module.Invoke(
                    context,
                    "encode",
                    input,
                    tag
                )))
                {
                    throw new InvalidOperationException(WrongValue);
                }
            }

            async Task Reject(Term input, string kind, Term reason)
            {
                try
                {
                    await module.Invoke(
                        context,
                        "encode",
                        input,
                        tag
                    );
                }
                catch (ErlangException error) when (error.ExceptionClass == kind && error.Reason.Equals(reason))
                {
                    return;
                }

                throw new InvalidOperationException(WrongValue);
            }
        });

        if (Layout(image) != (7, 1, 2) || Layout(dependent) == (7, 1, 2))
        {
            throw new InvalidOperationException(WrongLayout);
        }
    }

    private static (int Frame, int Switches, int BadMatches) Layout(byte[] image)
    {
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions");
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Items(instruction, "Operands") is [_, var name, var arity]
                && atoms[Value(name)] == "encode" && Value(arity) == 2
        );
        if (start < 0)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        object[] body = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        int frame = Value(Items(body.Single(instruction => Opcode(instruction) == Allocate), "Operands")[0]);

        return (frame, body.Count(instruction => Opcode(instruction) == SelectValue), body.Count(instruction => Opcode(instruction) == BadMatch));
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
