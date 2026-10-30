using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang;

public sealed partial class FunctionTerm(int arity, Func<ITermExecutionContext, IReadOnlyList<Term>, ValueTask<Term>> invoke) : Term
{
    private static long next;
    internal long Identity { get; } = Interlocked.Increment(ref next);
    public int Arity => ownedFunctionStorage?.ReadFunctionArity() ?? arity;
    public ExternalFunctionIdentity? ExternalIdentity { get; }
    private readonly LocalFunctionIdentity? localIdentity;
    private readonly Func<LocalFunctionIdentity>? identityProvider;
    public LocalFunctionIdentity? LocalIdentity => identityProvider?.Invoke() ?? localIdentity;
    internal LocalFunctionIdentity? StoredLocalIdentity => localIdentity;
    private readonly IReadOnlyList<Term> environment = Array.Empty<Term>();
    public IReadOnlyList<Term> Environment => ownedFunctionStorage?.ReadFunctionEnvironment() ?? environment;

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
        this.environment = Array.AsReadOnly(environment.ToArray());
    }

    public ValueTask<Term> Invoke(ITermExecutionContext context, IReadOnlyList<Term> arguments)
    {
        if (arguments.Count != Arity)
            throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadArity), Term.Tuple(this, Cons.From(arguments))));

        return context is ITermInvocationBoundary506 { OwnedInvocationEnabled506: true } boundary
            ? boundary.InvokeOwnedFunction506(this, arguments)
            : InvokeDirect506(context, arguments);
    }

    internal FunctionTerm CopyOpaqueForTransfer(IReadOnlyList<Term> environment)
    {
        if (ownedFunctionStorage is null && localIdentity is null && identityProvider is null && Environment.Count == 0)
            return this;

        return new FunctionTerm(
            Arity,
            invoke,
            ExternalIdentity,
            localIdentity,
            identityProvider,
            Identity,
            environment
        );
    }

    internal FunctionTerm CopyOpaqueForHeapStorage(IReadOnlyList<Term> copiedEnvironment)
        => new FunctionTerm(
            Arity,
            invoke,
            ExternalIdentity,
            localIdentity,
            identityProvider,
            Identity,
            copiedEnvironment
        );

    internal ValueTask<Term> InvokeDirect506(ITermExecutionContext context, IReadOnlyList<Term> arguments)
        => ownedFunctionStorage?.InvokeFunctionBody(context, arguments) ?? invoke(context, arguments);

    internal ValueTask<Term> InvokeHeapBody(ITermExecutionContext context, IReadOnlyList<Term> arguments)
        => invoke(context, arguments);

    public FunctionTerm(
        Func<LocalFunctionIdentity> identityProvider,
        int arity,
        IEnumerable<Term> environment,
        Func<ITermExecutionContext, IReadOnlyList<Term>, ValueTask<Term>> invoke
    ) : this(arity, invoke)
    {
        this.identityProvider = identityProvider;
        this.environment = Array.AsReadOnly(environment.ToArray());
    }

    public override string ToString() => $"#Fun<{Identity}/{Arity}>";
}
