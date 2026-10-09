using System.Numerics;

namespace Erlang;

public sealed class ModuleRegistry
{
    private readonly Dictionary<(string Module, string Function, int Arity), ErlangFunction> functions = new();
    public void Register(string module, string function, int arity, ErlangFunction body) => functions.Add((module, function, arity), body);
    public ValueTask<Term> Call(ProcessContext context, string module, string function, params Term[] arguments)
        => functions.TryGetValue((module, function, arguments.Length), out var f) ? f(context, arguments) : throw new ErlangException(ErlangErrorReasons.UndefinedFunction);
    public IReadOnlyCollection<(string Module, string Function, int Arity)> Exports => functions.Keys;
}
