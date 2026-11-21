namespace Erlang.Compiler;

internal static class BeamParseTransformRunner
{
    private const string EntryPoint = "parse_transform";

    internal static async Task<Term> InvokeAsync(
        RuntimeLibrarySelection selection,
        string module,
        Term forms,
        Term options,
        RuntimeNativePrimitives nativePrimitives,
        CancellationToken cancellation
    )
    {
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentException.ThrowIfNullOrWhiteSpace(module);
        ArgumentNullException.ThrowIfNull(forms);
        ArgumentNullException.ThrowIfNull(options);
        if (!selection.Modules.Any(choice => choice.Module == module && choice.Backend == RuntimeModuleBackend.Beam))
        {
            throw new ArgumentException(CodeGenerationDiagnostics.ParseTransformRequiresBeam(module), nameof(selection));
        }

        await using var runtime = await ProcessRuntime.CreateAsync(selection, nativePrimitives, cancellation)
            .ConfigureAwait(false);
        var result = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle worker = runtime.Spawn(async context =>
        {
            try
            {
                Term value = await runtime.Modules.Call(
                    context,
                    module,
                    EntryPoint,
                    forms,
                    options
                );
                result.TrySetResult(value);
            }
            catch (Exception error) when (error is not IOwnedControlTransfer506 and not OperationCanceledException)
            {
                result.TrySetException(error);
                _ = result.Task.Exception;
            }

            return Nil.Value;
        });

        Term reason = await worker.Completion.WaitAsync(cancellation).ConfigureAwait(false);
        if (!reason.Equals(Term.A(ProcessExitReasons.Normal)))
        {
            throw new ErlangException(reason, ErlangExceptionClasses.Exit);
        }

        if (!result.Task.IsCompleted)
        {
            throw new InvalidOperationException(CodeGenerationDiagnostics.ParseTransformDidNotReturn(module));
        }

        return await result.Task.ConfigureAwait(false);
    }
}
