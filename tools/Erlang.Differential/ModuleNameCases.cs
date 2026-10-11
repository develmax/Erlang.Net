namespace Erlang.Differential;

public static class ModuleNameCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = [
        new(
            "module-latin1-maximum",
            "-module('ÿ'). -export([run/0]). run()->{module,M}=erlang:fun_info(fun()->ok end,module),M.",
            Term.Tuple(Term.A("ok"), Term.A("ÿ"))
        ),
        new(
            "module-visible-with-spaces",
            "-module(' visible '). -export([run/0]). run()->{module,M}=erlang:fun_info(fun()->ok end,module),M.",
            Term.Tuple(Term.A("ok"), Term.A(" visible "))
        ),
        new(
            "module-visible-with-nbsp",
            "-module(' visible'). -export([run/0]). run()->{module,M}=erlang:fun_info(fun()->ok end,module),M.",
            Term.Tuple(Term.A("ok"), Term.A(" visible"))
        ),
        new(
            "module-visible-with-soft-hyphen",
            "-module('­visible'). -export([run/0]). run()->{module,M}=erlang:fun_info(fun()->ok end,module),M.",
            Term.Tuple(Term.A("ok"), Term.A("­visible"))
        )
    ];
}
