using System.Text;
using Erlang.Compiler;

namespace Erlang.Tests;

internal static class BeamCheckedListReturnChecks
{
    internal const string TestName = "compiler/beam/checked-entry-list-return-keeps-register";
    private const string Module = "beam_checked_list_return";
    private const string ControlsModule = "beam_checked_list_controls";
    private const string Controls = """
        -module(beam_checked_list_controls).
        -export([repeated/1, verify_binding/1]).

        repeated([]) ->
            Entries = [erlang:put(checked_list_stage, repeated)],
            {ok, {Entries, Entries}}.

        verify_binding([]) ->
            [previous] = Entries = erlang:get(checked_list_stage),
            Entries = [erlang:put(checked_list_stage, rebound)],
            {ok, {verified, Entries}}.
        """;

    internal static async Task Run()
    {
        string file = Module + ".erl";
        string source = Encoding.UTF8.GetString(BeamModuleEmissionChecks.Read(file));
        byte[] compiled = CodeGeneration.CompileBeamModule(source, file);
        if (!compiled.AsSpan().SequenceEqual(BeamModuleEmissionChecks.Read(Module + ".beam")))
        {
            throw new InvalidOperationException(BeamCheckedListReturnDiagnostics.ByteMismatch);
        }

        var image = BeamModule.Prepare(compiled);
        var controls = BeamModule.Prepare(CodeGeneration.CompileBeamModule(Controls, ControlsModule + ".erl"));
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term stage = Term.A("checked_list_stage");
            Term ok = Term.A("ok");
            Term undefined = Term.A("undefined");
            Term first = Term.A("first");
            Term second = Term.A("second");

            try
            {
                await image.Invoke(context, "init", Term.A("invalid"));
                throw new InvalidOperationException(BeamCheckedListReturnDiagnostics.EntryMatch);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A("function_clause")))
            {
                Term stageValue = await runtime.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    ErlangModuleNames.Get,
                    stage
                );
                if (!undefined.Equals(stageValue))
                {
                    throw new InvalidOperationException(BeamCheckedListReturnDiagnostics.EntryMatch);
                }
            }

            Term flags = Term.Tuple(Term.A("ordered"), Term.I(2), Term.I(7));
            if (!Term.Tuple(ok, Term.Tuple(flags, Term.List(undefined, first))).Equals(await image.Invoke(context, "init", Nil.Value)))
            {
                throw new InvalidOperationException(BeamCheckedListReturnDiagnostics.Result);
            }

            Term repeated = Term.List(second);
            if (!Term.Tuple(ok, Term.Tuple(repeated, repeated)).Equals(await controls.Invoke(context, "repeated", Nil.Value)))
            {
                throw new InvalidOperationException(BeamCheckedListReturnDiagnostics.Binding);
            }

            Term previous = Term.List(Term.A("previous"));
            await runtime.Modules.Call(
                context,
                ErlangModuleNames.Module,
                ErlangModuleNames.Put,
                stage,
                previous
            );
            try
            {
                await controls.Invoke(context, "verify_binding", Nil.Value);
                throw new InvalidOperationException(BeamCheckedListReturnDiagnostics.Binding);
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.Tuple(Term.A("badmatch"), Term.List(previous))))
            {
                return Nil.Value;
            }
        });
    }
}
