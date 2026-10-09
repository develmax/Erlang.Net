using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang;

public sealed class ErlangException(Term reason, string exceptionClass = ErlangExceptionClasses.Error) : Exception(reason.ToString())
{
    public Term Reason { get; } = reason;
    public string ExceptionClass { get; } = exceptionClass;
    public ErlangException(string reason) : this(Term.A(reason)) { }
}
