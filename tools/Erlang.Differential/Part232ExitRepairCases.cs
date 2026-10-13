namespace Erlang.Differential;

public static class Part232ExitRepairCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body) => cases.Add(new(
            "erlang-seq-exit-repair-" + name,
            "-module(seq_exit_repair). -export([run/0]). run()->process_flag(trap_exit,true),S=self()," + body + ",process_flag(trap_exit,false),done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));
        Add(
            "dying-self-tracer-two-linked-token-copies",
            "A=spawn(fun()->process_flag(trap_exit,true),receive {'EXIT',P,normal}->{serial,{3,N}}=erlang:seq_trace_info(serial),{1,child,N,P,3}=erlang:seq_trace(sequential_trace_token,[]),S!{a,P,N} after 5000->error(no_a_exit) end end),B=spawn(fun()->process_flag(trap_exit,true),receive {'EXIT',P,normal}->{serial,{3,N}}=erlang:seq_trace_info(serial),{1,child,N,P,3}=erlang:seq_trace(sequential_trace_token,[]),S!{b,P,N} after 5000->error(no_b_exit) end end),P=spawn(fun()->link(A),link(B),false=erlang:system_flag(sequential_tracer,self()),erlang:seq_trace(label,child),erlang:seq_trace(serial,{3,10}),erlang:seq_trace(send,true),ok end),N1=receive {a,P,NA}->NA after 5000->error(no_a_reply) end,N2=receive {b,P,NB}->NB after 5000->error(no_b_reply) end,[11,12]=lists:sort([N1,N2]),false=erlang:system_flag(sequential_tracer,false)"
        );
        Add(
            "live-self-tracer-send-print-suppression-then-death-advance",
            "P=spawn_link(fun()->receive begin_trace->false=erlang:system_flag(sequential_tracer,self()),erlang:seq_trace(label,child),erlang:seq_trace(serial,{3,10}),erlang:seq_trace(send,true),true=erlang:seq_trace_print(hidden),{serial,{3,10}}=erlang:seq_trace_info(serial),S!{ready,self()},{serial,{3,10}}=erlang:seq_trace_info(serial),receive finish->ok after 5000->error(no_finish) end after 5000->error(no_begin) end end),_=erlang:send_after(0,P,begin_trace),receive {ready,P}->{serial,{3,10}}=erlang:seq_trace_info(serial),{1,child,10,P,3}=erlang:seq_trace(sequential_trace_token,[]) after 5000->error(no_ready) end,_=erlang:send_after(0,P,finish),receive {'EXIT',P,normal}->{serial,{3,11}}=erlang:seq_trace_info(serial),{1,child,11,P,3}=erlang:seq_trace(sequential_trace_token,[]) after 5000->error(no_exit) end,false=erlang:system_flag(sequential_tracer,false)"
        );

        return cases.AsReadOnly();
    }
}
