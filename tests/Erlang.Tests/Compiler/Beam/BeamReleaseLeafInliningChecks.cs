using System.Buffers.Binary;
using Erlang;
using Erlang.Compiler;
using static Erlang.Tests.BeamDocumentationEmissionChecks;

namespace Erlang.Tests;

internal static class BeamReleaseLeafInliningChecks
{
    internal const string TestName = "compiler/beam/release-inline-bounded-leaf";
    private const string Module = "beam_release_inline_leaf";
    private const string Failure = "Release auto-inlining must substitute only the bounded leaf and honor the effective options.";
    private const string Source = """
        -module(beam_release_inline_leaf).
        %% options
        -export([value/1]).
        value(Input) -> pack(Input).
        pack(Argument) -> {ok, Argument}.
        """;

    private const string MapCallbackSource = """
        -module(beam_release_inline_leaf).
        -export([value/3]).
        value(Key, Map, Callback) -> lookup(Key, Map, Callback).
        lookup(Key, Map, Callback) ->
            case Map of
                #{Key := Found} -> Found;
                _ -> Callback(Key)
            end.
        """;

    internal static async Task Run()
    {
        var directory = Directory.CreateTempSubdirectory("erlang-beam-release-inline-leaf-");
        try
        {
            string path = Path.Combine(directory.FullName, Module + ".erl");
            var fallback = new BeamReleaseBuildProfile(
                "10.0.4",
                path,
                directory.FullName,
                [],
                ["inline"],
                EmitDocumentation: false
            );
            var explicitInline = fallback with { RecordedCompileOptions = [Term.A("inline")], WarningOptions = ["no_inline"] };
            var explicitNone = fallback with { RecordedCompileOptions = [] };
            var debugOnly = explicitNone with { RecordedDebugOptions = [Term.A("inline")] };
            var disabled = fallback with { RecordedCompileOptions = [Term.A("inline"), Term.A("no_inline")] };

            CheckLocalCount(Source, fallback, 0);
            CheckLocalCount(Source, explicitInline, 0);
            CheckLocalCount(Source, explicitNone, 1);
            CheckLocalCount(Source, debugOnly, 1);
            CheckLocalCount(Source, disabled, 1);
            CheckLocalCount(Source.Replace("%% options", "-compile(no_inline).", StringComparison.Ordinal), fallback, 1);
            CheckLocalCount(Source.Replace("pack(Input)", "pack(erlang:integer_to_binary(Input))", StringComparison.Ordinal), fallback, 1);
            CheckLocalCount(Source.Replace("{ok, Argument}", "erlang:integer_to_binary(Argument)", StringComparison.Ordinal), fallback, 1);
            CheckLocalCount(Source.Replace("pack(Input)", "{pack(Input), fun pack/1}", StringComparison.Ordinal), fallback, 1);
            CheckLocalCount(
                Source.Replace("-export([value/1])", "-export([value/1, pack/1])", StringComparison.Ordinal),
                fallback,
                0,
                4
            );

            var small = explicitInline with
            {
                RecordedCompileOptions = [Term.A("inline"), Term.Tuple(Term.A("inline_size"), Term.I(7))]
            };
            CheckLocalCount(MapCallbackSource, small, 1);
            CheckLocalCount(MapCallbackSource, disabled, 1);
            CheckLocalCount(
                MapCallbackSource.Replace(
                    "-> lookup(Key, Map, Callback).",
                    "-> lookup(erlang:integer_to_binary(Key), Map, Callback).",
                    StringComparison.Ordinal
                ),
                explicitInline,
                1
            );
            var inlinedMap = CheckLocalCount(MapCallbackSource, explicitInline, 0);
            await CheckMapCallback(inlinedMap);

            byte[] CheckLocalCount(
                string source,
                BeamReleaseBuildProfile profile,
                int expected,
                int? exports = null
            )
            {
                File.WriteAllText(path, source);
                byte[] beam = CodeGeneration.CompileBeamFile(path, buildProfile: profile);
                _ = BeamModule.Prepare(beam);
                var chunks = Chunks(beam);
                int locals = BinaryPrimitives.ReadInt32BigEndian(chunks.Single(chunk => chunk.Id == "LocT").Data);
                int exported = BinaryPrimitives.ReadInt32BigEndian(chunks.Single(chunk => chunk.Id == "ExpT").Data);
                if (locals != expected || exports is int exportCount && exported != exportCount)
                {
                    throw new InvalidOperationException(Failure);
                }

                return beam;
            }
        }
        finally
        {
            directory.Delete(recursive: true);
        }

    }

    private static async Task CheckMapCallback(byte[] bytes)
    {
        var image = BeamModule.Prepare(bytes);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term key = Term.A("key");
            Term value = Term.A("found");
            Term invalid = Term.A("not_a_fun");
            var map = new MapTerm([new(key, value)]);
            var fallback = Term.Tuple(Term.A("fallback"), key);
            int calls = 0;
            var callback = new FunctionTerm(1, (_, arguments) =>
            {
                calls++;

                return ValueTask.FromResult<Term>(Term.Tuple(Term.A("fallback"), arguments[0]));
            });

            if (!value.Equals(await image.Invoke(
                context,
                "value",
                key,
                map,
                invalid
            ))
                || !value.Equals(await image.Invoke(
                    context,
                    "value",
                    key,
                    map,
                    callback
                ))
                || calls != 0
                || !fallback.Equals(await image.Invoke(
                    context,
                    "value",
                    key,
                    new MapTerm([]),
                    callback
                ))
                || calls != 1
                || !fallback.Equals(await image.Invoke(
                    context,
                    "value",
                    key,
                    Nil.Value,
                    callback
                ))
                || calls != 2)
            {
                throw new InvalidOperationException(Failure);
            }

            try
            {
                await image.Invoke(
                    context,
                    "value",
                    key,
                    Nil.Value,
                    invalid
                );
                throw new InvalidOperationException(Failure);
            }
            catch (ErlangException exception) when (exception.Reason.Equals(Term.Tuple(Term.A("badfun"), invalid)))
            {
            }

            return Nil.Value;
        });
    }
}
