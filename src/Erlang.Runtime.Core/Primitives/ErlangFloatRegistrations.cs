using System.Text;

namespace Erlang;

internal static class ErlangFloatRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        foreach (int arity in new[] { 1, 2 })
        {
            registry.Register(
                ErlangModuleNames.Module,
                ErlangFloatBifNames.ToList,
                arity,
                (_, arguments) => ValueTask.FromResult<Term>(Term.String(ErlangFloatOutput.Format(arguments)))
            );
            registry.Register(
                ErlangModuleNames.Module,
                ErlangFloatBifNames.ToBinary,
                arity,
                (_, arguments) => ValueTask.FromResult<Term>(new BitString(Encoding.ASCII.GetBytes(ErlangFloatOutput.Format(arguments))))
            );
        }
    }
}
