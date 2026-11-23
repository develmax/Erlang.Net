using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamGeneratedListResultChecks
{
    internal const string TestName = "compiler/beam/generated-list-result-types-its-local-consumer";
    private const int ListType = (1 << 2) | (1 << 7);
    private const string WrongOperand = "Expected the list result type only for the consumer of the generated helper.";
    private const string WrongValue = "The generated list and its consumer returned an unexpected value or error.";
    private const string SourceFile = "lc_result_probe.erl";
    private const string Source = """
        -module(lc_result_probe).
        -export([nil_check/1, value_length/1, copied_length/1, discarded/1,
                 appended/2, merged/3, genuine_local/2]).

        nil_check(Input) ->
            case [X || X <- Input, is_atom(X)] of
                [] -> empty;
                Result -> {items,Result}
            end.

        value_length(Input) -> length([X || X <- Input, is_integer(X)]).

        copied_length(Input) ->
            Result = [X || X <- Input, is_atom(X)],
            {Result,length(Result)}.

        discarded(Input) ->
            [erlang:put(lc_result_probe,X) || X <- Input],
            length(Input).

        appended(Input,Tail) -> length([X || X <- Input, is_atom(X)] ++ Tail).

        merged(Tag,Input,Other) ->
            Result = case Tag of generated -> [X || X <- Input, is_atom(X)]; other -> Other end,
            length(Result).

        genuine_local(Tag,Input) -> length(user_result(Tag,Input)).
        user_result(keep,Input) -> Input;
        user_result(drop,_) -> [].
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
            Term atom = Term.A("value");
            Term nonempty = Term.List(atom);
            Term mixed = Term.List(atom, Term.I(2));
            if (!(await module.Invoke(context, "nil_check", Nil.Value)).Equals(Term.A("empty"))
                || !(await module.Invoke(context, "nil_check", mixed)).Equals(Term.Tuple(Term.A("items"), nonempty))
                || !(await module.Invoke(context, "value_length", mixed)).Equals(Term.I(1))
                || !(await module.Invoke(context, "copied_length", mixed)).Equals(Term.Tuple(nonempty, Term.I(1)))
                || !(await module.Invoke(context, "discarded", mixed)).Equals(Term.I(2)))
            {
                throw new InvalidOperationException(WrongValue);
            }

            try
            {
                await module.Invoke(
                    context,
                    "appended",
                    mixed,
                    atom
                );
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

        Check("nil_check", IsNil, ssaEnabled);
        Check("value_length", GcBifOne, ssaEnabled);
        Check("copied_length", GcBifOne, ssaEnabled);
        Check("discarded", GcBifOne, false);
        Check("appended", GcBifOne, false);
        Check("merged", GcBifOne, false);
        Check("genuine_local", GcBifOne, false);

        void Check(string function, byte opcode, bool expectType)
        {
            int match = Array.FindIndex(entries, entry => atoms[(int)(BigInteger)Property(Operand(instructions[entry], 1), "Value")] == function);
            object operation = instructions[entries[match]..entries[match + 1]].Single(item => Opcode(item) == opcode);
            object operand = Operand(operation, opcode == GcBifOne ? 3 : 1);
            object? typeIndex = Property(operand, "TypeIndex");
            if (expectType
                ? typeIndex is not int index || (int)Property(types[index], "TypeUnion") != ListType
                : typeIndex is not null)
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
