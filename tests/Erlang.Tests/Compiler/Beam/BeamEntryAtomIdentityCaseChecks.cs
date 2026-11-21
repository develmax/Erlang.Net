using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamEntryAtomIdentityCaseChecks
{
    internal const string TestName = "compiler/beam/shares-terminal-atom-identity-case-returns";
    private const string Fixture = "beam_literal_identity_case_probe.erl";
    private const string WrongLayout = "The atom identity case must use one shared return and retain its case failure.";
    private const string WrongResult = "The terminal case changed its result or rejected the wrong value.";

    private const string Source = """
        -module(beam_literal_identity_case_probe).
        -export([member/2, selected_second/2, guarded/2, singleton/1, boolean/1, mapped/1, explicit_default/1, inline/1]).

        member(Value, _) ->
            case Value of
                zulu -> zulu;
                alpha -> alpha;
                pi -> pi;
                'écho' -> 'écho'
            end.

        selected_second(_, Value) ->
            case Value of
                south -> south;
                north -> north
            end.

        guarded(Value, Flag) ->
            case Value of
                gamma when Flag =:= true -> gamma;
                delta -> delta;
                epsilon -> epsilon
            end.

        singleton(Value) ->
            case Value of single -> single end.

        boolean(Value) ->
            case Value of true -> true; false -> false end.

        mapped(Value) ->
            case Value of zulu -> other; alpha -> alpha end.

        explicit_default(Value) ->
            case Value of zulu -> zulu; alpha -> alpha; _ -> fallback end.

        inline(Value) -> case Value of alpha -> alpha; beta -> beta end.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        object code = Code(image);
        var instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        Check("member", ["alpha", "pi", "zulu", "écho"], failureLine: true);
        Check("inline", ["alpha", "beta"], failureLine: false);
        object[] singleton = Body("singleton");
        if (!singleton.Any(instruction => Opcode(instruction) == ExactEqualTest)
            || singleton.Any(instruction => Opcode(instruction) == SelectValue))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        BeamModule module = BeamModule.Prepare(image, new BeamDependencySelection([]));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term alpha = Term.A("alpha");
            Term gamma = Term.A("gamma");
            Term south = Term.A("south");
            Term echo = Term.A("écho");
            if (!echo.Equals(await module.Invoke(
                context,
                "member",
                echo,
                alpha
            ))
                || !south.Equals(await module.Invoke(
                    context,
                    "selected_second",
                    alpha,
                    south
                ))
                || !gamma.Equals(await module.Invoke(
                    context,
                    "guarded",
                    gamma,
                    Term.A("true")
                ))
                || !Term.A("other").Equals(await module.Invoke(context, "mapped", Term.A("zulu")))
                || !Term.A("fallback").Equals(await module.Invoke(context, "explicit_default", gamma)))
            {
                throw new InvalidOperationException(WrongResult);
            }

            try
            {
                await module.Invoke(
                    context,
                    "member",
                    gamma,
                    alpha
                );
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                && error.Reason.Equals(Term.Tuple(Term.A("case_clause"), gamma)))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(WrongResult);
        });

        void Check(string name, string[] expected, bool failureLine)
        {
            object[] operations = Body(name);
            object? selector = operations.SingleOrDefault(instruction => Opcode(instruction) == SelectValue);
            object? failure = operations.SingleOrDefault(instruction => Opcode(instruction) == CaseEnd);
            if (selector is null || failure is null || operations.Count(instruction => Opcode(instruction) == Return) != 1
                || operations.Any(instruction => Opcode(instruction) == ExactEqualTest))
            {
                throw new InvalidOperationException(WrongLayout);
            }

            object[] select = Operands(selector);
            int target = Value(select[4]);
            int failureLabel = Value(select[1]);
            if (Value(select[0]) != 0 || Value(Operands(failure)[0]) != 0 || target == failureLabel
                || !select.Skip(3).Where((_, index) => index % 2 == 0).Select(operand => atoms[Value(operand)]).SequenceEqual(expected)
                || select.Skip(4).Where((_, index) => index % 2 == 0).Any(operand => Value(operand) != target))
            {
                throw new InvalidOperationException(WrongLayout);
            }

            int entry = Array.IndexOf(operations, selector);
            int rejected = Array.IndexOf(operations, failure);
            if (operations.Skip(entry + 1).Take(rejected - entry - 1).Any(instruction => Opcode(instruction) == Line) != failureLine)
            {
                throw new InvalidOperationException(WrongLayout);
            }
        }

        object[] Body(string name)
        {
            int entry = Array.FindIndex(instructions, instruction =>
                Opcode(instruction) == FunctionInfo && atoms[Value(Operands(instruction)[1])] == name);
            if (entry < 0)
            {
                throw new InvalidOperationException(WrongLayout);
            }

            return instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Operands(object instruction) => Items(instruction, "Operands").ToArray();

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
