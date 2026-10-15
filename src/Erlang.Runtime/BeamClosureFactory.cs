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
        BeamExecutionIdentity execution
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
                    execution
                )
                : throw new ArgumentException(BeamClosureDiagnostics.Context)
        );
        bindings.Add(function, new(code, tableIndex));

        return function;
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
