using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamRecursiveListFrameChecks
{
    internal const string TestName = "compiler/beam/retains-one-owner-in-generated-recursive-list-helpers";
    private const string Fixture = "beam_recursive_list_frames.erl";
    private const string ProvenTupleFrame = "A list of source-proven tuple elements retained an unnecessary rejection branch.";
    private const string UnknownTupleFrame = "A caller-controlled generator lost its shape and improper-tail checks.";
    private const string TransformFrame = "A recursive call-mapping frame did not preserve the mapped result and tail.";
    private const string MissingFixtureFunction = "A recursive fixture did not have its generated helper.";

    private const string Source = """
        -module(beam_recursive_list_frames).
        -export([closed/0,unknown/1,mixed/0,improper/0,render/1,render_local/1]).

        closed() -> [Key || {Key, _} <- rows()].
        unknown(Input) -> [Key || {Key, _} <- Input].
        mixed() -> [Key || {Key, _} <- mixed_rows()].
        improper() -> [Key || {Key, _} <- improper_rows()].
        render(Input) -> ["N=" ++ integer_to_list(Value) || Value <- Input].
        render_local(Input) -> [token(Value) ++ "!" || Value <- Input].
        token(first) -> "first";
        token(second) -> "second".
        rows() -> [{alpha,1},{beta,token(second)}].
        mixed_rows() -> [{alpha,1},non_tuple,{beta,2}].
        improper_rows() -> [{alpha,1}|bad_tail].
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        var functions = ReadFunctions(image);
        var closed = functions("-closed/0-lc$^0/1-0-");
        if (Find(closed, Allocate) < 0 || Find(closed, Allocate) > Find(closed, GetList)
            || Find(closed, IsTuple) >= 0 || Find(closed, IsNil) >= 0)
        {
            throw new InvalidOperationException(ProvenTupleFrame);
        }

        var unknown = functions("-unknown/1-lc$^0/1-0-");
        var mixed = functions("-mixed/0-lc$^0/1-0-");
        if (Find(unknown, IsTuple) < 0 || Find(unknown, IsNil) < 0 || Find(mixed, IsTuple) < 0)
        {
            throw new InvalidOperationException(UnknownTupleFrame);
        }

        var rendered = functions("-render/1-lc$^0/1-0-");
        int renderedFrame = Find(rendered, Allocate);
        int trim = Find(rendered, TrimFrame);
        if (renderedFrame < 0 || Value(Operands(rendered[renderedFrame])[0]) != 2
            || trim < 0 || Value(Operands(rendered[trim])[0]) != 1 || Find(rendered, IsNil) < 0)
        {
            throw new InvalidOperationException(TransformFrame);
        }

        var local = functions("-render_local/1-lc$^0/1-0-");
        int localFrame = Find(local, Allocate);
        if (localFrame < 0 || Value(Operands(local[localFrame])[0]) != 1 || Find(local, Swap) < 0
            || Find(local, IsNil) < 0)
        {
            throw new InvalidOperationException(TransformFrame);
        }

        var module = BeamModule.Prepare(image, new BeamDependencySelection(["lists"]));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term expected = Term.List(Term.A("alpha"), Term.A("beta"));
            if (!expected.Equals(await module.Invoke(context, "mixed")))
            {
                throw new InvalidOperationException(UnknownTupleFrame);
            }

            Term reason = Term.Tuple(Term.A(ErlangErrorReasons.BadGenerator), Term.A("bad_tail"));
            try
            {
                _ = await module.Invoke(context, "improper");
            }
            catch (ErlangException error) when (error.Reason.Equals(reason))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(UnknownTupleFrame);
        });
    }

    private static Func<string, object[]> ReadFunctions(byte[] image)
    {
        object code = Code(image);
        var instructions = Items(code, "Instructions").ToArray();
        var atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();

        return name =>
        {
            int info = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo && atoms[Value(Operands(instruction)[1])] == name
            );
            if (info < 0)
            {
                throw new InvalidOperationException(MissingFixtureFunction);
            }

            return instructions.Skip(info + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        };
    }

    private static int Find(object[] instructions, byte opcode) =>
        Array.FindIndex(instructions, instruction => Opcode(instruction) == opcode);

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
