using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamWritableBinaryListHelperChecks
{
    internal const string TestName = "compiler/beam/moves-a-single-bit-list-builder-into-a-writable-helper";
    private const string Module = "beam_writable_binary_list_helper";
    private const string PackHelper = "-pack/1-lbc$^0/2-0-";
    private const string RawHelper = "-raw/1-lbc$^0/2-0-";
    private const string WrongLayout = "Expected one writable accumulator and a frameless helper with the full X root prefix.";
    private const string WrongValue = "The writable helper changed a produced value or changed the original generator failure.";
    private const string Source = """
        -module(beam_writable_binary_list_helper).
        -export([pack/1, raw/1, wide/1]).
        pack(Items) ->
            << <<(Item-$0):1>> || Item <- Items >>.
        raw(Items) ->
            << <<Item:1>> || Item <- Items >>.
        wide(Items) ->
            << <<Item:2>> || Item <- Items >>.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Module + ".erl");
        BeamModule module = BeamModule.Prepare(image);
        if (!module.Functions.Any(function => function.Name == PackHelper)
            || !module.Functions.Any(function => function.Name == RawHelper)
            || module.Functions.Any(function => function.Name == "-wide/1-lbc$^0/2-0-"))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        RequireLayout(Code(image), PackHelper);
        RequireLayout(Code(image), RawHelper);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term expected = new BitString([0xA0], 3);
            Term packed = await module.Invoke(context, "pack", Term.List(Term.I(49), Term.I(48), Term.I(49)));
            Term raw = await module.Invoke(context, "raw", Term.List(Term.I(1), Term.I(0), Term.I(1)));
            Term empty = await module.Invoke(context, "pack", Nil.Value);
            if (!expected.Equals(packed) || !expected.Equals(raw) || !new BitString([]).Equals(empty))
            {
                throw new InvalidOperationException(WrongValue);
            }

            Term invalid = Term.A("tail");
            Term improper = new Cons(Term.I(49), invalid);
            try
            {
                await module.Invoke(context, "pack", improper);
            }
            catch (ErlangException error) when (
                error.Reason.Equals(Term.Tuple(Term.A(ComprehensionErrorReasons.BadGenerator), invalid))
                && error.StackTraceTerm is Cons
                {
                    Head: TupleTerm { Items: [Atom { Name: Module }, Atom { Name: PackHelper }, _, _] }
                })
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(WrongValue);
        });
    }

    private static void RequireLayout(object code, string helper)
    {
        var functions = ((IEnumerable)Property(code, "Functions")).Cast<object>();
        object function = functions.Single(item => (string)Property(item, "Name") == helper);
        int first = (int)Property(function, "FirstInstructionIndex");
        int end = (int)Property(function, "EndInstructionIndex");
        var instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>()
            .Skip(first).Take(end - first)
            .Select(instruction => (Instruction: instruction, Opcode: (byte)Property(Property(instruction, "Definition"), "Opcode")))
            .ToArray();
        object construction = instructions.Single(instruction => instruction.Opcode == CreateBinary).Instruction;
        var operands = ((IEnumerable)Property(construction, "Operands")).Cast<object>().ToArray();
        if (instructions.Any(instruction => instruction.Opcode is Allocate or AllocateHeap)
            || !instructions.Any(instruction => instruction.Opcode == TailLocalCall)
            || (BigInteger)Property(operands[2], "Value") != 3
            || (BigInteger)Property(operands[4], "Value") != 1)
        {
            throw new InvalidOperationException(WrongLayout);
        }
    }
}
