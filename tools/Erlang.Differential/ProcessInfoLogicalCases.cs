namespace Erlang.Differential;

public static class ProcessInfoLogicalCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } =
    [
        new(
            "process-info-logical-small-bump",
            "-module(process_info_logical). -export([run/0]). run()->{reductions,A}=process_info(self(),reductions),true=erlang:bump_reductions(0),true=erlang:bump_reductions(1),{reductions,B}=process_info(self(),reductions),true=is_integer(B),true=(B>=A),done.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "process-info-logical-bump-after-yield",
            "-module(process_info_logical). -export([run/0]). run()->{reductions,A}=process_info(self(),reductions),true=erlang:yield(),{reductions,B}=process_info(self(),reductions),true=(B>=A),true=erlang:bump_reductions(4000),{reductions,C}=process_info(self(),reductions),true=(C>=B),done.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "process-info-logical-bump-error-domain",
            "-module(process_info_logical). -export([run/0]). run()->ok=reject(fun()->erlang:bump_reductions(-1) end),ok=reject(fun()->erlang:bump_reductions(not_integer) end),ok=reject(fun()->erlang:bump_reductions(1152921504606846976) end),done. reject(F)->try F() of _->unexpected catch error:badarg->ok end.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "process-info-logical-defaults",
            "-module(process_info_logical). -export([run/0]). run()->{error_handler,error_handler}=process_info(self(),error_handler),{priority,normal}=process_info(self(),priority),done.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "process-info-logical-charged-reductions-monotonic",
            "-module(process_info_logical). -export([run/0]). run()->{reductions,A}=process_info(self(),reductions),true=is_integer(A),true=(A>=0),true=erlang:bump_reductions(4000),{reductions,B}=process_info(self(),reductions),true=is_integer(B),true=(B>=A),done.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "process-info-logical-spawn-module-initial-mfa",
            "-module(process_info_logical). -export([run/0,child/1,nested/1]). run()->{P,R}=spawn_monitor(process_info_logical,child,[self()]),receive {P,{initial_call,{process_info_logical,child,1}}}->ok end,receive {'DOWN',R,process,P,normal}->done end. child(Owner)->nested(Owner). nested(Owner)->Owner!{self(),process_info(self(),initial_call)},ok.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "process-info-logical-spawn-callback-initial-mfa",
            "-module(process_info_logical). -export([run/0]). run()->Owner=self(),{P,R}=spawn_monitor(fun()->Owner!{self(),process_info(self(),initial_call)} end),receive {P,{initial_call,{erlang,apply,2}}}->ok end,receive {'DOWN',R,process,P,normal}->done end.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "process-info-logical-logical-list-order-and-duplicates",
            "-module(process_info_logical). -export([run/0]). run()->[{priority,normal},{error_handler,error_handler},{priority,normal},{reductions,R}]=process_info(self(),[priority,error_handler,priority,reductions]),true=is_integer(R),done.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "process-info-logical-suspending-empty",
            "-module(process_info_logical). -export([run/0]). run()->{suspending,[]}=process_info(self(),suspending),done.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            "process-info-logical-suspending-active-counts",
            "-module(process_info_logical). -export([run/0]). run()->{P,R}=spawn_monitor(fun()->receive stop->ok end end),true=erlang:suspend_process(P),{suspending,[{P,1,0}]}=process_info(self(),suspending),true=erlang:suspend_process(P),{suspending,[{P,2,0}]}=process_info(self(),suspending),true=erlang:resume_process(P),{suspending,[{P,1,0}]}=process_info(self(),suspending),true=erlang:resume_process(P),{suspending,[]}=process_info(self(),suspending),P!stop,receive {'DOWN',R,process,P,normal}->done end.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
        )
    ];
}
