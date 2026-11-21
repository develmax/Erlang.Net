using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamAutomaticClosureIdentityChecks
{
    internal const string TestName = "compiler/beam/uses-ssa-closure-identities-after-automatic-inlining";
    private const string Module = "beam_automatic_closure_identity";
    private const string Fixture = Module + ".erl";
    private const string IncorrectName = "The escaping closure retained the wrong Core or SSA function identifier for its inlining options.";
    private const string IncorrectValue = "The escaping callback did not retain its captured value.";
    private const string Source = """
        -module(beam_automatic_closure_identity).
        -export([escape/2]).
        escape(Extra, Values) ->
            lists:map(fun(Value) -> wrap(Extra, Value) end, Values).
        wrap(Extra, Value) ->
            {Extra, Value}.
        """;

    private const string ReturnedSource = """
        -module(beam_automatic_closure_identity).
        -export([escape/2, invoke/2]).

        escape(Extra, Values) ->
            {fun(Value) -> wrap(Extra, Value) end, Values}.

        invoke(Callback, [First,Second]) ->
            [Callback(First), Callback(Second)].

        wrap(Extra, Value) ->
            {Extra, Value}.
        """;

    private const string ScopedCallbacks = """
        -module(beam_automatic_closure_identity).
        -export([incoming/1, check/1, select/2]).

        incoming({Value, Callbacks}) ->
            lists:all(fun(Callback) -> Callback(Value) end, Callbacks).

        check(Value) ->
            incoming({Value, [fun accepted/1]}).

        select(nomatch, _Values) ->
            false;
        select(Selected, Values) ->
            lists:any(fun(Value) -> Value =:= Selected end, Values).

        accepted(Value) -> Value =:= allowed.
        """;

    private const string PredicateValues = """
        -module(beam_automatic_closure_identity).
        -import(lists, [all/2, any/2]).
        -export([decide/1, tagged/1]).

        decide({options, Values}) ->
            case all(fun(Value) -> Value =:= off end, Values) of
                true -> disabled;
                false -> any(fun(on) -> true; (_) -> false end, Values)
            end.

        tagged(Values) ->
            any(fun({inline, _}) -> true; (_) -> false end, Values).
        """;

    private const string OpaqueFoldValues = """
        -module(beam_automatic_closure_identity).
        -import(lists, [foldl/3]).
        -export([fold/3, check/1]).

        fold(Callback, Values, Seed) ->
            foldl(fun(Value, Acc) -> relay(Callback, Value, Acc) end, Seed, Values).

        relay(Callback, Value, Acc) ->
            invoke(Callback, Value, Acc).

        invoke(Callback, Value, Acc) when is_function(Callback, 2) ->
            Callback(Value, Acc);
        invoke(_, _, Acc) ->
            Acc.

        check(Values) ->
            fold(fun append_value/2, Values, seed).

        append_value(Value, Acc) ->
            {Acc, Value}.
        """;

    internal static async Task Run()
    {
        BeamModule automatic = Compile("-compile(inline).");
        RequireName(automatic, automatic: true);
        RequireName(Compile(""), automatic: false);
        RequireName(Compile("-compile([inline,no_inline])."), automatic: false);
        RequireName(Compile("-compile({inline,[{wrap,2}]})."), automatic: false);
        RequireName(Compile("-compile(inline).", ReturnedSource), automatic: true);
        RequireName(Compile("", ReturnedSource), automatic: false);

        BeamModule scopes = Compile("-compile(inline).", ScopedCallbacks);
        RequireScopedNames(scopes, automatic: true);
        RequireScopedNames(Compile("-compile([inline,no_inline]).", ScopedCallbacks), automatic: false);

        BeamModule predicateValues = Compile("-compile(inline).", PredicateValues);
        RequirePredicateNames(predicateValues, automatic: true);
        RequirePredicateNames(Compile("-compile([inline,no_inline]).", PredicateValues), automatic: false);

        BeamModule opaqueFold = Compile("-compile(inline).", OpaqueFoldValues);
        RequireFoldName(opaqueFold, automatic: true);
        RequireFoldName(Compile("-compile([inline,no_inline]).", OpaqueFoldValues), automatic: false);

        var directory = Directory.CreateTempSubdirectory("erlang-automatic-closure-");
        try
        {
            string path = Path.Combine(directory.FullName, Fixture);
            File.WriteAllText(path, Source);
            var profile = new BeamReleaseBuildProfile(
                "10.0.6",
                path,
                directory.FullName,
                [],
                [],
                RecordedCompileOptions: [Term.A(CompilerOptionNames.Inline)]
            );
            RequireName(BeamModule.Prepare(CodeGeneration.CompileBeamFile(path, buildProfile: profile)), automatic: true);
        }
        finally
        {
            directory.Delete(recursive: true);
        }

        var selection = new RuntimeLibrarySelection(
            [RuntimeModuleSelection.Managed("erlang"), RuntimeModuleSelection.Managed("lists")]
        );
        await using var runtime = await ProcessRuntime.CreateAsync(selection, RuntimeNativePrimitives.InternalScalars413, CancellationToken.None);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term captured = Term.A("captured");
            Term actual = await automatic.Invoke(
                context,
                "escape",
                captured,
                Term.List(Term.I(1), Term.I(2))
            );
            Term expected = Term.List(Term.Tuple(captured, Term.I(1)), Term.Tuple(captured, Term.I(2)));
            if (!actual.Equals(expected)
                || !Term.A("true").Equals(await scopes.Invoke(context, "check", Term.A("allowed")))
                || !Term.A("false").Equals(await scopes.Invoke(context, "check", Term.A("blocked")))
                || !Term.A("true").Equals(await scopes.Invoke(
                    context,
                    "select",
                    captured,
                    Term.List(Term.A("other"), captured)
                ))
                || !Term.A("false").Equals(await scopes.Invoke(
                    context,
                    "select",
                    Term.A("nomatch"),
                    Term.List(Term.A("nomatch"))
                )))
            {
                throw new InvalidOperationException(IncorrectValue);
            }

            Term options = Term.A("options");
            if (!Term.A("disabled").Equals(await predicateValues.Invoke(
                    context,
                    "decide",
                    Term.Tuple(options, Term.List(Term.A("off")))
                ))
                || !Term.A("true").Equals(await predicateValues.Invoke(
                    context,
                    "decide",
                    Term.Tuple(options, Term.List(Term.A("off"), Term.A("on")))
                ))
                || !Term.A("true").Equals(await predicateValues.Invoke(
                    context,
                    "tagged",
                    Term.List(Term.Tuple(Term.A("inline"), Term.A("target")))
                )))
            {
                throw new InvalidOperationException(IncorrectValue);
            }

            Term folded = await opaqueFold.Invoke(context, "check", Term.List(Term.A("one"), Term.A("two")));
            Term ordered = Term.Tuple(Term.Tuple(Term.A("seed"), Term.A("one")), Term.A("two"));
            if (!ordered.Equals(folded))
            {
                throw new InvalidOperationException(IncorrectValue);
            }

            return Nil.Value;
        });
    }

    private static BeamModule Compile(string options, string original = Source)
    {
        string source = original.Replace("-export(", options + "\n-export(", StringComparison.Ordinal);

        return BeamModule.Prepare(
            CodeGeneration.CompileBeamModule(source, Fixture),
            new BeamDependencySelection(["lists"])
        );
    }

    private static void RequireFoldName(BeamModule module, bool automatic)
    {
        string identity = automatic ? "-fold/3-anonymous-0-" : "-fold/3-fun-0-";
        if (!module.Functions.Any(function => function.Name == identity && function.Arity == 3))
        {
            throw new InvalidOperationException(IncorrectName);
        }
    }

    private static void RequirePredicateNames(BeamModule module, bool automatic)
    {
        string identity = automatic ? "-anonymous-" : "-fun-";
        if (!module.Functions.Any(function => function.Name == "-decide/1" + identity + "0-" && function.Arity == 1)
            || !module.Functions.Any(function => function.Name == "-decide/1" + identity + "1-" && function.Arity == 1)
            || !module.Functions.Any(function => function.Name == "-tagged/1" + identity + "0-" && function.Arity == 1))
        {
            throw new InvalidOperationException(IncorrectName);
        }
    }

    private static void RequireScopedNames(BeamModule module, bool automatic)
    {
        string suffix = automatic ? "-anonymous-0-" : "-fun-0-";
        if (!module.Functions.Any(function => function.Name == "-incoming/1" + suffix && function.Arity == 2)
            || !module.Functions.Any(function => function.Name == "-select/2" + suffix && function.Arity == 2))
        {
            throw new InvalidOperationException(IncorrectName);
        }
    }

    private static void RequireName(BeamModule module, bool automatic)
    {
        string expected = automatic ? "-escape/2-anonymous-0-" : "-escape/2-fun-0-";
        string[] closures = module.Functions.Where(function => function.Name.StartsWith("-escape/2-", StringComparison.Ordinal))
            .Select(function => function.Name).ToArray();
        if (closures is not [var name] || name != expected)
        {
            throw new InvalidOperationException(IncorrectName);
        }
    }
}
