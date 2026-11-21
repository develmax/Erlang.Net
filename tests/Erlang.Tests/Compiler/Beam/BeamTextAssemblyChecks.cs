using Erlang.Compiler;
using static Erlang.Tests.BeamDocumentationEmissionChecks;

namespace Erlang.Tests;

internal static class BeamTextAssemblyChecks
{
    internal const string TestName = "compiler/beam/assembles-text-terms-with-optional-abstract-source";
    private const string Module = "beam_text_assembly";
    private const string FileName = Module + ".S";
    private const string StubName = Module + ".erl";
    private const string Mismatch = "The text assembly or abstract companion did not match the supported OTP contract.";
    private const string Assembly = """
        {module,beam_text_assembly}.
        {exports,[{pick,1}]}.
        {attributes,[]}.
        {labels,4}.
        {function,pick,1,2}.
        {label,1}.
        {func_info,{atom,beam_text_assembly},{atom,pick},1}.
        {label,2}.
        {test,is_eq_exact,{f,3},[{x,0},{integer,0}]}.
        {move,{atom,zero},{x,0}}.
        return.
        {label,3}.
        {move,{atom,other},{x,0}}.
        return.
        """;
    private const string Stub = """
        -module(beam_text_assembly).
        -moduledoc false.
        -export([pick/1]).
        -spec pick(term()) -> atom().
        pick(_) -> erlang:nif_error(stub).
        """;

    internal static async Task Run()
    {
        var temporary = Directory.CreateTempSubdirectory("erlang-text-assembly-");
        try
        {
            string asmPath = Path.Combine(temporary.FullName, FileName);
            string stubPath = Path.Combine(temporary.FullName, StubName);
            File.WriteAllText(asmPath, Assembly + "\n");
            File.WriteAllText(stubPath, Stub + "\n");
            var profile = new BeamReleaseBuildProfile(
                "10.0.6",
                asmPath,
                temporary.FullName,
                [],
                []
            );
            byte[] standalone = CodeGeneration.CompileBeamAssemblyFile(asmPath, profile);
            byte[] attached = CodeGeneration.CompileBeamAssemblyFile(asmPath, profile, stubPath);
            CheckMetadata(standalone, attached);

            var module = BeamModule.Prepare(attached);
            await using var runtime = new ProcessRuntime();
            await using var actor = new RuntimeNodeFoundationHostActor(runtime);
            await actor.InvokeAsync(async context =>
            {
                Term zero = await module.Invoke(context, "pick", Term.I(0));
                Term other = await module.Invoke(context, "pick", Term.A("any"));
                if (!zero.Equals(Term.A("zero")) || !other.Equals(Term.A("other")))
                {
                    throw new InvalidOperationException(Mismatch);
                }

                return Nil.Value;
            });

            Reject<CompileException>(Assembly.Replace("{f,3}", "{f,9}", StringComparison.Ordinal));
            Reject<CompileException>(Assembly.Replace("{integer,0}", "{integer,0+0}", StringComparison.Ordinal));
            Reject<NotSupportedException>(Assembly.Replace("return.", "not_an_opcode.", StringComparison.Ordinal));

            void Reject<TError>(string text) where TError : Exception
            {
                File.WriteAllText(asmPath, text);
                try
                {
                    _ = CodeGeneration.CompileBeamAssemblyFile(asmPath, profile);
                }
                catch (TError)
                {
                    return;
                }

                throw new InvalidOperationException(Mismatch);
            }
        }
        finally
        {
            temporary.Delete(recursive: true);
        }
    }

    private static void CheckMetadata(byte[] standalone, byte[] attached)
    {
        var plain = Chunks(standalone).ToDictionary(chunk => chunk.Id, chunk => chunk.Data);
        var abstracted = Chunks(attached).ToDictionary(chunk => chunk.Id, chunk => chunk.Data);
        if (plain.ContainsKey("Docs") || abstracted.ContainsKey("Docs")
            || !plain["Code"].AsSpan().SequenceEqual(abstracted["Code"])
            || ExternalTermFormat.Decode(RawEtf(abstracted["Dbgi"])) is not TupleTerm
            {
                Items: [_, _, TupleTerm { Items: [var forms, var options] }]
            }
            || !abstracted["Dbgi"].AsSpan().SequenceEqual(RawEtf(abstracted["Dbgi"]))
            || !options.Equals(Term.List(Term.A("debug_info")))
            || !Cons.Items(forms).OfType<TupleTerm>().Any(form => form.Items is
                [Atom { Name: "attribute" }, _, Atom { Name: "moduledoc" }, Atom { Name: "false" }]))
        {
            throw new InvalidOperationException(Mismatch);
        }
    }
}
