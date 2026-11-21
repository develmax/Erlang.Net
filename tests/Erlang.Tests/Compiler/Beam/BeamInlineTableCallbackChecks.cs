using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamInlineTableCallbackChecks
{
    internal const string TestName = "compiler/beam/keeps-inline-table-callbacks-framed-and-protected";
    private const string Module = "beam_inline_table_callback_probe";
    private const string Fixture = Module + ".erl";
    private const string WrongInstructions = "The inline table callback did not retain the required register layout.";
    private const string WrongResult = "The inline table callback returned an unexpected result.";
    private const string MissingFailure = "The inline table callback accepted an invalid result or suppressed its failure.";

    private const string Source = """
        -module(beam_inline_table_callback_probe).
        -export([encode/2, decode/2, select_encode/1, select_decode/1]).

        encode(Value, Tags) ->
            {_,Key,Field} = Value,
            Callback = beam_inline_table_callback_probe:select_encode(Key),
            {KB,KL} = emit_pair(Key,[<<6>>]),
            {Raw,_} = Callback('Type',Field,[]),
            {VB,VL} = emit_pair(Raw,[]),
            Bytes = [KB,VB],
            Length = KL + VL,
            finish(Tags,Bytes,Length).

        decode(Value, Tags) ->
            Initial = prepare(Value,Tags),
            [K|Tail] = Initial,
            Key = read_field(K,[6]),
            [V|Left] = Tail,
            Raw = read_field(V,[]),
            Callback = beam_inline_table_callback_probe:select_decode(Key),
            Field = try Callback('Type',Raw,[])
                    catch Class:Reason when Class =:= error; Class =:= exit ->
                        exit({rejected,Reason})
                    end,
            case Left of [] -> true; _ -> exit({error,{asn1,{unexpected,Left}}}) end,
            Result = {decoded,Key,Field},
            Result.

        select_encode(valid) -> fun(_,Value,_) -> {Value,1} end;
        select_encode(_) -> fun(_,_,_) -> malformed end.
        select_decode(valid) -> fun(_,Value,_) -> Value end;
        select_decode(_) -> fun(_,Value,_) -> exit(Value) end.
        emit_pair(Value, _) -> {Value,1}.
        finish(Tags, Bytes, Length) -> {Tags,Bytes,Length}.
        prepare(Value, _) -> Value.
        read_field(Value, _) -> Value.
        """;

    internal static byte[] CompileFixture() => CodeGeneration.CompileBeamModule(Source, Fixture);

    internal static async Task Run()
    {
        byte[] image = CompileFixture();
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions").ToArray();
        Check("encode", 3);
        Check("decode", 4);

        await using var runtime = new ProcessRuntime();
        var module = BeamModule.Prepare(image, new BeamDependencySelection([Module, "erlang"]));
        runtime.Modules.Register(
            Module,
            "select_encode",
            1,
            (context, arguments) => module.Invoke(context, "select_encode", arguments[0])
        );
        runtime.Modules.Register(
            Module,
            "select_decode",
            1,
            (context, arguments) => module.Invoke(context, "select_decode", arguments[0])
        );
        await using var receiver = new RuntimeNodeFoundationHostActor(runtime);
        await receiver.InvokeAsync(async context =>
        {
            Term good = Term.A("valid");
            Term bad = Term.A("invalid");
            Term value = Term.A("sample");
            Term record = Term.A("decoded");
            Term tags = Term.A("tags");
            Term bytes = Term.List(good, value);
            Term encoded = await module.Invoke(
                context,
                "encode",
                Term.Tuple(record, good, value),
                tags
            );
            Term decoded = await module.Invoke(
                context,
                "decode",
                bytes,
                tags
            );
            if (!Term.Tuple(tags, bytes, Term.I(2)).Equals(encoded) || !Term.Tuple(record, good, value).Equals(decoded))
            {
                throw new InvalidOperationException(WrongResult);
            }

            try
            {
                await module.Invoke(
                    context,
                    "encode",
                    Term.Tuple(record, bad, value),
                    tags
                );
                throw new InvalidOperationException(MissingFailure);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A("badmatch"), Term.A("malformed"))))
            {
            }

            try
            {
                await module.Invoke(
                    context,
                    "decode",
                    Term.List(bad, value),
                    tags
                );
                throw new InvalidOperationException(MissingFailure);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A("rejected"), value)))
            {
            }

            return Nil.Value;
        });

        void Check(string name, int frame)
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && Items(instruction, "Operands").ToArray() is [_, var atom, _]
                    && atoms[(int)(BigInteger)Property(atom, "Value")] == name
            );
            if (start < 0)
            {
                throw new InvalidOperationException(WrongInstructions);
            }

            object[] body = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
            var frames = body.Where(instruction => Opcode(instruction) == Allocate)
                .Select(instruction => (int)(BigInteger)Property(Items(instruction, "Operands").First(), "Value"))
                .ToArray();
            if (frames is not [var size] || size != frame || body.Count(instruction => Opcode(instruction) == CallFun) != 1)
            {
                throw new InvalidOperationException(WrongInstructions);
            }
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
