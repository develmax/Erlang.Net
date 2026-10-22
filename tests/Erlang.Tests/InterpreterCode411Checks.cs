namespace Erlang.Tests;

internal static class InterpreterCode411Checks
{
    internal static async Task Run()
    {
        var runtime = new ProcessRuntime();
        ProcessContext? context = null;
        try
        {
            Require(!runtime.Modules.HasExport(InterpreterCode411Names.Module, InterpreterCode411Names.CoverageSupport, 0));
            PreparedBeamImage? prepared = null;
            await runtime.InitializeModulesAsync(registry =>
            {
                // The unchanged source image's whole import selection is explicit.
                var imports = System.Text.Json.JsonSerializer.Deserialize<string[]>(
                    LibraryDependencyOriginalHostChecks.ReadRetainedResource(LibraryDependencyOriginalNames.Dependencies))!;
                prepared = registry.PrepareBeam(InterpreterCode411Resources.Read(), new BeamDependencySelection(imports));
                var expected = new HashSet<BeamFunctionSignature>
                {
                    new(InterpreterCode411Names.CoverageSupport, 0), new(InterpreterCode411Names.Mode, 0),
                    new(InterpreterCode411Names.Mode, 1), new(InterpreterCode411Names.SingleArgument[1], 1),
                    new(InterpreterCode411Names.GetCoverage, 2), new(InterpreterCode411Names.SingleArgument[2], 1),
                    new(InterpreterCode411Names.SingleArgument[3], 1)
                };
                Require(prepared.Name == InterpreterCode411Names.Module && prepared.NativePrimitiveExports.Count == 7
                    && expected.SetEquals(prepared.NativePrimitiveExports));
                Require(!registry.HasExport(InterpreterCode411Names.Module, InterpreterCode411Names.CoverageSupport, 0));
                Require(registry.PublishPreparedBeam(prepared).Status == BeamPublicationStatus.Published);
            });
            await using (var actor = new RuntimeNodeFoundationHostActor(runtime))
            {
                context = await actor.Ready;
                await actor.InvokeAsync(async caller =>
                {
                    Require((await runtime.Modules.Call(caller, InterpreterCode411Names.Module, InterpreterCode411Names.CoverageSupport))
                        .Equals(Term.A(ErlangBooleanAtoms.False)));
                    Require(ReferenceEquals(runtime.Modules.SourceOwner(InterpreterCode411Names.Module, InterpreterCode411Names.CoverageSupport, 0), prepared));
                    Term payload = Term.Tuple(
                        Term.A(InterpreterCode411Names.Module),
                        new MapTerm([new KeyValuePair<Term, Term>(Term.I(11), Term.List(Term.I(7), Term.I(8)))])
                    );
                    foreach (int? depth in new int?[] { null, 0, 1, 2, 8 })
                    {
                        if (depth is int actual)
                            _ = await runtime.Modules.Call(
                                caller,
                                ErlangModuleNames.Module,
                                InterpreterCode411Names.SystemFlag,
                                Term.A(InterpreterCode411Names.BacktraceDepth),
                                Term.I(actual)
                            );
                        await Error(
                            runtime,
                            caller,
                            InterpreterCode411Names.Mode,
                            []
                        );
                        foreach (string function in InterpreterCode411Names.SingleArgument)
                        {
                            await Error(
                                runtime,
                                caller,
                                function,
                                [Term.A(function == InterpreterCode411Names.SingleArgument[1]
                                ? InterpreterCode411Names.Function : InterpreterCode411Names.Module)]
                            );
                            await Error(
                                runtime,
                                caller,
                                function,
                                [payload]
                            );
                        }
                        await Error(
                            runtime,
                            caller,
                            InterpreterCode411Names.GetCoverage,
                            [Term.A(InterpreterCode411Names.Function), Term.A(InterpreterCode411Names.Module)]
                        );
                        await Error(
                            runtime,
                            caller,
                            InterpreterCode411Names.GetCoverage,
                            [payload, Term.List(payload)]
                        );
                        Require(LibrarySelectionCancellationPhaseAccess.ActiveLeases(caller) == 0);
                    }

                    return true;
                });
            }
        }
        finally
        {
            await runtime.DisposeAsync();
        }
        Require(context is not null && LibrarySelectionCancellationPhaseAccess.ActiveLeases(context) == 0);
        Require(LibrarySelectionPublicCancellationAccess.Disposed(runtime)
            && LibrarySelectionPublicCancellationAccess.Dispatcher(runtime).IsCompletedSuccessfully);
    }

    private static async Task Error(
        ProcessRuntime runtime,
        ProcessContext context,
        string function,
        Term[] arguments
    )
    {
        ErlangException? captured = null;
        try
        {
            _ = await runtime.Modules.Call(
                context,
                InterpreterCode411Names.Module,
                function,
                arguments
            );
        }
        catch (ErlangException failure)
        {
            captured = failure;
        }
        Require(captured is not null && captured.ExceptionClass == ErlangExceptionClasses.Error
            && captured.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)));
        Term expected = Term.List(Term.Tuple(
            Term.A(InterpreterCode411Names.Module),
            Term.A(function),
            Cons.From(arguments),
            Term.List(Term.Tuple(
                Term.A(InterpreterCode411Names.ErrorInfo),
                new MapTerm([new KeyValuePair<Term, Term>(Term.A(InterpreterCode411Names.ModuleKey), Term.A(InterpreterCode411Names.Formatter))])
            ))
        ));
        Require(captured!.StackTraceTerm.Equals(expected));
    }

    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(InterpreterCode411Names.Failure);
    }
}
