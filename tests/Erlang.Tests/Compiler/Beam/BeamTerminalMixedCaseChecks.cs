using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTerminalMixedCaseChecks
{
    internal const string TestName = "compiler/beam/disjoint-terminal-atom-and-tagged-tuple";
    private const string WrongSelection = "A terminal atom and disjoint tagged tuple must use the successful tuple type proof.";
    private const int TupleType = 1 << 11;
    private const string SourceFile = "terminal_case_probe.erl";
    private const string Source = """
        -module(terminal_case_probe).
        -export([atom_first/1, tuple_first/1, guarded/1, nonterminal/1]).
        atom_first(Input) ->
            case terminal_case_peer:select_value(Input) of
                ok -> ok;
                {error, _} -> terminal_case_peer:cleanup(Input)
            end.
        tuple_first(Input) ->
            case terminal_case_peer:select_value(Input) of
                {error, _} -> terminal_case_peer:cleanup(Input);
                ok -> ok
            end.
        guarded(Input) ->
            case terminal_case_peer:select_value(Input) of
                ok -> ok;
                {error, Data} when is_atom(Data) -> terminal_case_peer:cleanup(Data);
                {error, Fallback} -> {other, Fallback}
            end.
        nonterminal(Input) ->
            Result = case terminal_case_peer:select_value(Input) of ok -> ok; {error, _} -> ignored end,
            {result, Result}.
        """;

    internal static Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, SourceFile);
        byte[] withoutSsa = BeamAdjacentNumericResultChecks.Compile(
            Source.Replace("-export(", "-compile(no_ssa_opt).\n-export(", StringComparison.Ordinal),
            disabled: true
        );
        Check(beam, expectSpecialization: true);
        Check(withoutSsa, expectSpecialization: false);
        CheckColdReturnExpansion();

        return Task.CompletedTask;
    }

    private static void CheckColdReturnExpansion()
    {
        const byte atom = 0x12;
        const byte nil = 0x02;
        const byte x0 = 0x03;
        const byte zero = 0x00;
        const byte one = 0x10;
        const byte two = 0x20;
        const byte info = 0x10;
        const byte entry = 0x20;
        const byte literalArm = 0x30;
        const byte errorArm = 0x40;
        const byte returnArm = 0x50;
        const byte toLiteral = 0x35;
        const byte toError = 0x45;
        const byte toReturn = 0x55;
        byte[] prefix = [Label, info, FunctionInfo, atom, atom, one, Label, entry];
        byte[] tuple = [IsTuple, toLiteral, x0, IsTaggedTuple, toError, ExtendedTypedRegisterOperand, x0, zero, two, atom];
        byte[] tail = [TailExternalCallWithFrame, one, zero, zero];
        byte[] identity = [Label, literalArm, ExactNotEqualTest, toReturn, x0, atom];
        byte[] cold = [Label, errorArm, Line, one, CaseEnd, x0];
        byte[] shared = [Label, returnArm, Deallocate, zero, Return, EndCode];
        byte[] expanded = [Label, literalArm, ExactEqualTest, toError, x0, atom, Deallocate, zero, Return];
        if (Rewrite([.. prefix, .. tuple, .. tail, .. identity, .. cold, .. shared]) is not { } code
            || !code.AsSpan().SequenceEqual([.. prefix, .. tuple, .. tail, .. expanded, .. cold, .. shared])
            || Rewrite([.. prefix, .. tuple, Move, nil, x0, .. identity, .. cold, .. shared]) is not null
            || Rewrite([.. prefix, ExactEqualTest, toReturn, x0, atom, .. tuple, .. tail, .. identity, .. cold, .. shared]) is not null)
        {
            throw new InvalidOperationException(WrongSelection);
        }

        static byte[]? Rewrite(byte[] source)
        {
            ReadOnlyMemory<byte> instructions = source;
            object header = New(
                "BeamCodeHeader",
                0,
                (int)InitYRegisters,
                6,
                1,
                instructions
            );
            object body = Call(
                Type("BeamInstructionReader"),
                "Read",
                header,
                2,
                0,
                null,
                null
            );

            return (byte[]?)Call(
                typeof(BeamModuleCompiler),
                "ExpandShortReturnJumps",
                instructions,
                body
            );
        }
    }

    private static void Check(byte[] beam, bool expectSpecialization)
    {
        object code = Code(beam);
        object structure = Property(code, "Structure");
        object table = Call(Type("BeamTypeTableReader"), "Read", structure);
        object[] types = ((IEnumerable)Property(table, "Entries")).Cast<object>().ToArray();
        string[] atoms = ((IEnumerable)Property(structure, "Atoms")).Cast<string>().ToArray();
        object[] instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        int[] entries = Enumerable.Range(0, instructions.Length)
            .Where(index => Opcode(instructions[index]) == FunctionInfo).ToArray();

        AssertSpecialized("atom_first", expectSpecialization);
        AssertSpecialized("tuple_first", expectSpecialization);
        AssertSpecialized("guarded", false);
        AssertSpecialized("nonterminal", false);

        void AssertSpecialized(string function, bool expected)
        {
            int match = Array.FindIndex(entries, entry => atoms[(int)(BigInteger)Property(Operand(instructions[entry], 1), "Value")] == function);
            object[] body = instructions[entries[match]..entries[match + 1]];
            int tuple = Array.FindIndex(body, operation => Opcode(operation) == IsTuple);
            bool selected = tuple >= 0 && tuple + 1 < body.Length && Opcode(body[tuple + 1]) == IsTaggedTuple
                && Property(Operand(body[tuple + 1], 1), "TypeIndex") is int index
                && (int)Property(types[index], "TypeUnion") == TupleType;
            if (selected != expected || !body.Any(operation => Opcode(operation) == CaseEnd))
            {
                throw new InvalidOperationException(WrongSelection);
            }
        }
    }

    private static object Operand(object instruction, int position) =>
        ((IEnumerable)Property(instruction, "Operands")).Cast<object>().ElementAt(position);

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");
}
