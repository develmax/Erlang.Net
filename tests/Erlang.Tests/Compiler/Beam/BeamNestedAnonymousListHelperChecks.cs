using System.Collections;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamNestedAnonymousListHelperChecks
{
    internal const string TestName = "compiler/beam/keeps-nested-list-helper-in-lexical-owner";
    private const string Fixture = "beam_nested_anonymous_list_helper.erl";
    private const string Helper = "-map/2-lc$^0/1-0-";
    private const string ShadowHelper = "-shadow/2-lc$^0/1-0-";
    private const string WrongIdentity = "The callback list helper changed its enclosing function, capture arity or lifted order.";
    private const string WrongValue = "The callback list helper changed list order, lexical scope or generator failure.";
    private const string Source = """
        -module(beam_nested_anonymous_list_helper).
        %% options
        -export([map/2, shadow/2]).

        map(Source, Tag) ->
            dispatch(fun(Value) -> mark(Value, left) end,
                     fun(Input) -> [mark(Item, Tag) || Item <- Input] end,
                     Source).

        shadow(Source, Tag) ->
            dispatch(fun(Value) -> mark(Value, Tag) end,
                     fun(Tag) -> [mark(Item, Tag) || Item <- Tag] end,
                     Source).

        dispatch(Other, Mapper, Input) -> {Other(sibling), Mapper(Input)}.
        mark(Value, Tag) -> {Value, Tag}.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        var functions = ReadFunctions(image);
        int mappingClosure = Array.FindIndex(functions, function => function == ("-map/2-fun-1-", 2));
        int sibling = Array.FindIndex(functions, function => function == ("-map/2-fun-2-", 1));
        int mappingHelper = Array.FindIndex(functions, function => function == (Helper, 2));
        if (mappingClosure < 0 || mappingHelper != mappingClosure + 1 || sibling != mappingHelper + 1
            || !functions.Contains((ShadowHelper, 2)))
        {
            throw new InvalidOperationException(WrongIdentity);
        }

        var noSsaSource = new Parser(
            Source.Replace("%% options", "-compile(no_ssa_opt).", StringComparison.Ordinal)
        ).ParseModule();
        var locations = Enumerable.Range(1, noSsaSource.DeclaredFunctions.Count)
            .Select(line => new BeamSourceLocation(null, line)).ToArray();
        byte[] noSsa = new BeamModuleCompiler(noSsaSource.Name, sourceModule: noSsaSource).Emit(
            noSsaSource.DeclaredFunctions,
            locations,
            noSsaSource.DeclaredExports.ToHashSet(),
            new BeamModuleMetadata(noSsaSource.Attributes, null)
        );
        byte[] guarded = CodeGeneration.CompileBeamModule(
            Source.Replace("fun(Input) ->", "fun(Input) when is_list(Input) ->", StringComparison.Ordinal),
            Fixture
        );
        if (ReadFunctions(noSsa).Any(function => function.Name == Helper)
            || ReadFunctions(guarded).Any(function => function.Name == Helper))
        {
            throw new InvalidOperationException(WrongIdentity);
        }

        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term first = Term.A("first");
            Term second = Term.A("second");
            Term tag = Term.A("tag");
            Term siblingValue = Term.A("sibling");
            Term values = Term.List(first, second);
            Term mapped = Term.Tuple(
                Term.Tuple(siblingValue, Term.A("left")),
                Term.List(Term.Tuple(first, tag), Term.Tuple(second, tag))
            );
            Term shadowed = Term.Tuple(
                Term.Tuple(siblingValue, tag),
                Term.List(Term.Tuple(first, values), Term.Tuple(second, values))
            );
            if (!mapped.Equals(await module.Invoke(
                context,
                "map",
                values,
                tag
            ))
                || !shadowed.Equals(await module.Invoke(
                    context,
                    "shadow",
                    values,
                    tag
                )))
            {
                throw new InvalidOperationException(WrongValue);
            }

            Term bad = Term.A("bad");
            try
            {
                _ = await module.Invoke(
                    context,
                    "map",
                    new Cons(first, bad),
                    tag
                );
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A(ErlangErrorReasons.BadGenerator), bad))
                && error.StackTraceTerm is Cons { Head: TupleTerm { Items: [_, Atom { Name: Helper }, _, _] } })
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(WrongValue);
        });
    }

    private static (string Name, int Arity)[] ReadFunctions(byte[] image) =>
        ((IEnumerable)Property(Code(image), "Functions")).Cast<object>()
        .Select(function => ((string)Property(function, "Name"), (int)Property(function, "Arity"))).ToArray();
}
