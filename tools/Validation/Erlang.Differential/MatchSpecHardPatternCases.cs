namespace Erlang.Differential;

public static class MatchSpecHardPatternCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } =
    [
        new(
            "erlang-match-hard-pattern-direct-variable-guard-body",
            "-module(match_hard_pattern_contract). -export([run/0]). run()->{error,[{error,\"Variable found in map key.\"}]}=erlang:match_spec_test(#{},[{#{'$1'=>a},[{unknown_guard}],[{unknown_body}]}],table),done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "erlang-match-hard-pattern-direct-wildcard-guard-empty-body",
            "-module(match_hard_pattern_contract). -export([run/0]). run()->{error,[{error,\"Underscore found in map key.\"}]}=erlang:match_spec_test(#{},[{#{'_'=>a},['$2'],[]}],table),done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "erlang-match-hard-pattern-tuple-nested-variable",
            "-module(match_hard_pattern_contract). -export([run/0]). run()->{error,[{error,\"Variable found in map key.\"}]}=erlang:match_spec_test({#{}},[{{#{'$1'=>a}},[{unknown_guard}],[true]}],table),done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "erlang-match-hard-pattern-list-nested-wildcard",
            "-module(match_hard_pattern_contract). -export([run/0]). run()->{error,[{error,\"Underscore found in map key.\"}]}=erlang:match_spec_test([#{}],[{[#{'_'=>a}],[],[{unknown_body}]}],table),done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "erlang-match-hard-pattern-map-value-nested-variable",
            "-module(match_hard_pattern_contract). -export([run/0]). run()->{error,[{error,\"Variable found in map key.\"}]}=erlang:match_spec_test(#{outer=>#{}},[{#{outer=>#{'$1'=>a}},[{unknown_guard}],[true]}],table),done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "erlang-match-hard-pattern-hard-first-clause-stops-later",
            "-module(match_hard_pattern_contract). -export([run/0]). run()->{error,[{error,\"Variable found in map key.\"}]}=erlang:match_spec_test(any,[{#{'$1'=>a},[],[true]},{'_',[{unknown_guard}],[{unknown_body}]}],table),done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "erlang-match-hard-pattern-hard-second-clause-stops-third",
            "-module(match_hard_pattern_contract). -export([run/0]). run()->{error,[{error,\"Variable found in map key.\"}]}=erlang:match_spec_test(any,[{'_',[],[true]},{#{'$1'=>a},[],[true]},{'_',[{unknown_guard}],[{unknown_body}]}],table),done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "erlang-match-hard-pattern-recoverable-guard-body-order",
            "-module(match_hard_pattern_contract). -export([run/0]). run()->{error,[{error,\"Function unknown_guard/0 does_not_exist.\"},{error,\"Function unknown_body/0 does_not_exist.\"}]}=erlang:match_spec_test(any,[{'_',[{unknown_guard}],[{unknown_body}]}],table),done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "erlang-match-hard-pattern-recoverable-guard-empty-body-order",
            "-module(match_hard_pattern_contract). -export([run/0]). run()->{error,[{error,\"Function unknown_guard/0 does_not_exist.\"},{error,\"Body clause does not return anything.\"}]}=erlang:match_spec_test(any,[{'_',[{unknown_guard}],[]}],table),done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "erlang-match-hard-pattern-recoverable-clause-stops-later",
            "-module(match_hard_pattern_contract). -export([run/0]). run()->{error,[{error,\"Function unknown_guard/0 does_not_exist.\"},{error,\"Function unknown_body/0 does_not_exist.\"}]}=erlang:match_spec_test(any,[{'_',[{unknown_guard}],[{unknown_body}]},{'_',[true],['$2']}],table),done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "erlang-match-hard-pattern-invalid-trace-head-stops-expressions",
            "-module(match_hard_pattern_contract). -export([run/0]). run()->{error,[{error,\"Match head is invalid in this context.\"}]}=erlang:match_spec_test([],[{bad,[{unknown_guard}],[{unknown_body}]}],trace),done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "erlang-match-hard-pattern-tuple-deferred-composite-order",
            "-module(match_hard_pattern_contract). -export([run/0]). run()->{error,[{error,\"Underscore found in map key.\"}]}=erlang:match_spec_test({#{},#{}},[{{#{'$1'=>a},#{'_'=>b}},[],[true]}],table),done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "erlang-match-hard-pattern-list-deferred-composite-order",
            "-module(match_hard_pattern_contract). -export([run/0]). run()->{error,[{error,\"Underscore found in map key.\"}]}=erlang:match_spec_test([#{},#{}],[{[#{'$1'=>a},#{'_'=>b}],[],[true]}],table),done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "erlang-match-hard-pattern-literal-compound-map-key",
            "-module(match_hard_pattern_contract). -export([run/0]). run()->{ok,true,[],[]}=erlang:match_spec_test(#{{'$1'}=>value},[{#{{'$1'}=>value},[],[true]}],table),done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        )
    ];
}
