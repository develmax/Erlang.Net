using Erlang;
using Erlang.Compiler;

internal static class LocalFunctionIdentityChecks
{
    public static async Task<int> Run()
    {
        try
        {
            const string source = "-module(local_identity_probe). -export([make/2]). make(Value,_Unused)->fun()->Value end.";
            await using var origin = new ProcessRuntime("origin");
            await using var target = new ProcessRuntime("target");
            var firstDefinition = new Parser(source).ParseModule();
            var secondDefinition = new Parser(source).ParseModule();
            firstDefinition.Register(origin.Modules);
            secondDefinition.Register(target.Modules);
            FunctionTerm? first = null;
            var originProcess = origin.Spawn(async context =>
            {
                first = (FunctionTerm)await origin.Modules.Call(
                    context,
                    "local_identity_probe",
                    "make",
                    Term.I(42),
                    Term.A("unused_first")
                );
                var second = (FunctionTerm)await origin.Modules.Call(
                    context,
                    "local_identity_probe",
                    "make",
                    Term.I(42),
                    Term.A("unused_second")
                );
                if (!first.Equals(second) || first.GetHashCode() != second.GetHashCode() || !new HashSet<Term> { first }.Contains(second))
                    throw new InvalidDataException(Erlang.Tests.LocalFunctionCheckDiagnostics.LocalExactEqualityHashCoherence);
                if (first.Environment.Count != 1 || !first.Environment[0].Equals(Term.I(42)))
                    throw new InvalidDataException(Erlang.Tests.LocalFunctionCheckDiagnostics.ActualFreeEnvironment);
                bool immutable = false;
                try
                {
                    ((IList<Term>)first.Environment)[0] = Term.I(0);
                }
                catch (NotSupportedException)
                {
                    immutable = true;
                }
                if (!immutable)
                    throw new InvalidDataException(Erlang.Tests.LocalFunctionCheckDiagnostics.MutableExposedEnvironment);

                return Term.A("done");
            });
            Term firstExit = await originProcess.Completion.WaitAsync(TimeSpan.FromSeconds(10));
            if (!firstExit.Equals(Term.A("normal")) || first is null)
                throw new InvalidDataException(Erlang.Tests.LocalFunctionCheckDiagnostics.OriginExecution(firstExit));
            var targetProcess = target.Spawn(async context =>
            {
                var independent = (FunctionTerm)await target.Modules.Call(
                    context,
                    "local_identity_probe",
                    "make",
                    Term.I(42),
                    Term.A("other")
                );
                if (!first.Equals(independent) || first.GetHashCode() != independent.GetHashCode())
                    throw new InvalidDataException(Erlang.Tests.LocalFunctionCheckDiagnostics.IndependentCanonicalCodeIdentity);
                Term owner = await target.Modules.Call(
                    context,
                    "erlang",
                    "fun_info",
                    first,
                    Term.A("pid")
                );
                if (!owner.Equals(Term.Tuple(Term.A("pid"), target.InitializationPid)))
                    throw new InvalidDataException(Erlang.Tests.LocalFunctionCheckDiagnostics.InvokingVMInitializationOwner);
                Term value = await first.Invoke(context, []);
                if (!value.Equals(Term.I(42)))
                    throw new InvalidDataException(Erlang.Tests.LocalFunctionCheckDiagnostics.CrossContextLocalInvocation);

                return Term.A("done");
            });
            Term targetExit = await targetProcess.Completion.WaitAsync(TimeSpan.FromSeconds(10));
            if (!targetExit.Equals(Term.A("normal")))
                throw new InvalidDataException(Erlang.Tests.LocalFunctionCheckDiagnostics.TargetExecution(targetExit));
            Console.WriteLine("Local identity, hash, immutable environment, canonical code and cross-runtime owner checks passed; native unrun");

            return 0;
        }
        catch (Exception exception)
        {
            Console.WriteLine("LOCAL FUNCTION FAIL " + exception);

            return 1;
        }
    }
}
