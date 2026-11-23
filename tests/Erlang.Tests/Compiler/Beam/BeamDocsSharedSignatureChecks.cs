using System.Text;
using Erlang.Compiler;
using static Erlang.Tests.BeamDocumentationEmissionChecks;

namespace Erlang.Tests;

internal static class BeamDocsSharedSignatureChecks
{
    internal const string TestName = "compiler/beam/documentation-types-use-shared-spec-signatures";
    private const string Fixture = "beam_docs_shared_signatures.erl";
    private const string Mismatch = "Documentation type signatures did not follow the OTP source precedence.";
    private const string Source = """
        -module(beam_docs_shared_signatures).
        -export([collision/2, preferred/2]).
        -export_type([collision/2, preferred/2, callback_type/2, plain/2]).
        -opaque collision(Key, Value) :: {Key, Value}.
        -doc "preferred(FromDoc, OtherDoc)\nExplicit type documentation.".
        -type preferred(Key, Value) :: {Key, Value}.
        -type callback_type(Key, Value) :: {Key, Value}.
        -type plain(First, Second) :: {First, Second}.
        -callback callback_type(State :: term(), Event :: term()) -> ok.

        -spec collision(Left :: term(), Right :: term()) -> collision(term(), term()).
        collision(Left, Right) -> {Left, Right}.

        -spec preferred(Argument :: term(), Option :: term()) -> preferred(term(), term()).
        preferred(Argument, Option) -> {Argument, Option}.
        """;

    internal static Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, Fixture);
        byte[] docs = Chunks(beam).Single(chunk => chunk.Id == "Docs").Data;
        if (ExternalTermFormat.Decode(RawEtf(docs)) is not TupleTerm { Items: [_, _, _, _, _, _, var entries] })
        {
            throw new InvalidOperationException(Mismatch);
        }

        string[] expected =
        [
            "plain(First, Second)", "callback_type(State, Event)",
            "preferred(FromDoc, OtherDoc)", "collision(Left, Right)"
        ];
        var signatures = Cons.Items(entries).Cast<TupleTerm>()
            .Where(entry => entry.Items[0] is TupleTerm { Items: [Atom { Name: "type" }, _, _] })
            .Select(entry => Cons.Items(entry.Items[2]).Single());
        if (!signatures.SequenceEqual(expected.Select(text => (Term)new BitString(Encoding.UTF8.GetBytes(text)))))
        {
            throw new InvalidOperationException(Mismatch);
        }

        return Task.CompletedTask;
    }
}
