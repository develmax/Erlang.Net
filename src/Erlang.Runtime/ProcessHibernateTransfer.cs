namespace Erlang;

public sealed class ProcessHibernateTransfer : Exception
{
    public string Module { get; }
    public string Function { get; }
    public IReadOnlyList<Term> Arguments { get; }

    internal ProcessHibernateTransfer(string module, string function, IReadOnlyList<Term> arguments)
    {
        Module = module;
        Function = function;
        Arguments = Array.AsReadOnly(arguments.ToArray());
    }
}
