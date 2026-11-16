using System.Text;
using Erlang.Compiler;

namespace Erlang.Tests;

internal static class NamedFunSourceChecks
{
    internal const string TestName = "compiler/source/named-recursive-fun";

    internal static async Task Run()
    {
        const string file = "named_fun_source_probe.erl";
        string source = Encoding.UTF8.GetString(BeamModuleEmissionChecks.Read(file));
        ModuleDefinition module = new Parser(source).ParseModule();
        await using var runtime = new ProcessRuntime();
        module.Register(runtime.Modules);
        Term? actual = null;
        var process = runtime.Spawn(async context =>
        {
            var pair = (TupleTerm)await Execution.InvokeAsync(module, "make", context, [Term.I(7)]);
            var loop = (FunctionTerm)pair.Items[1];
            Term sum = await loop.Invoke(context, [Term.I(2000), Term.I(0)]);
            var self = (FunctionTerm)await Execution.InvokeAsync(module, "self_fun", context, []);
            var outer = (FunctionTerm)await Execution.InvokeAsync(module, "nested", context, []);
            var inner = (FunctionTerm)await outer.Invoke(context, []);
            Term shadowed = await Execution.InvokeAsync(module, "shadow", context, []);
            Term immediate = await Execution.InvokeAsync(module, "immediate", context, [Term.I(13)]);
            actual = Term.Tuple(
                pair.Items[0],
                sum,
                Term.I(loop.Environment.Count),
                Term.I(self.Environment.Count),
                Term.A(self.Equals(await self.Invoke(context, [])) ? "true" : "false"),
                Term.A(outer.Equals(await inner.Invoke(context, [])) ? "true" : "false"),
                shadowed,
                immediate
            );

            return actual;
        });
        Term exitReason = await process.Completion;
        Term expected = Term.Tuple(
            Term.A("outer"),
            Term.I(2001007),
            Term.I(1),
            Term.I(0),
            Term.A("true"),
            Term.A("true"),
            Term.Tuple(Term.A("matched"), Term.A("other")),
            Term.Tuple(Term.I(13), Term.I(13), Term.I(13), Term.A("local"))
        );
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException($"Named fun returned {actual}, exit {exitReason}.");
        }
    }
}
