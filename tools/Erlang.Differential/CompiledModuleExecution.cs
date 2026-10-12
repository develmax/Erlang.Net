namespace Erlang.Differential;

public static class CompiledModuleExecution
{
    public static async Task<Term> Run(GeneratedModuleArtifact artifact, CompiledModuleCallerContext callerContext = CompiledModuleCallerContext.UntrappedFresh)
    {
        await using var runtime = new ProcessRuntime();
        artifact.Definition.Register(runtime.Modules);
        Term? result = null;
        var process = runtime.Spawn(async context =>
        {
            context.TrapExits = callerContext == CompiledModuleCallerContext.TrappedFresh;
            try
            {
                result = Term.Tuple(
                    Term.A(OracleOutcomeTags.Success),
                    await runtime.Modules.Call(context, artifact.Definition.Name, GeneratedModuleMetadata.EntryPoint)
                );
            }
            catch (ErlangException exception)
            {
                result = Term.Tuple(Term.A(OracleOutcomeTags.Failure), Term.A(exception.ExceptionClass), exception.Reason);
            }

            return Term.A(OracleProcessResults.Completed);
        });
        Term reason = await process.Completion;
        if (!reason.Equals(Term.A(ProcessExitReasons.Normal)) || result is null)
            return Term.Tuple(Term.A(OracleOutcomeTags.Failure), Term.A(ErlangExceptionClasses.Exit), reason);

        return result;
    }
}
