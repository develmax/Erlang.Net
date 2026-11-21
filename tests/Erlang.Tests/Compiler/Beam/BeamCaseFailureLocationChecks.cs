using System.Collections;
using System.Numerics;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamCaseFailureLocationChecks
{
    internal const string TestName = "compiler/beam/case-clause-uses-the-source-case-location";
    private const string Fixture = "beam_case_failure_position.erl";
    private const string MappedFile = "mapped_case.yrl";
    private const string WrongResult = "A case result or its failure location differed from the source case.";
    private const string CaseClause = "case_clause";
    private const string Source = """
        -module(beam_case_failure_position).
        -export([pick/1, called/1, nested/1, covered/1, mapped/1]).
        pick(Value) ->
            case Value of
                {one, Out} when is_integer(Out) -> Out;
                #{other := Out} -> Out
            end.

        called(Value) ->
            case
                identity(Value)
            of
                {one, Out} when is_integer(Out) -> Out;
                #{other := Out} -> Out
            end.

        nested(Value) ->
            case Value of
                {inner, Child} ->
                    case Child of
                        {one, Result} -> Result;
                        #{other := Result} -> Result
                    end;
                fallback -> ok
            end.

        covered(Value) ->
            case Value of
                {one, Out} -> Out;
                _ -> none
            end.

        -file("mapped_case.yrl", 200).
        mapped(Value) ->
            case Value of
                {one, Result} when is_integer(Result) -> Result;
                #{other := Result} -> Result
            end.

        identity(Value) -> erlang:put(position_observed, Value), Value.
        """;

    private const string BooleanFixture = "beam_boolean_case_line_probe.erl";
    private const string BooleanSource = """
        -module(beam_boolean_case_line_probe).
        -export([nested/2, same_line/2, exhausted/2, init/1]).
        init(Value) -> erlang:put(selector,Value).
        nested({tag,N}, Input) ->
            case Input of
                [{Item,_,_}] ->
                    case
                        choose(Item) of
                        false -> [local(Item,N)];
                        true -> erlang:put(case_branch,N)
                    end;
                _ -> fallback
            end;
        nested(Old, Input) ->
            State = convert(Old),
            nested(State, Input).
        same_line(Item, N) -> case choose(Item) of false -> [local(Item,N)]; true -> erlang:put(case_branch,N) end.
        exhausted(Item, N) -> case choose(Item) of
            false -> [local(Item,N)];
            true -> erlang:put(case_branch,N);
            _ -> fallback
        end.
        choose(_) -> erlang:get(selector).
        local(Item,N) -> {Item,N}.
        convert(Old) -> {tag,Old}.
        """;

    internal static async Task Run()
    {
        var module = BeamModule.Prepare(CodeGeneration.CompileBeamModule(Source, Fixture));
        byte[] booleanCode = CodeGeneration.CompileBeamModule(BooleanSource, BooleanFixture);
        CheckColdBooleanFailure(booleanCode);
        var booleanModule = BeamModule.Prepare(booleanCode);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            var wrong = Term.A("wrong");
            await CheckFailure(
                "pick",
                wrong,
                Fixture,
                4
            );
            await CheckFailure(
                "called",
                wrong,
                Fixture,
                10
            );
            await CheckFailure(
                "nested",
                Term.Tuple(Term.A("inner"), wrong),
                Fixture,
                20
            );
            await CheckFailure(
                "nested",
                wrong,
                Fixture,
                18
            );
            await CheckFailure(
                "mapped",
                wrong,
                MappedFile,
                202
            );

            Term matched = Term.Tuple(Term.A("one"), Term.I(5));
            if (!(await module.Invoke(context, "pick", matched)).Equals(Term.I(5))
                || !(await module.Invoke(context, "covered", wrong)).Equals(Term.A("none")))
            {
                throw new InvalidOperationException(WrongResult);
            }

            await booleanModule.Invoke(context, "init", wrong);
            Term input = Term.A("item");
            Term bound = Term.I(2);
            await CheckBooleanFailure(
                "nested",
                [Term.Tuple(Term.A("tag"), bound), Term.List(Term.Tuple(input, wrong, wrong))],
                7
            );
            await CheckBooleanFailure("same_line", [input, bound], 17);
            if (!(await booleanModule.Invoke(
                context,
                "exhausted",
                input,
                bound
            )).Equals(Term.A("fallback")))
            {
                throw new InvalidOperationException(WrongResult);
            }

            return Nil.Value;

            async Task CheckBooleanFailure(string function, Term[] arguments, int line)
            {
                try
                {
                    _ = await booleanModule.Invoke(context, function, arguments);
                }
                catch (ErlangException error)
                {
                    if (!error.Reason.Equals(Term.Tuple(Term.A(CaseClause), wrong))
                        || error.StackTraceTerm is not Cons { Head: TupleTerm { Items: [_, Atom name, _, var position] } }
                        || name.Name != function
                        || !Cons.Items(position).Contains(Term.Tuple(Term.A("file"), Term.String(BooleanFixture)))
                        || !Cons.Items(position).Contains(Term.Tuple(Term.A("line"), Term.I(line))))
                    {
                        throw new InvalidOperationException(WrongResult, error);
                    }

                    return;
                }

                throw new InvalidOperationException(WrongResult);
            }

            async Task CheckFailure(
                string function,
                Term input,
                string file,
                int line
            )
            {
                try
                {
                    _ = await module.Invoke(context, function, input);
                }
                catch (ErlangException error)
                {
                    if (!error.Reason.Equals(Term.Tuple(Term.A(CaseClause), wrong))
                        || error.StackTraceTerm is not Cons { Head: TupleTerm { Items: [_, Atom name, _, var position] } }
                        || name.Name != function
                        || !Cons.Items(position).Contains(Term.Tuple(Term.A("file"), Term.String(file)))
                        || !Cons.Items(position).Contains(Term.Tuple(Term.A("line"), Term.I(line))))
                    {
                        throw new InvalidOperationException(WrongResult, error);
                    }

                    return;
                }

                throw new InvalidOperationException(WrongResult);
            }
        });
    }

    private static void CheckColdBooleanFailure(byte[] beam)
    {
        object code = Code(beam);
        string[] atoms = Children(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Children(code, "Instructions").ToArray();
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
            && atoms[(int)(BigInteger)Property(Children(instruction, "Operands").ElementAt(1), "Value")] == "nested"
        );
        byte[] operations = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
            .Select(Opcode).ToArray();
        int cold = Array.IndexOf(operations, CaseEnd);
        if (cold < 2 || cold != operations.Length - 3
            || operations[cold - 1] != Line || operations[cold - 2] != Label
            || !operations.Take(cold).Contains(TailLocalCallWithFrame)
            || Operations("exhausted").Contains(CaseEnd))
        {
            throw new InvalidOperationException(WrongResult);
        }

        byte[] sameLine = Operations("same_line");
        int otherFailure = Array.IndexOf(sameLine, CaseEnd);
        if (otherFailure < 1 || sameLine[otherFailure - 1] != Label)
        {
            throw new InvalidOperationException(WrongResult);
        }

        byte[] Operations(string function)
        {
            int start = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                && atoms[(int)(BigInteger)Property(Children(instruction, "Operands").ElementAt(1), "Value")] == function
            );

            return instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
                .Select(Opcode).ToArray();
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Children(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>();

}
