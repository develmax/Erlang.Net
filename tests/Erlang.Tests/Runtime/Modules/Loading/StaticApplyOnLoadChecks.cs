using Erlang.Compiler;
using Erlang.Differential;

namespace Erlang.Tests;

public static class StaticApplyOnLoadChecks
{
    public static async Task Run()
    {
        await BeamLoaderDirectChecks.RunSource(StaticApplyOnLoadNames.ObserverSource);
        var cases = StaticApplyOnLoadNames.Cases;

        foreach (var c in cases)
            foreach (bool generated in new[] { false, true })
            {
                await using var runtime = new ProcessRuntime();
                using GeneratedModuleArtifact? artifact = generated ? GeneratedModuleCompiler.Compile(c.Value) : null;
                var definition = artifact?.Definition ?? new Parser(c.Value).ParseModule();
                definition.Register(runtime.Modules);
                var testAssembly = typeof(Erlang.Tests.BeamLoaderDirectChecks).Assembly;
                string resource = testAssembly.GetManifestResourceNames().Single(n => n.EndsWith(BeamLoaderDirectNames.FreshResource));
                using var stream = testAssembly.GetManifestResourceStream(resource)!;
                using var memory = new MemoryStream();
                stream.CopyTo(memory);
                string module = BeamOnLoadHostNames.FreshSuccessModule;
                Exception? failure = null;
                Term? result = null;
                var actor = runtime.Spawn(async context =>
                {
                    try
                    {
                        Term reference = await runtime.Modules.Call(
                            context,
                            ErlangModuleNames.Module,
                            BeamLoaderAdmissionNames.Prepare,
                            Term.A(module),
                            new BitString(memory.ToArray())
                        );
                        await runtime.Modules.Call(
                            context,
                            ErlangModuleNames.Module,
                            BeamLoaderAdmissionNames.Finish,
                            Term.List(reference)
                        );
                        try
                        {
                            result = await runtime.Modules.Call(
                                context,
                                definition.Name,
                                BeamOnLoadHostNames.Run,
                                Term.A(module)
                            );
                        }
                        catch (ErlangException error)
                        {
                            result = Term.Tuple(Term.A(error.ExceptionClass), error.Reason);
                        }
                        if (c.Key == StaticApplyOnLoadNames.EffectOnce)
                        {
                            Term? first = await context.ReceiveAsync(t => t.Equals(Term.A(StaticApplyOnLoadNames.ItemEvaluated)) ? t : null, TimeSpan.Zero);
                            Term? twice = await context.ReceiveAsync(t => t.Equals(Term.A(StaticApplyOnLoadNames.ItemEvaluated)) ? t : null, TimeSpan.Zero);
                            if (first is null || twice is not null)
                                throw new InvalidOperationException(StaticApplyOnLoadNames.Failure);
                        }
                    }
                    catch (Exception error)
                    {
                        failure = error;
                    }

                    return Nil.Value;
                });
                await actor.Completion;
                if (failure is not null)
                    throw failure;
                Term expected = c.Key switch
                {
                    StaticApplyOnLoadNames.Unqualified or StaticApplyOnLoadNames.Qualified or StaticApplyOnLoadNames.EffectOnce => Term.Tuple(Term.A(ErlangExceptionClasses.Error), Term.Tuple(Term.A(ErlangErrorReasons.BadMatch), Term.A(ErlangBooleanAtoms.False))),
                    StaticApplyOnLoadNames.Dynamic or StaticApplyOnLoadNames.ListVariable or StaticApplyOnLoadNames.Improper => Term.Tuple(Term.A(ErlangExceptionClasses.Error), Term.A(ErlangErrorReasons.BadArgument)),
                    StaticApplyOnLoadNames.ItemThrow => Term.Tuple(Term.A(ErlangExceptionClasses.Error), Term.A(StaticApplyOnLoadNames.ItemFailed)),
                    _ => Term.Tuple(
                        Term.A(StaticApplyOnLoadNames.Local),
                        Term.A(ErlangModuleNames.Module),
                        Term.A(BeamLoaderDirectNames.Callback),
                        Term.List(Term.A(module))
                    )
                };
                if (result is null || !result.Equals(expected))
                    throw new InvalidOperationException(StaticApplyOnLoadNames.Failure);
            }
    }
}
