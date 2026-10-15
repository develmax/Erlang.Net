namespace Erlang.Differential;

public static class ProcessInfoDefaultSubsetCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } =
    [
        new(
            ProcessInfoDefaultSubsetCaseNames.CommonFields,
            "-module(default_process_info_subset_contract). -export([run/0]). run()->I=process_info(self()),lists:foreach(fun(K)->{K,_}=lists:keyfind(K,1,I) end,[current_function,initial_call,status,message_queue_len,links,dictionary,trap_exit,error_handler,priority,group_leader,reductions,suspending]),false=lists:keymember(registered_name,1,I),done.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            ProcessInfoDefaultSubsetCaseNames.CommonTypes,
            "-module(default_process_info_subset_contract). -export([run/0]). run()->I=process_info(self()),lists:foreach(fun(K)->{K,V}=lists:keyfind(K,1,I),true=is_integer(V),true=(V>=0) end,[message_queue_len,reductions]),{trap_exit,B}=lists:keyfind(trap_exit,1,I),true=is_boolean(B),{group_leader,G}=lists:keyfind(group_leader,1,I),true=is_pid(G),lists:foreach(fun(K)->{K,V}=lists:keyfind(K,1,I),true=is_list(V) end,[links,dictionary,suspending]),done.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            ProcessInfoDefaultSubsetCaseNames.RegisteredName,
            "-module(default_process_info_subset_contract). -export([run/0]). run()->true=register(default_subset_contract,self()),I=process_info(self()),{registered_name,default_subset_contract}=lists:keyfind(registered_name,1,I),{registered_name,default_subset_contract}=process_info(self(),registered_name),done.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            ProcessInfoDefaultSubsetCaseNames.Query2StableProjection,
            "-module(default_process_info_subset_contract). -export([run/0]). run()->I=process_info(self()),lists:foreach(fun(K)->T=process_info(self(),K),T=lists:keyfind(K,1,I) end,[trap_exit,error_handler,priority,group_leader,dictionary]),done.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            ProcessInfoDefaultSubsetCaseNames.InvalidTargets,
            "-module(default_process_info_subset_contract). -export([run/0]). run()->lists:foreach(fun(V)->ok=try process_info(V) of _->unexpected catch error:badarg->ok end end,[0,ok,{},[],make_ref(),<<>>,fun()->ok end]),done.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            ProcessInfoDefaultSubsetCaseNames.DeadLocal,
            "-module(default_process_info_subset_contract). -export([run/0]). run()->P=spawn(fun()->ok end),R=monitor(process,P),receive {'DOWN',R,process,P,_}->ok end,undefined=process_info(P),done.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            ProcessInfoDefaultSubsetCaseNames.StaleLocal,
            "-module(default_process_info_subset_contract). -export([run/0]). run()->B=term_to_binary(self()),<<131,88,_/binary>>=B,S=byte_size(B)-4,<<H:S/binary,C:32>>=B,P=binary_to_term(<<H/binary,(C bxor 1):32>>),true=is_pid(P),undefined=process_info(P),done.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ),
        new(
            ProcessInfoDefaultSubsetCaseNames.RemoteTarget,
            "-module(default_process_info_subset_contract). -export([run/0]). run()-><<131,A/binary>>=term_to_binary(default_subset_remote@invalid),P=binary_to_term(<<131,88,A/binary,1:32,0:32,0:32>>),true=is_pid(P),ok=try process_info(P) of _->unexpected catch error:badarg->ok end,done.",
            Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(CompiledContractOutcomeNames.Completion))
        )
    ];
}
