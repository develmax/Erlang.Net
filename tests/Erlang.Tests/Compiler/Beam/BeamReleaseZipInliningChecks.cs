using Erlang;
using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamReleaseZipInliningChecks
{
    internal const string TestName = "compiler/beam/keeps-a-release-zip-call-boundary";
    private const string Module = "beam_release_zip_inline";
    private const string Zip = "zip";
    private const string RunFunction = "run";
    private const string WrongBinding = "The release ZIP call changed the caller bindings, row operands, or mismatch payload.";
    private const string WrongProfile = "The release ZIP mapper lost its call boundary before inline helper lowering was available.";
    private const string Source = """
        -module(beam_release_zip_inline).
        %% options
        -export([run/2]).
        run(A, B) -> {A, zip(A, B), B}.
        zip(Left, Right) -> [erlang:element(A, B) || A <- Left && B <- Right].
        """;

    internal static async Task Run()
    {
        var directory = Directory.CreateTempSubdirectory("erlang-beam-release-zip-inline-");
        try
        {
            string path = Path.Combine(directory.FullName, Module + ".erl");
            var profile = new BeamReleaseBuildProfile(
                "10.0.4",
                path,
                directory.FullName,
                [],
                [],
                RecordedCompileOptions: [Term.A("inline")],
                EmitDocumentation: false
            );
            BeamModule inline = Compile(Source, profile);
            BeamModule separate = Compile(Source, profile with { RecordedCompileOptions = [] });
            BeamModule disabled = Compile(
                Source,
                profile with { RecordedCompileOptions = [Term.A("inline"), Term.A("no_inline")] }
            );
            BeamModule sourceInline = Compile(
                Source.Replace("%% options", "-compile(inline).", StringComparison.Ordinal),
                profile with { RecordedCompileOptions = [] }
            );
            BeamModule deferredArgument = Compile(
                Source.Replace("zip(A, B)", "zip(hd(A), B)", StringComparison.Ordinal),
                profile
            );
            if (!HasLocalZip(inline) || !HasLocalZip(separate) || !HasLocalZip(disabled)
                || !HasLocalZip(sourceInline) || !HasLocalZip(deferredArgument))
            {
                throw new InvalidOperationException(WrongProfile);
            }

            await using var runtime = new ProcessRuntime();
            await using var actor = new RuntimeNodeFoundationHostActor(runtime);
            await actor.InvokeAsync(async context =>
            {
                Term left = Term.List(Term.I(2), Term.I(1));
                Term first = Term.Tuple(Term.A("a"), Term.A("b"));
                Term second = Term.Tuple(Term.A("c"), Term.A("d"));
                Term right = Term.List(first, second);
                Term expected = Term.Tuple(left, Term.List(Term.A("b"), Term.A("c")), right);
                Term shortRight = Term.List(first);
                Term reason = Term.Tuple(
                    Term.A(ComprehensionErrorReasons.BadGenerators),
                    Term.Tuple(Term.List(Term.I(1)), Nil.Value)
                );
                foreach (BeamModule module in new[] { inline, separate, sourceInline })
                {
                    if (!expected.Equals(await module.Invoke(
                        context,
                        RunFunction,
                        left,
                        right
                    )))
                    {
                        throw new InvalidOperationException(WrongBinding);
                    }

                    try
                    {
                        await module.Invoke(
                            context,
                            RunFunction,
                            left,
                            shortRight
                        );
                    }
                    catch (ErlangException error) when (error.Reason.Equals(reason))
                    {
                        continue;
                    }

                    throw new InvalidOperationException(WrongBinding);
                }

                return Nil.Value;
            });

            BeamModule Compile(string text, BeamReleaseBuildProfile buildProfile)
            {
                File.WriteAllText(path, text);

                return BeamModule.Prepare(CodeGeneration.CompileBeamFile(path, buildProfile: buildProfile));
            }
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private static bool HasLocalZip(BeamModule module) => module.Functions.Any(function => function.Name == Zip);
}
