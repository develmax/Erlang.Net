namespace Erlang.Differential;

public static class NativeUnderscoreCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string declarations, string body) => cases.Add(new(
            "erlang-native-underscore-" + name,
            "-module(native_underscore_contract). -export([run/0]). " + declarations + " run()->" + body + ",done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));
        Add("unused-declaration", "-record #'_'{x=1}.", "ok");
        Add(
            "runtime-create",
            "",
            "R=records:create(native_underscore_contract,'_',[{x,1}],#{is_exported=>false}),'_'=records:get_name(R),1=records:get(x,R),true=is_record(R)"
        );
        Add(
            "local-update-existing",
            "-record #'_'{x=1}.",
            "R=records:create(native_underscore_contract,'_',[{x,1}],#{is_exported=>false}),U=R#'_'{x=2},1=R#'_'.x,2=U#'_'.x,'_'=records:get_name(U)"
        );
        Add(
            "qualified-construction",
            "-record #'_'{x=1}. -export_record(['_']).",
            "R=#native_underscore_contract:'_'{},1=R#native_underscore_contract:'_'.x,'_'=records:get_name(R)"
        );

        return cases;
    }
}
