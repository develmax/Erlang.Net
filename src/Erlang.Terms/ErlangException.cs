using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang;

public sealed class ErlangException(Term reason, string exceptionClass = ErlangExceptionClasses.Error) : Exception(reason.ToString())
{
    public Term Reason { get; } = reason;
    public string ExceptionClass { get; } = exceptionClass;
    public Term StackTraceTerm { get; init; } = Nil.Value;
    public bool ExplicitStackTrace
    {
        get; init;
    }

    private ErlangExceptionTraceOrigin traceOrigin;

    public ErlangException WithStackFrame(string module, string function, IReadOnlyList<Term> arguments)
        => WithFrame(
            module,
            function,
            arguments,
            false
        );

    internal ErlangException WithSelectedHandlerFrame(string module, string function, IReadOnlyList<Term> arguments)
        => WithFrame(
            module,
            function,
            arguments,
            true
        );

    private ErlangException WithFrame(
        string module,
        string function,
        IReadOnlyList<Term> arguments,
        bool selectedHandler
    )
    {
        if (ExplicitStackTrace)
            return this;
        Term frame = Term.Tuple(
            Term.A(module),
            Term.A(function),
            Cons.From(arguments),
            Nil.Value
        );

        return new(Reason, ExceptionClass)
        {
            StackTraceTerm = traceOrigin == ErlangExceptionTraceOrigin.SelectedHandlerCaptured
                ? AppendFrame(StackTraceTerm, frame)
                : new Cons(frame, StackTraceTerm),
            traceOrigin = selectedHandler ? ErlangExceptionTraceOrigin.SelectedHandlerCaptured : traceOrigin
        };
    }

    private static Term AppendFrame(Term trace, Term frame)
    {
        var frames = new List<Term>();
        Term tail = trace;
        while (tail is Cons current)
        {
            frames.Add(current.Head);
            tail = current.Tail;
        }
        Term result = new Cons(frame, tail);
        for (int index = frames.Count - 1; index >= 0; index--)
            result = new Cons(frames[index], result);

        return result;
    }

    public ErlangException(string reason) : this(Term.A(reason)) { }
}
