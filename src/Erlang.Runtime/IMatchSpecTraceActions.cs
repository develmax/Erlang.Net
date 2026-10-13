namespace Erlang;

public interface IMatchSpecTraceActions
{
    bool Supports(string name, int arity);

    Term Execute(ProcessContext context, string name, IReadOnlyList<Term> arguments);
}
