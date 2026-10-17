namespace Erlang.Tests;

public static class ProplistGetValueMfaContracts
{
    internal static bool OwnsTest(string name)
        => name.StartsWith(ProplistGetValueHostNames.TestPrefix, StringComparison.Ordinal)
            || name.StartsWith(ProplistGetValueHostNames.CompiledTestPrefix, StringComparison.Ordinal)
            || name.StartsWith(ProplistGetValueHostNames.MfaTestPrefix, StringComparison.Ordinal);

    public static bool Owns(string module, string function, int arity)
        => module == ProplistGetValueHostNames.Module
            && function == ProplistGetValueHostNames.Function
            && arity is 2 or 3;

    public static async Task Run(int arity)
    {
        if (arity is not (2 or 3))
            throw new ArgumentOutOfRangeException(nameof(arity));

        await using var runtime = new ProcessRuntime();
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        Term key = Term.A(ProplistGetValueHostNames.Key);
        Term expected = arity == 2
            ? Term.A(ErlangBooleanAtoms.True)
            : Term.Tuple(Term.I(42), Nil.Value);
        Term[] arguments = arity == 2
            ? [key, Term.List(key)]
            : [key, Nil.Value, expected];
        Term result = await owner.InvokeAsync(context => runtime.Modules.Call(
            context,
            ProplistGetValueHostNames.Module,
            ProplistGetValueHostNames.Function,
            arguments
        ).AsTask()).WaitAsync(ProplistGetValueHostNames.Deadline);
        if (!result.Equals(expected))
            throw new InvalidOperationException(ProplistGetValueHostNames.Failure);
    }
}
