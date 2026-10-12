namespace Erlang.Differential;

public static class CompiledModuleExecution
{
    public static Task<Term> Run(GeneratedModuleArtifact artifact, CompiledModuleCallerContext callerContext = CompiledModuleCallerContext.UntrappedFresh) =>
        Execute(artifact, callerContext, null);

    public static async Task<CompiledModuleStreamResult> RunCaptured(GeneratedModuleArtifact artifact, CompiledModuleCallerContext callerContext = CompiledModuleCallerContext.UntrappedFresh)
    {
        using var output = new MemoryStream();
        using var error = new MemoryStream();
        using var streams = new RuntimeStandardStreams(output, error);
        Term result = await Execute(artifact, callerContext, streams);

        return new(result, Convert.ToHexString(output.ToArray()), Convert.ToHexString(error.ToArray()));
    }

    private static async Task<Term> Execute(GeneratedModuleArtifact artifact, CompiledModuleCallerContext callerContext, RuntimeStandardStreams? streams)
    {
        await using var runtime = streams is null ? new ProcessRuntime() : new ProcessRuntime(streams);
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
