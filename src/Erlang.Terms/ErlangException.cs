using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang;

public sealed class ErlangException(Term reason, string exceptionClass = ErlangExceptionClasses.Error) : Exception(reason.ToString())
{
    public Term Reason { get; } = reason;
    public string ExceptionClass { get; } = exceptionClass;
    public Term StackTraceTerm { get; init; } = Nil.Value;

    public ErlangException WithStackFrame(string module, string function, IReadOnlyList<Term> arguments) => new(Reason, ExceptionClass)
    {
        StackTraceTerm = new Cons(Term.Tuple(
            Term.A(module),
            Term.A(function),
            Cons.From(arguments),
            Nil.Value
        ), StackTraceTerm)
    };

    public ErlangException(string reason) : this(Term.A(reason)) { }
}
