using System.Collections;
using Erlang.Compiler;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamTaggedAppendApplyChecks
{
    private const string Source = """
        -module(beam_tagged_append_apply).
        -export([run/3, target/1, target/2, observe/1]).
        -record(options, {extra, padding, selector}).

        run(Name, Arguments, #options{selector=default}) ->
            apply(?MODULE, Name, Arguments);
        run(Name, Arguments, #options{selector=M, extra=Extra}) when is_atom(M) ->
            erlang:apply(M, Name, Arguments ++ Extra);
        run(Name, Arguments, #options{selector=Selector, extra=Extra}) ->
            ?MODULE:observe([Selector, Name, Arguments | Extra]).

        target(Value) -> Value.
        target(Left, Right) -> {Left, Right}.
        observe(Value) -> Value.
        """;

    private const string Module = "beam_tagged_append_apply";
    private const string RunFunction = "run";
    private const string Target = "target";
    private const string Default = "default";
    private const string Options = "options";

    internal static async Task Run()
    {
        byte[] beam = CodeGeneration.CompileBeamModule(Source, Module + ".erl");
        object code = Code(beam);
        byte[] operations = ((IEnumerable)Property(code, "Instructions")).Cast<object>()
            .Select(instruction => (byte)Property(Property(instruction, "Definition"), "Opcode")).ToArray();
        int allocate = Array.IndexOf(operations, BeamEmissionFormat.Allocate);
        int needsFrame = Array.IndexOf(operations, BeamEmissionFormat.IsAtom);
        int fastTail = Array.IndexOf(operations, BeamEmissionFormat.TailExternalCall);
        int append = Array.IndexOf(operations, BeamEmissionFormat.ExternalCall);
        int framedTail = Array.IndexOf(operations, BeamEmissionFormat.TailExternalCallWithFrame);
        int listTail = Array.LastIndexOf(operations, BeamEmissionFormat.TailExternalCall);
        if (operations.Count(op => op == BeamEmissionFormat.Allocate) != 1
            || Array.IndexOf(operations, BeamEmissionFormat.IsTaggedTuple) < 0
            || fastTail < 0 || fastTail >= needsFrame || allocate != needsFrame + 1
            || append <= allocate || framedTail <= append || listTail <= framedTail)
        {
            throw new InvalidOperationException(BeamTaggedAppendApplyDiagnostics.FramePlacement);
        }

        await using var runtime = new ProcessRuntime();
        var dependencies = new BeamDependencySelection([ErlangModuleNames.Module, Module]);
        if (runtime.Modules.PublishPreparedBeam(runtime.Modules.PrepareBeam(beam, dependencies)).Status != BeamPublicationStatus.Published)
        {
            throw new InvalidOperationException(BeamTaggedAppendApplyDiagnostics.Publication);
        }

        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term marker = Term.A("marker");
            Term arguments = Term.List(marker);
            Term extra = Term.List(Term.I(7));
            Term selector = Term.Tuple(marker);
            Term name = Term.A(Target);
            Term fast = await Invoke(OptionsTerm(Term.A(Default), extra));
            Term slow = await Invoke(OptionsTerm(Term.A(Module), extra));
            Term fallback = await Invoke(OptionsTerm(selector, extra));
            if (!fast.Equals(marker)
                || !slow.Equals(Term.Tuple(marker, Term.I(7)))
                || !fallback.Equals(Term.List(
                    selector,
                    name,
                    arguments,
                    Term.I(7)
                )))
            {
                throw new InvalidOperationException(BeamTaggedAppendApplyDiagnostics.BranchResult);
            }

            try
            {
                await Invoke(Term.Tuple(Term.A(Options), marker, selector));
            }
            catch (ErlangException error) when (error.Reason.Equals(Term.A("function_clause")))
            {
                return Nil.Value;
            }

            throw new InvalidOperationException(BeamTaggedAppendApplyDiagnostics.UncheckedInput);

            ValueTask<Term> Invoke(Term input) => runtime.Modules.Call(
                context,
                Module,
                RunFunction,
                name,
                arguments,
                input
            );

            Term OptionsTerm(Term mode, Term extraArguments) => Term.Tuple(
                Term.A(Options),
                extraArguments,
                marker,
                mode
            );
        });
    }
}
