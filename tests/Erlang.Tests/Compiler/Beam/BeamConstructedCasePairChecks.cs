using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamConstructedCasePairChecks
{
    internal const string TestName = "compiler/beam/binds-unboxed-pair-results-of-a-constructed-case";
    private const string File = "beam_constructed_case_pair.erl";
    private const string WrongShape = "The case constructed or matched a pair whose two members are already known.";
    private const string WrongValue = "The case pair changed the returned value, effect order, or failure.";
    private const string Source = """
        -module(beam_constructed_case_pair).
        -export([run/1, opaque/2, observed/0]).

        run(Input) ->
            {First, Second} = case Input of
                [] -> {1, [1]};
                [N | Rest] -> {touch(N), [1, N+1 | Rest]}
            end,
            {ok, First, Second}.

        opaque(Input, Fallback) ->
            {First, Second} = case Input of
                [] -> {1, [1]};
                [_ | _] -> Fallback
            end,
            {ok, First, Second}.

        touch(N) -> erlang:put(observed, N), N.
        observed() -> erlang:get(observed).
        """;

    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, File);
        object code = Code(beam);
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        byte[] positive = Body("run");
        byte[] unknown = Body("opaque");
        if (positive.Count(op => op == PutTuple) != 1 || positive.Contains(BadMatch)
            || positive.Contains(TestArity) || !positive.Contains(CaseEnd)
            || !unknown.Contains(BadMatch))
        {
            throw new InvalidOperationException(WrongShape);
        }

        BeamModule module = BeamModule.Prepare(beam);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term one = Term.I(1);
            Term three = Term.I(3);
            Term four = Term.I(4);
            Term five = Term.I(5);
            Term ok = Term.A("ok");
            Term wrong = Term.A("invalid");
            if (!Term.Tuple(ok, one, Term.List(one)).Equals(await module.Invoke(context, "run", Nil.Value))
                || !Term.Tuple(ok, three, Term.List(one, four, five))
                    .Equals(await module.Invoke(context, "run", Term.List(three, five)))
                || !three.Equals(await module.Invoke(context, "observed")))
            {
                throw new InvalidOperationException(WrongValue);
            }

            await HasFailure("run", [wrong], Term.Tuple(Term.A("case_clause"), wrong));
            await HasFailure("opaque", [Term.List(one), wrong], Term.Tuple(Term.A("badmatch"), wrong));
            await HasFailure("run", [Term.List(wrong)], Term.A("badarith"));
            if (!wrong.Equals(await module.Invoke(context, "observed")))
            {
                throw new InvalidOperationException(WrongValue);
            }

            return Nil.Value;

            async Task HasFailure(string function, Term[] arguments, Term reason)
            {
                try
                {
                    _ = await module.Invoke(context, function, arguments);
                }
                catch (ErlangException error) when (error.Reason.Equals(reason))
                {
                    return;
                }

                throw new InvalidOperationException(WrongValue);
            }
        });

        byte[] Body(string name)
        {
            int info = Array.FindIndex(
                instructions,
                instruction => Opcode(instruction) == FunctionInfo
                    && atoms[(int)(BigInteger)Property(Items(instruction, "Operands").ElementAt(1), "Value")] == name
            );

            return info < 0 ? [] : instructions.Skip(info + 1).Select(Opcode).TakeWhile(op => op != FunctionInfo).ToArray();
        }
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static IEnumerable<object> Items(object instance, string name) => ((IEnumerable)Property(instance, name)).Cast<object>();
}
