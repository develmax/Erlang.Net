namespace Erlang.Differential;

public static class MatchSpecFlatMapOrderCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } =
    [
        new(
            "erlang-match-flatmap-numeric-reverse-construction",
            "-module(match_flatmap_order_contract). -export([run/0]). run()->{ok,first,[],[]}=erlang:match_spec_test([],[{'_',[],[#{2=>{message,second},1=>{message,first}}]}],trace),done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "erlang-match-flatmap-numeric-three-actions",
            "-module(match_flatmap_order_contract). -export([run/0]). run()->{ok,first,[],[]}=erlang:match_spec_test([],[{'_',[],[#{3=>{message,third},1=>{message,first},2=>{message,second}}]}],trace),done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "erlang-match-flatmap-mixed-numeric-atom-actions",
            "-module(match_flatmap_order_contract). -export([run/0]). run()->{ok,first,[],[]}=erlang:match_spec_test([],[{'_',[],[#{some_atom=>{message,second},1=>{message,first}}]}],trace),done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "erlang-match-flatmap-dynamic-z-before-a",
            "-module(match_flatmap_order_contract). -export([run/0]). run()->{'EXIT',{badarg,_}}=catch erlang:list_to_existing_atom(\"match_flatmap_z_first\"),{'EXIT',{badarg,_}}=catch erlang:list_to_existing_atom(\"match_flatmap_a_second\"),K1=erlang:list_to_atom(\"match_flatmap_z_first\"),K2=erlang:list_to_atom(\"match_flatmap_a_second\"),true=K1>K2,{ok,first,[],[]}=erlang:match_spec_test([],[{'_',[],[#{K2=>{message,second},K1=>{message,first}}]}],trace),done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "erlang-match-flatmap-dynamic-a-before-z",
            "-module(match_flatmap_order_contract). -export([run/0]). run()->{'EXIT',{badarg,_}}=catch erlang:list_to_existing_atom(\"match_flatmap_a_first_inverse\"),{'EXIT',{badarg,_}}=catch erlang:list_to_existing_atom(\"match_flatmap_z_second_inverse\"),K1=erlang:list_to_atom(\"match_flatmap_a_first_inverse\"),K2=erlang:list_to_atom(\"match_flatmap_z_second_inverse\"),true=K1<K2,{ok,first,[],[]}=erlang:match_spec_test([],[{'_',[],[#{K2=>{message,second},K1=>{message,first}}]}],trace),done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "erlang-match-flatmap-numeric-diagnostic-order",
            "-module(match_flatmap_order_contract). -export([run/0]). run()->{error,[{error,\"Variable $2 is unbound.\"},{error,\"Variable $1 is unbound.\"}]}=erlang:match_spec_test(any,[{'_',[],[#{1=>'$1',2=>'$2'}]}],table),done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "erlang-match-flatmap-dynamic-diagnostic-order",
            "-module(match_flatmap_order_contract). -export([run/0]). run()->{'EXIT',{badarg,_}}=catch erlang:list_to_existing_atom(\"match_flatmap_z_first_diagnostic\"),{'EXIT',{badarg,_}}=catch erlang:list_to_existing_atom(\"match_flatmap_a_second_diagnostic\"),K1=erlang:list_to_atom(\"match_flatmap_z_first_diagnostic\"),K2=erlang:list_to_atom(\"match_flatmap_a_second_diagnostic\"),{error,[{error,\"Variable $2 is unbound.\"},{error,\"Variable $1 is unbound.\"}]}=erlang:match_spec_test(any,[{'_',[],[#{K1=>'$1',K2=>'$2'}]}],table),done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "erlang-match-flatmap-numeric-nested-tuple-actions",
            "-module(match_flatmap_order_contract). -export([run/0]). run()->{ok,first,[],[]}=erlang:match_spec_test([],[{'_',[],[#{1=>{{{message,first},{message,middle}}},2=>{message,last}}]}],trace),done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        )
    ];
}
