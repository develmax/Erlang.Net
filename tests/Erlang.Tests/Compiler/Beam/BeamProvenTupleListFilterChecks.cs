using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamProvenTupleListFilterChecks
{
    internal const string TestName = "compiler/beam/filters-proven-tuple-list-before-allocating";
    private const string Fixture = "beam_proven_tuple_list_filter_probe.erl";
    private const string WrongLayout = "A proven tuple filter must place the field dispatch before the frame.";
    private const string WrongResult = "Tuple filtering changed a retained item, guard failure, or generator failure.";
    private const string Source = """
        -module(beam_proven_tuple_list_filter_probe).
        -export([from_producer/1, unknown/1, mixed/1, mixed_alias/1]).

        from_producer(Input) ->
            Tokens = make_tokens(Input),
            [Token || Token <- Tokens, element(1, Token) =/= blank,
                      element(1, Token) =/= hidden].

        unknown(Input) ->
            [Token || Token <- Input, element(1, Token) =/= blank,
                      element(1, Token) =/= hidden].

        mixed(Input) ->
            Tokens = mixed_tokens(Input),
            [Token || Token <- Tokens, element(2, Token) =/= blank,
                      element(2, Token) =/= hidden].

        mixed_alias(Input) ->
            Tokens = mixed_alias_tokens(Input),
            [Token || Token <- Tokens, element(2, Token) =/= blank,
                      element(2, Token) =/= hidden].

        make_tokens([]) -> [];
        make_tokens([Head | Tail]) ->
            case Head of
                gap -> [{blank,Head} | make_tokens(Tail)];
                mark -> [{hidden,Head,seen} | make_tokens(Tail)];
                {copy,Value} -> [{copy,Value} | make_tokens(Tail)];
                {keep,_,_} = Matched -> [Matched | make_tokens(Tail)];
                _ -> [{visible,Head} | make_tokens(Tail)]
            end.

        mixed_tokens([]) -> [];
        mixed_tokens([short | Tail]) -> [{short} | mixed_tokens(Tail)];
        mixed_tokens([Head | Tail]) -> [{token,Head} | mixed_tokens(Tail)].

        mixed_alias_tokens([]) -> [];
        mixed_alias_tokens([Head | Tail]) ->
            case Head of
                Short = {short} -> [Short | mixed_alias_tokens(Tail)];
                Matched = {keep,_} -> [Matched | mixed_alias_tokens(Tail)];
                Other -> [Other | mixed_alias_tokens(Tail)]
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
                && atoms[Value(Items(instruction, "Operands").ElementAt(1))]
                    .StartsWith("-from_producer/1-lc$", StringComparison.Ordinal)
        );
        object[] prefix = instructions.Skip(entry + 1)
            .Where(instruction => Opcode(instruction) is not (Label or Line)).Take(6).ToArray();
        if (entry < 0 || !prefix.Select(Opcode).SequenceEqual(new byte[]
            {
                IsNonemptyList, GetList, GetTupleElement, SelectValue, TailLocalCall, Allocate
            })
            || !Items(prefix[^1], "Operands").Select(Value).SequenceEqual([1, 2]))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        BeamModule module = BeamModule.Prepare(image, new BeamDependencySelection([]));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term visible = Term.A("visible");
            Term payload = Term.A("payload");
            Term kept = Term.Tuple(visible, payload);
            Term bad = Term.A("improper");
            Term copied = Term.Tuple(Term.A("copy"), payload);
            Term aliased = Term.Tuple(Term.A("keep"), payload, visible);
            Term keptAlias = Term.Tuple(Term.A("keep"), payload);
            if (!Nil.Value.Equals(await module.Invoke(context, "from_producer", Nil.Value))
                || !Term.List(copied, aliased, kept).Equals(await module.Invoke(
                    context,
                    "from_producer",
                    Term.List(
                        Term.A("gap"),
                        copied,
                        aliased,
                        payload,
                        Term.A("mark")
                    )
                ))
                || !Term.List(kept).Equals(await module.Invoke(
                    context,
                    "unknown",
                    Term.List(
                        Term.Tuple(),
                        bad,
                        Term.Tuple(Term.A("hidden"), payload),
                        kept
                    )
                ))
                || !Term.List(Term.Tuple(Term.A("token"), payload)).Equals(await module.Invoke(
                    context,
                    "mixed",
                    Term.List(Term.A("short"), Term.A("blank"), payload)
                ))
                || !Term.List(keptAlias).Equals(await module.Invoke(
                    context,
                    "mixed_alias",
                    Term.List(
                        Term.Tuple(Term.A("short")),
                        keptAlias,
                        Term.Tuple(Term.A("keep"), Term.A("hidden")),
                        bad
                    )
                )))
            {
                throw new InvalidOperationException(WrongResult);
            }

            try
            {
                await module.Invoke(context, "from_producer", new Cons(copied, bad));
                throw new InvalidOperationException(WrongResult);
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                && error.Reason.Equals(Term.A("function_clause")))
            {
                // The input fails before the producer establishes its tuple-list result.
            }

            try
            {
                await module.Invoke(context, "unknown", new Cons(kept, bad));
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                && error.Reason.Equals(Term.Tuple(Term.A("bad_generator"), bad)))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(WrongResult);
        });
    }

    private static byte Opcode(object instruction) =>
        (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static IEnumerable<object> Items(object owner, string name) =>
        ((IEnumerable)Property(owner, name)).Cast<object>();
}
