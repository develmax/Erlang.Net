namespace Erlang.Differential;

public static class ProcessDelayCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    public static string? FixtureName(string function, int arity) => function == ProcessDelayNames.Function && arity == 2 ? "erlang-delay-zero" : null;

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body) => cases.Add(new(
            "erlang-delay-" + name,
            "-module(delay_contract). -export([run/0]). run()->" + body + ",done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));
        Add("zero", "{kept,42}=erlang:delay_trap({kept,42},0)");
        Add("finite", "kept=erlang:delay_trap(kept,1)");
        Add("zero-keeps-message", "self()!message,kept=erlang:delay_trap(kept,0),receive message->ok after 0->error(consumed) end");
        Add(
            "finite-keeps-messages",
            "self()!first,self()!second,kept=erlang:delay_trap(kept,2),receive first->ok after 0->error(first_consumed) end,receive second->ok after 0->error(second_consumed) end"
        );
        foreach (string timeout in new[] { "-1", "0.0", "3.14", "4294967296", "18446744073709551616", "-18446744073709551616", "wrong", "[]", "<<>>" })
            Add(
                "invalid-" + cases.Count,
                "try erlang:delay_trap(kept," + timeout + ") of _->error(accepted) catch error:timeout_value->ok end"
            );
        foreach (string timeout in new[] { "infinity", "4294967295", "4294967294", "2147483648" })
            Add(
                "kill-long-" + timeout,
                "S=self(),{P,R}=spawn_monitor(fun()->S!{ready,self()},erlang:delay_trap(kept," + timeout + ") end),receive {ready,P}->ok after 5000->error(no_ready) end,receive {'DOWN',R,process,P,_}->error(early) after 20->ok end,exit(P,kill),receive {'DOWN',R,process,P,killed}->ok after 5000->error(no_killed) end"
            );
        Add(
            "receive-after-maximum",
            "S=self(),{P,R}=spawn_monitor(fun()->S!{ready,self()},receive finish->S!finished after 4294967295->error(expired) end end),receive {ready,P}->ok after 5000->error(no_ready) end,P!finish,receive finished->ok after 5000->error(no_finish) end,receive {'DOWN',R,process,P,normal}->ok after 5000->error(no_normal) end"
        );

        return cases;
    }
}
