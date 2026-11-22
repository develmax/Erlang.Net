using Erlang;
using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamLeftMatchOriginChecks
{
    internal const string TestName = "compiler/beam/badmatch-uses-the-first-pattern-annotation";
    private const string SourceFile = "beam_left_match_origin_probe.erl";
    private const string WrongOrigin = "The match failure did not preserve the source pattern location.";

    private const string Source = """
        -module(beam_left_match_origin_probe).
        -export([payload/1, pair/1]).
        -record(payload,{key,value}).
        payload(Input) ->
            #payload{key = Key,
                     value = Value} =
                load(Input),
            {Key, Value}.
        pair(Input) ->
            ((
               {Left, Right})) =
                load(Input),
            {Right, Left}.
        load(Arg) -> Arg.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, SourceFile);
        var module = BeamModule.Prepare(image);
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term bad = Term.A("wrong");
            await CheckFailure("payload", 5);
            await CheckFailure("pair", 11);

            Term record = Term.Tuple(Term.A("payload"), Term.I(1), Term.I(2));
            if (!(await module.Invoke(context, "payload", record)).Equals(Term.Tuple(Term.I(1), Term.I(2)))
                || !(await module.Invoke(context, "pair", Term.Tuple(Term.I(1), Term.I(2)))).Equals(Term.Tuple(Term.I(2), Term.I(1))))
            {
                throw new InvalidOperationException(WrongOrigin);
            }

            return Nil.Value;

            async Task CheckFailure(string function, int line)
            {
                try
                {
                    _ = await module.Invoke(context, function, bad);
                }
                catch (ErlangException error)
                {
                    if (!error.Reason.Equals(Term.Tuple(Term.A("badmatch"), bad))
                        || error.StackTraceTerm is not Cons { Head: TupleTerm { Items: [_, Atom name, _, var location] } }
                        || name.Name != function
                        || !Cons.Items(location).Contains(Term.Tuple(Term.A("file"), Term.String(SourceFile)))
                        || !Cons.Items(location).Contains(Term.Tuple(Term.A("line"), Term.I(line))))
                    {
                        throw new InvalidOperationException(WrongOrigin, error);
                    }

                    return;
                }

                throw new InvalidOperationException(WrongOrigin);
            }
        });
    }
}
