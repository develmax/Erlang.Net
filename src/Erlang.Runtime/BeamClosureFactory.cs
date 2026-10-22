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
        var function = new FunctionTerm(
            identity,
            lambda.TotalArity - lambda.FreeVariables,
            captured,
            (context, arguments) => context is ProcessContext process
                ? BeamRegisterInterpreter.ExecuteFunction(
                    process,
                    code,
                    target,
                    arguments,
                    captured,
                    execution,
                    invocation: invocation
                )
                : throw new ArgumentException(BeamClosureDiagnostics.Context)
        );
        bindings.Add(function, new(
            code,
            tableIndex,
            execution,
            identity
        ));

        return function;
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
