using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamClosedIntegerListCaseChecks
{
    internal const string TestName = "compiler/beam/shares-prefix-tests-in-closed-integer-list-cases";
    private const string Fixture = "beam_closed_integer_list_case_probe.erl";
    private const string WrongLayout = "The closed list selector must share its prefix and preserve two original failure values.";
    private const string WrongResult = "The closed list selector changed its successful result or rejected the wrong value.";
    private const string Source = """
        -module(beam_closed_integer_list_case_probe).
        -export([decode/2, mixed_prefix/2, guarded/2, defaulted/2]).

        decode(Scope, Value) ->
            case Scope of
                read ->
                    case Value of
                        [4,5] -> five;
                        [4,2] -> two;
                        [4,9] -> nine
                    end
            end.

        mixed_prefix(Scope, Value) ->
            case Scope of
                read ->
                    case Value of
                        [4,5] -> five;
                        [8,2] -> two
                    end
            end.

        guarded(Scope, Value) ->
            case Scope of
                read ->
                    case Value of
                        [4,5] when Scope =/= read -> five;
                        [4,2] -> two
                    end
            end.

        defaulted(Scope, Value) ->
            case Scope of
                read ->
                    case Value of
                        [4,5] -> five;
                        [4,2] -> two;
                        _ -> other
                    end
            end.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        object code = Code(image);
        object[] instructions = Items(code, "Instructions").ToArray();
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        int entry = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && atoms[Value(Items(instruction, "Operands").ElementAt(1))] == "decode"
        );
        object[] body = instructions.Skip(entry + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo)
            .Where(instruction => Opcode(instruction) is not (Label or Line))
            .ToArray();
        if (entry < 0 || !body.Select(Opcode).SequenceEqual(new byte[]
            {
                ExactEqualTest, IsNonemptyList, GetList, ExactEqualTest, IsNonemptyList, GetList, SelectValue,
                IsNil, Move, Return, IsNil, Move, Return, IsNil, Move, Return, CaseEnd, CaseEnd
            })
            || !body.TakeLast(2).Select(instruction => Value(Items(instruction, "Operands").Single())).SequenceEqual([1, 0]))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        BeamModule module = BeamModule.Prepare(image, new BeamDependencySelection([]));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term read = Term.A("read");
            Term other = Term.A("other");
            Term five = Term.A("five");
            Term two = Term.A("two");
            Term good = Term.List(Term.I(4), Term.I(5));
            if (!five.Equals(await module.Invoke(
                context,
                "decode",
                read,
                good
            ))
                || !two.Equals(await module.Invoke(
                    context,
                    "decode",
                    read,
                    Term.List(Term.I(4), Term.I(2))
                ))
                || !two.Equals(await module.Invoke(
                    context,
                    "mixed_prefix",
                    read,
                    Term.List(Term.I(8), Term.I(2))
                ))
                || !other.Equals(await module.Invoke(
                    context,
                    "defaulted",
                    read,
                    Term.I(42)
                )))
            {
                throw new InvalidOperationException(WrongResult);
            }

            await Reject(
                "decode",
                other,
                good,
                other
            );
            foreach (Term rejected in new[]
            {
                Nil.Value, Term.I(42), Term.List(Term.I(4)), Term.List(Term.I(8), Term.I(5)),
                Term.List(Term.I(4), Term.I(7)), Term.List(Term.I(4), Term.I(5), Term.I(8)),
                Term.List(new FloatTerm(4.0), Term.I(5)), Term.List(Term.I(4), new FloatTerm(5.0)),
                new Cons(Term.I(4), new Cons(Term.I(5), other))
            })
            {
                await Reject(
                    "decode",
                    read,
                    rejected,
                    rejected
                );
            }

            await Reject(
                "guarded",
                read,
                good,
                good
            );

            return Nil.Value;

            async Task Reject(
                string function,
                Term scope,
                Term value,
                Term reason
            )
            {
                try
                {
                    await module.Invoke(
                        context,
                        function,
                        scope,
                        value
                    );
                }
                catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                    && error.Reason.Equals(Term.Tuple(Term.A("case_clause"), reason)))
                {
                    return;
                }

                throw new InvalidOperationException(WrongResult);
            }
        });
    }

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
