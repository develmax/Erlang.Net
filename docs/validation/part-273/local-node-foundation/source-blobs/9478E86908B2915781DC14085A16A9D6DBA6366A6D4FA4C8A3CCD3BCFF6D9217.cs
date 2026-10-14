using System.Reflection;

namespace Erlang.Tests;

internal static class RuntimeNodeAdmissionHostSetup
{
    internal static void Attach(ProcessContext context, string name)
    {
        context.Runtime.Register(RuntimeNodeControlNames.Kernel, context.Self);
        MethodInfo method = typeof(ProcessRuntime).GetMethod(RuntimeNodeAdmissionHostSetupNames.Admission, BindingFlags.Instance | BindingFlags.NonPublic)!;
        Term result = (Term)method.Invoke(context.Runtime, new object[] { context, Term.A(name), Term.I(0) })!;
        if (!result.Equals(Term.A(ErlangBooleanAtoms.True)) || !context.Runtime.IsDistributionAlive)
            throw new InvalidOperationException(RuntimeNodeAdmissionHostSetupNames.Failure);
        context.Runtime.Unregister(RuntimeNodeControlNames.Kernel);
    }
}
