using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang;

public sealed class FunctionTerm(int arity, Func<ITermExecutionContext, IReadOnlyList<Term>, ValueTask<Term>> invoke) : Term
{
    private static long next;
    internal long Identity { get; } = Interlocked.Increment(ref next);
    public int Arity { get; } = arity;
    public ExternalFunctionIdentity? ExternalIdentity { get; }
    private readonly LocalFunctionIdentity? localIdentity;
    private readonly Func<LocalFunctionIdentity>? identityProvider;
    public LocalFunctionIdentity? LocalIdentity => identityProvider?.Invoke() ?? localIdentity;
    internal LocalFunctionIdentity? StoredLocalIdentity => localIdentity;
    public IReadOnlyList<Term> Environment { get; } = Array.Empty<Term>();

    public FunctionTerm(ExternalFunctionIdentity identity, Func<ITermExecutionContext, IReadOnlyList<Term>, ValueTask<Term>> invoke) : this(identity.Arity, invoke)
    {
        ExternalIdentity = identity;
    }

    public FunctionTerm(
        LocalFunctionIdentity identity,
        int arity,
        IEnumerable<Term> environment,
        Func<ITermExecutionContext, IReadOnlyList<Term>, ValueTask<Term>> invoke
    ) : this(arity, invoke)
    {
        localIdentity = identity;
        Environment = Array.AsReadOnly(environment.ToArray());
    }

    public ValueTask<Term> Invoke(ITermExecutionContext context, IReadOnlyList<Term> arguments)
    {
        if (arguments.Count != Arity)
            throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadArity), Term.Tuple(this, Cons.From(arguments))));

        return context is ITermInvocationBoundary506 { OwnedInvocationEnabled506: true } boundary
            ? boundary.InvokeOwnedFunction506(this, arguments)
            : invoke(context, arguments);
    }

    internal ValueTask<Term> InvokeDirect506(ITermExecutionContext context, IReadOnlyList<Term> arguments)
        => invoke(context, arguments);

    public FunctionTerm(
        Func<LocalFunctionIdentity> identityProvider,
        int arity,
        IEnumerable<Term> environment,
        Func<ITermExecutionContext, IReadOnlyList<Term>, ValueTask<Term>> invoke
    ) : this(arity, invoke)
    {
        this.identityProvider = identityProvider;
        Environment = Array.AsReadOnly(environment.ToArray());
    }

    public override string ToString() => $"#Fun<{Identity}/{Arity}>";
}
