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

    internal static async Task Run()
    {
        BeamModule automatic = Compile("-compile(inline).");
        RequireName(automatic, automatic: true);
        RequireName(Compile(""), automatic: false);
        RequireName(Compile("-compile([inline,no_inline])."), automatic: false);
        RequireName(Compile("-compile({inline,[{wrap,2}]})."), automatic: false);
        RequireName(Compile("-compile(inline).", ReturnedSource), automatic: true);
        RequireName(Compile("", ReturnedSource), automatic: false);

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
            if (!actual.Equals(expected))
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
