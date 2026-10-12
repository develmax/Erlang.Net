namespace Erlang.Differential;

public static class ModuleAttributeCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } =
    [
        Create("vsn-integer", "-vsn(42).", "[{vsn,[42]}]=module_info(attributes)"),
        Create("vsn-list", "-vsn([1,2]).", "[{vsn,[1,2]}]=module_info(attributes)"),
        Create("vsn-empty", "-vsn([]).", "[{vsn,[]}]=module_info(attributes)"),
        Create("vsn-string", "-vsn(\"release\").", "[{vsn,\"release\"}]=module_info(attributes)"),
        Create("vsn-binary", "-vsn(<<1,2,3>>).", "[{vsn,[<<1,2,3>>]}]=module_info(attributes)"),
        Create("vsn-map", "-vsn(#{release=>2}).", "[{vsn,[#{release:=2}]}]=module_info(attributes)"),
        Create("atom", "-custom(kept).", "[{vsn,[_]},{custom,[kept]}]=module_info(attributes)"),
        Create("list", "-custom([a,b]).", "[{vsn,[_]},{custom,[a,b]}]=module_info(attributes)"),
        Create("empty", "-custom([]).", "[{vsn,[_]},{custom,[]}]=module_info(attributes)"),
        Create("improper", "-custom([a|tail]).", "[{vsn,[_]},{custom,[a|tail]}]=module_info(attributes)"),
        Create("improper-multiple", "-custom([a,b,c|tail]).", "[{vsn,[_]},{custom,[a,b,c|tail]}]=module_info(attributes)"),
        Create("improper-nested", "-custom([[a|tail]]).", "[{vsn,[_]},{custom,[[a|tail]]}]=module_info(attributes)"),
        Create("improper-tuple", "-custom({tag,[a|tail]}).", "[{vsn,[_]},{custom,[{tag,[a|tail]}]}]=module_info(attributes)"),
        Create("vsn-improper", "-vsn([a|tail]).", "[{vsn,[a|tail]}]=module_info(attributes)"),
        Create("string", "-custom(\"hello\").", "[{vsn,[_]},{custom,\"hello\"}]=module_info(attributes)"),
        Create("tuple", "-custom({tag,[1,2]}).", "[{vsn,[_]},{custom,[{tag,[1,2]}]}]=module_info(attributes)"),
        Create("map", "-custom(#{a=>1,b=>2}).", "[{vsn,[_]},{custom,[#{a:=1,b:=2}]}]=module_info(attributes)"),
        Create("duplicate-map-key", "-custom(#{a=>1,a=>2}).", "[{vsn,[_]},{custom,[#{a:=2}]}]=module_info(attributes)"),
        Create("bits", "-custom(<<5:3,2:2>>).", "[{vsn,[_]},{custom,[<<5:3,2:2>>]}]=module_info(attributes)"),
        Create("signed", "-custom(-123).", "[{vsn,[_]},{custom,[-123]}]=module_info(attributes)"),
        Create("positive", "-custom(+42).", "[{vsn,[_]},{custom,[42]}]=module_info(attributes)"),
        Create("float", "-custom(-1.25).", "[{vsn,[_]},{custom,[-1.25]}]=module_info(attributes)"),
        Create("farity", "-custom(run/0).", "[{vsn,[_]},{custom,[{run,0}]}]=module_info(attributes)"),
        Create("nested-farity", "-custom([{run/0,hidden/1}]).", "[{vsn,[_]},{custom,[{{run,0},{hidden,1}}]}]=module_info(attributes)"),
        Create("map-farity", "-custom(#{callback=>run/0}).", "[{vsn,[_]},{custom,[#{callback:={run,0}}]}]=module_info(attributes)"),
        Create("repeated", "-custom(first). -custom(second).", "[{vsn,[_]},{custom,[first]},{custom,[second]}]=module_info(attributes)"),
        Create(
            "ordered-explicit-vsn",
            "-custom(before). -vsn(version). -other(after_value).",
            "[{custom,[before]},{vsn,[version]},{other,[after_value]}]=module_info(attributes)"
        ),
        Create("behaviour", "-behaviour(gen_server).", "[{vsn,[_]},{behaviour,[gen_server]}]=module_info(attributes)")
    ];

    private static CompiledModuleCase Create(string name, string attributes, string body) => new(
        "erlang-module-attributes-" + name,
        "-module(module_attributes_contract). -export([run/0]). " + attributes + " run()->" + body + ",done.",
        Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
    );
}
