using System.Runtime.CompilerServices;

namespace Erlang;

internal static class BeamClosureFactory
{
    private static readonly ConditionalWeakTable<FunctionTerm, BeamClosureBinding> bindings = new();

    internal static FunctionTerm Create(BeamLinkedCode code, int tableIndex, IReadOnlyList<Term> environment)
        => Create(
            code,
            tableIndex,
            environment,
            new(new ModuleCodeImage(code.Structure.Atoms[1]!), code)
        );

    internal static FunctionTerm Create(
        BeamLinkedCode code,
        int tableIndex,
        IReadOnlyList<Term> environment,
        BeamExecutionIdentity execution,
        LocalFunctionInvocation? invocation = null,
        RuntimeFunctionEntries? entries = null,
        LocalFunctionIdentity? legacyIdentity = null
    )
    {
        BeamLambdaEntry lambda = code.Lambdas[tableIndex];
        IReadOnlyList<Term> captured = Array.AsReadOnly(environment.ToArray());
        BeamLinkedFunction target = code.Functions.Single(function => function.EntryLabel == lambda.Label);
        var identity = new LocalFunctionIdentity(
            Term.A(code.Structure.Atoms[1]!),
            Term.A(code.Structure.Atoms[lambda.FunctionAtomIndex]!),
            lambda.StoredIndex,
            lambda.OldUniqueness,
            BeamChecksumReader.Read(code),
            tableIndex
        );
        LocalFunctionIdentity? first = legacyIdentity ?? entries?.GetCanonicalIdentityForHash(identity, lambda.TotalArity - lambda.FreeVariables);
        if (first is not null)
            identity = identity with { OldUnique = first.OldUnique, LegacyIndex = first.OldIndex };
        FunctionTerm? function = null;
        function = new FunctionTerm(
            identity,
            lambda.TotalArity - lambda.FreeVariables,
            captured,
            (context, arguments) => context is ProcessContext process
                ? InvokeBoundFunction647(
                    process,
                    code,
                    target,
                    captured,
                    execution,
                    identity,
                    invocation?.OriginalFunction ?? function!,
                    arguments,
                    invocation
                )
                : throw new ArgumentException(BeamClosureDiagnostics.Context)
        );
        bindings.Add(function, new(
            code,
            tableIndex,
            execution,
            identity
        ));

        function.MarkRuntimeLocalTransferDescriptor();

        return function;
    }

    private static async ValueTask<Term> InvokeBoundFunction647(
        ProcessContext process,
        BeamLinkedCode capturedCode,
        BeamLinkedFunction capturedTarget,
        IReadOnlyList<Term> environment,
        BeamExecutionIdentity execution,
        LocalFunctionIdentity identity,
        FunctionTerm original,
        IReadOnlyList<Term> arguments,
        LocalFunctionInvocation? invocation
    )
    {
        // Unadmitted HOST-only linked-code helpers keep their existing route.
        if (execution.Owner is not PreparedBeamImage owner || !ReferenceEquals(owner.Owner, process.Runtime.Modules))
            return await BeamRegisterInterpreter.ExecuteFunction(
                process,
                capturedCode,
                capturedTarget,
                arguments,
                environment,
                execution,
                invocation: invocation
            ).ConfigureAwait(false);

        SourceFunctionAdmission admission;
        try
        {
            admission = await process.Runtime.Modules.AdmitSourceFunction(
                process,
                identity.Module.Name,
                identity.Unique,
                owner,
                execution.Image,
                original,
                invocation
            ).ConfigureAwait(false);
        }
        catch (ErlangException error) when (error.Reason is TupleTerm
        {
            Items: [Atom { Name: ErlangErrorReasons.BadFunction }, FunctionTerm failed]
        } && ReferenceEquals(failed, original))
        {
            throw process.CaptureBeamFunAdmissionFailure650(error, original);
        }
        if (admission.Kind == SourceFunctionAdmissionKind.Retry)
            throw new LocalFunctionPublicationChangedException(invocation!);
        if (admission.Kind == SourceFunctionAdmissionKind.MissingCurrentLocalFunction)
        {
            process.CompleteBeamFunAdmission650(original);

            return await process.Runtime.Modules.InvokeUndefinedLambda(
                process,
                identity.Module.Name,
                original,
                arguments,
                invocation
            ).ConfigureAwait(false);
        }

        // The actual invocation lease is transferred through the single selected
        // interpreter entry. This finally also covers setup/cancellation failure.
        using SourceCodeLease? lease = admission.Lease;
        if (admission.Owner is not PreparedBeamImage selected || lease is null
            || !ReferenceEquals(selected.Owner, process.Runtime.Modules)
            || !ReferenceEquals(admission.Image, selected.ExecutionIdentity.Image)
            || selected.Name != identity.Module.Name || !selected.Descriptor.Fingerprint.Equals(identity.Unique))
            throw new InvalidDataException(BeamClosureDiagnostics.KnownTarget);
        int tableIndex = -1;
        for (int index = 0; index < selected.Code.Lambdas.Count; index++)
        {
            BeamLambdaEntry candidate = selected.Code.Lambdas[index];
            if (candidate.StoredIndex == identity.Index
                && candidate.TotalArity - candidate.FreeVariables == arguments.Count
                && candidate.FreeVariables == environment.Count)
            {
                tableIndex = index;
                break;
            }
        }
        if (tableIndex < 0) throw new InvalidDataException(BeamClosureDiagnostics.KnownTarget);
        BeamLambdaEntry lambda = selected.Code.Lambdas[tableIndex];
        BeamLinkedFunction target = selected.Code.Functions.Single(function => function.EntryLabel == lambda.Label);

        process.CompleteBeamFunAdmission650(original);

        return await BeamRegisterInterpreter.ExecuteFunction(
            process,
            selected.Code,
            target,
            arguments,
            environment,
            selected.ExecutionIdentity,
            invocation: invocation,
            admittedLease647: lease
        ).ConfigureAwait(false);
    }

    internal static bool TryCaptureTransferBinding506(ProcessRuntime owner, FunctionTerm function, out BeamClosureBinding? binding)
    {
        if (!bindings.TryGetValue(function, out binding))
            return false;
        if (binding.Execution.Owner is PreparedBeamImage prepared && !ReferenceEquals(prepared.Owner, owner.Modules))
        {
            binding = null;

            return false;
        }

        return true;
    }

    internal static BeamClosureBinding CaptureStoredBinding(ProcessRuntime owner, FunctionTerm function)
    {
        if (!bindings.TryGetValue(function, out BeamClosureBinding? binding)
            || binding.Execution.Owner is not PreparedBeamImage prepared
            || !ReferenceEquals(prepared.Owner, owner.Modules)
            || !ReferenceEquals(prepared.Code, binding.Code)
            || !ReferenceEquals(prepared.ExecutionIdentity, binding.Execution))
            throw new NotSupportedException(RuntimeEtsFoundationNames.UnsupportedFunction);

        return binding;
    }

    internal static void RequireKnownTarget(Term value, BeamLinkedCode code, int tableIndex)
    {
        if (value is not FunctionTerm function || !bindings.TryGetValue(function, out BeamClosureBinding? binding)
            || !ReferenceEquals(binding.Code, code) || binding.TableIndex != tableIndex)
        {
            throw new NotSupportedException(BeamClosureDiagnostics.KnownTarget);
        }
    }
}
