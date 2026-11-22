using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamListConversionResultChecks
{
    internal const string TestName = "compiler/beam/list-conversion-types-its-successful-bif-consumer";
    internal const string InferredLocalTestName = "compiler/beam/inferred-local-proper-list-result";
    private const string WrongInferredLocalType = "Expected the proper list annotation only for a closed recursive accumulator.";
    private const string InferredLocalSource = """
        -module(beam_adjacent_numeric).
        %% options
        -export([value/2]).
        value(Items, Excluded) ->
            case collect(Items, Excluded, []) of
                [] -> [];
                Found -> [{output, Found}]
            end.
        collect([], _Excluded, Acc) ->
            Acc;
        collect([Item | Rest], Excluded, Acc) ->
            case lists:member(Item, Excluded) of
                true -> collect(Rest, Excluded, Acc);
                false -> collect(Rest, Excluded, [Item | Acc])
            end.
        """;

    internal static Task RunInferredLocalResult()
    {
        byte[] direct = BeamAdjacentNumericResultChecks.Compile(InferredLocalSource);
        byte[] disabled = BeamAdjacentNumericResultChecks.Compile(
            InferredLocalSource.Replace("%% options", "-compile(no_ssa_opt).", StringComparison.Ordinal),
            disabled: true
        );
        byte[] improper = BeamAdjacentNumericResultChecks.Compile(InferredLocalSource.Replace(
            "collect(Items, Excluded, [])",
            "collect(Items, Excluded, improper_tail)",
            StringComparison.Ordinal
        ));
        if (ReadType(direct) != ListType || ReadType(disabled) is not null || ReadType(improper) == ListType)
        {
            throw new InvalidOperationException(WrongInferredLocalType);
        }

        return Task.CompletedTask;

        static int? ReadType(byte[] beam) =>
            BeamAdjacentNumericResultChecks.InstructionInputType(
                beam,
                IsNil,
                1,
                first: true,
                required: false
            );
    }

    private const int ListType = (1 << 2) | (1 << 7);
    private const string WrongOperand = "The ordinary list BIF must type only the result of a resolved conversion.";
    private const string WrongValue = "The list conversion and its consumer returned an unexpected value or error.";
    private const string SourceFile = "list_conversion_probe.erl";
    private const string Source = """
        -module(list_conversion_probe).
        -compile({no_auto_import,[tuple_to_list/1]}).
        -export([tuple_tail/1, tuple_head/1, atom_length/1, binary_length/1,
                 copied_length/1, shadow/1, dynamic/2, unrelated/2, branch/3]).

        tuple_tail(Tuple) -> tl(erlang:tuple_to_list(Tuple)).
        tuple_head(Tuple) -> hd(erlang:tuple_to_list(Tuple)).
        atom_length(Atom) -> length(erlang:atom_to_list(Atom)).
        binary_length(Binary) -> length(erlang:binary_to_list(Binary)).
        copied_length(Atom) ->
            List = erlang:atom_to_list(Atom),
            {List,length(List)}.

        tuple_to_list(_) -> [head|improper].
        shadow(Tuple) -> tl(tuple_to_list(Tuple)).
        dynamic(Module,Tuple) -> tl(Module:tuple_to_list(Tuple)).
        unrelated(Tuple,List) -> erlang:tuple_to_list(Tuple), tl(List).
        branch(Flag,Tuple,List) ->
            Value = case Flag of true -> erlang:tuple_to_list(Tuple); false -> List end,
            tl(Value).
        """;

    internal static async Task Run()
    {
        byte[] enabled = CodeGeneration.CompileBeamModule(Source, SourceFile);
        byte[] disabled = BeamAdjacentNumericResultChecks.Compile(
            Source.Replace("-export(", "-compile(no_ssa_opt).\n-export(", StringComparison.Ordinal),
            disabled: true
        );
        CheckTypes(enabled, ssaEnabled: true);
        CheckTypes(disabled, ssaEnabled: false);

        var module = BeamModule.Prepare(enabled);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term first = Term.A("first");
            Term last = Term.A("last");
            Term improper = Term.A("improper");
            Term improperList = new Cons(first, improper);
            Term tuple = Term.Tuple(first, last);
            Term letters = Term.List(Term.I('a'), Term.I('b'));

            if (!(await module.Invoke(context, "tuple_tail", tuple)).Equals(Term.List(last))
                || !(await module.Invoke(context, "tuple_head", tuple)).Equals(first)
                || !(await module.Invoke(context, "atom_length", Term.A("ab"))).Equals(Term.I(2))
                || !(await module.Invoke(context, "binary_length", new BitString([1, 2, 3]))).Equals(Term.I(3))
                || !(await module.Invoke(context, "copied_length", Term.A("ab"))).Equals(Term.Tuple(letters, Term.I(2)))
                || !(await module.Invoke(context, "shadow", tuple)).Equals(improper)
                || !(await module.Invoke(
                    context,
                    "unrelated",
                    tuple,
                    improperList
                )).Equals(improper)
                || !(await module.Invoke(
                    context,
                    "branch",
                    Term.A("false"),
                    tuple,
                    improperList
                )).Equals(improper))
            {
                throw new InvalidOperationException(WrongValue);
            }

            try
            {
                await module.Invoke(context, "tuple_tail", Term.Tuple());
                throw new InvalidOperationException(WrongValue);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A("badarg")))
            {
            }

            return Nil.Value;
        });
    }

    private static void CheckTypes(byte[] beam, bool ssaEnabled)
    {
        object code = Code(beam);
        object structure = Property(code, "Structure");
        object table = Call(Type("BeamTypeTableReader"), "Read", structure);
        object[] types = ((IEnumerable)Property(table, "Entries")).Cast<object>().ToArray();
        string[] atoms = ((IEnumerable)Property(structure, "Atoms")).Cast<string>().ToArray();
        object[] instructions = ((IEnumerable)Property(code, "Instructions")).Cast<object>().ToArray();
        int[] entries = Enumerable.Range(0, instructions.Length)
            .Where(index => Opcode(instructions[index]) == FunctionInfo).ToArray();

        Check("tuple_tail", BifOne, ssaEnabled);
        Check("tuple_head", BifOne, ssaEnabled);
        Check("atom_length", GcBifOne, ssaEnabled);
        Check("binary_length", GcBifOne, ssaEnabled);
        Check(
            "copied_length",
            GcBifOne,
            ssaEnabled,
            expectedOnStack: true
        );
        Check("shadow", BifOne, false);
        Check("dynamic", BifOne, false);
        Check("unrelated", BifOne, false);
        Check("branch", BifOne, false);

        void Check(
            string function,
            byte opcode,
            bool expectType,
            bool expectedOnStack = false
        )
        {
            int match = Array.FindIndex(entries, entry => atoms[(int)(BigInteger)Property(Operand(instructions[entry], 1), "Value")] == function);
            object operation = instructions[entries[match]..entries[match + 1]].Single(item => Opcode(item) == opcode);
            object operand = Operand(operation, opcode == GcBifOne ? 3 : 2);
            object? typeIndex = Property(operand, "TypeIndex");
            if ((expectType
                    ? typeIndex is not int index || (int)Property(types[index], "TypeUnion") != ListType
                    : typeIndex is not null)
                || expectedOnStack && (string)Property(operand, "Kind") != "y")
            {
                throw new InvalidOperationException(WrongOperand);
            }
        }
    }

    private static object Operand(object instruction, int position) =>
        ((IEnumerable)Property(instruction, "Operands")).Cast<object>().ElementAt(position);

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");
}
