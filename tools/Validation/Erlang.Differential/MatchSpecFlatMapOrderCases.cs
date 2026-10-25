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
            "-module(match_flatmap_order_contract). -export([run/0]). run()->S=erlang:integer_to_list(erlang:unique_integer([positive,monotonic])),N1=\"match_flatmap_z_first_\"++S,N2=\"match_flatmap_a_second_\"++S,{'EXIT',{badarg,_}}=catch erlang:list_to_existing_atom(N1),{'EXIT',{badarg,_}}=catch erlang:list_to_existing_atom(N2),K1=erlang:list_to_atom(N1),K2=erlang:list_to_atom(N2),true=K1>K2,{ok,first,[],[]}=erlang:match_spec_test([],[{'_',[],[#{K2=>{message,second},K1=>{message,first}}]}],trace),done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "erlang-match-flatmap-dynamic-a-before-z",
            "-module(match_flatmap_order_contract). -export([run/0]). run()->S=erlang:integer_to_list(erlang:unique_integer([positive,monotonic])),N1=\"match_flatmap_a_first_inverse_\"++S,N2=\"match_flatmap_z_second_inverse_\"++S,{'EXIT',{badarg,_}}=catch erlang:list_to_existing_atom(N1),{'EXIT',{badarg,_}}=catch erlang:list_to_existing_atom(N2),K1=erlang:list_to_atom(N1),K2=erlang:list_to_atom(N2),true=K1<K2,{ok,first,[],[]}=erlang:match_spec_test([],[{'_',[],[#{K2=>{message,second},K1=>{message,first}}]}],trace),done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "erlang-match-flatmap-numeric-diagnostic-order",
            "-module(match_flatmap_order_contract). -export([run/0]). run()->{error,[{error,\"Variable $2 is unbound.\"},{error,\"Variable $1 is unbound.\"}]}=erlang:match_spec_test(any,[{'_',[],[#{1=>'$1',2=>'$2'}]}],table),done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "erlang-match-flatmap-dynamic-diagnostic-order",
            "-module(match_flatmap_order_contract). -export([run/0]). run()->S=erlang:integer_to_list(erlang:unique_integer([positive,monotonic])),N1=\"match_flatmap_z_first_diagnostic_\"++S,N2=\"match_flatmap_a_second_diagnostic_\"++S,{'EXIT',{badarg,_}}=catch erlang:list_to_existing_atom(N1),{'EXIT',{badarg,_}}=catch erlang:list_to_existing_atom(N2),K1=erlang:list_to_atom(N1),K2=erlang:list_to_atom(N2),{error,[{error,\"Variable $2 is unbound.\"},{error,\"Variable $1 is unbound.\"}]}=erlang:match_spec_test(any,[{'_',[],[#{K1=>'$1',K2=>'$2'}]}],table),done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "erlang-match-flatmap-numeric-nested-tuple-actions",
            "-module(match_flatmap_order_contract). -export([run/0]). run()->{ok,first,[],[]}=erlang:match_spec_test([],[{'_',[],[#{1=>{{{message,first},{message,middle}}},2=>{message,last}}]}],trace),done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        )
    ];
}
