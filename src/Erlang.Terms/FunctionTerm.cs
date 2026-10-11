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
    public LocalFunctionIdentity? LocalIdentity { get; }
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
        LocalIdentity = identity;
        Environment = Array.AsReadOnly(environment.ToArray());
    }

    public ValueTask<Term> Invoke(ITermExecutionContext context, IReadOnlyList<Term> arguments) => arguments.Count == Arity ? invoke(context, arguments) : throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadArity), Term.Tuple(this, Cons.From(arguments))));

    public override string ToString() => $"#Fun<{Identity}/{Arity}>";
}
