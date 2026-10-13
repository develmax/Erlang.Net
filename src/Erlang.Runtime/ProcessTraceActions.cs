namespace Erlang;

internal sealed class ProcessTraceActions : IMatchSpecTraceActions
{
    public bool Supports(string name, int arity) => name == MatchSpecNames.Message && arity == 1 || arity == 0 && name is MatchSpecNames.ReturnTrace or MatchSpecNames.ExceptionTrace;

    public Term Execute(ProcessContext context, string name, IReadOnlyList<Term> arguments)
    {
        if (!Supports(name, arguments.Count))
            throw MatchSpecNames.UnsupportedAction(name, arguments.Count);

        return CoreModules.Boolean(true);
    }
}
