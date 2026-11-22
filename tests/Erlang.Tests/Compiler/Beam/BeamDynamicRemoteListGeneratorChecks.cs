using System.Collections;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamDynamicRemoteListGeneratorChecks
{
    internal const string TestName = "compiler/beam/lifts-a-list-helper-after-one-dynamic-remote-producer";
    private const string Source = """
        -module(beam_dynamic_remote_list_generator).
        -export([run/3, protected/2]).

        run(Module, Source, Tag) ->
            [{Tag, Value} || {ok, Value} <- Module:hd(Source)].

        protected(Module, Source) ->
            try [Value || {ok, Value} <- Module:hd(Source)]
            catch error:_ -> caught
            end.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, "beam_dynamic_remote_list_generator.erl");
        var generated = ((IEnumerable)Property(Code(image), "Functions")).Cast<object>()
            .Select(item => ((string)Property(item, "Name"), (int)Property(item, "Arity"))).ToHashSet();
        if (!generated.Contains(("-run/3-lc$^0/1-0-", 2))
            || generated.Contains(("-protected/2-lc$^0/1-0-", 1)))
        {
            throw new InvalidOperationException("The dynamic producer must stay in its original unprotected parent.");
        }

        var module = BeamModule.Prepare(image, new BeamDependencySelection([ErlangModuleNames.Module]));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term erlang = Term.A(ErlangModuleNames.Module);
            Term first = Term.I(1);
            Term second = Term.I(2);
            Term ok = Term.A("ok");
            Term tag = Term.A("tag");
            Term other = Term.A("other");
            Term rows = Term.List(Term.Tuple(ok, first), other, Term.Tuple(ok, second));
            Term expected = Term.List(Term.Tuple(tag, first), Term.Tuple(tag, second));
            if (!expected.Equals(await module.Invoke(context, "run", [erlang, Term.List(rows), tag]))
                || !Nil.Value.Equals(await module.Invoke(context, "run", [erlang, Term.List(Nil.Value), tag]))
                || !Term.A("caught").Equals(await module.Invoke(context, "protected", [erlang, Nil.Value])))
            {
                throw new InvalidOperationException("The generator changed row selection, order, or protected failure.");
            }

            try
            {
                await module.Invoke(context, "run", [erlang, Nil.Value, tag]);
                throw new InvalidOperationException("The producer must retain its badarg.");
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A("badarg")))
            {
            }

            try
            {
                Term malformed = new Cons(Term.Tuple(ok, first), other);
                await module.Invoke(context, "run", [erlang, Term.List(malformed), tag]);
                throw new InvalidOperationException("The helper must retain the improper generator error.");
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A("bad_generator"), other)))
            {
                return Nil.Value;
            }
        });
    }
}
