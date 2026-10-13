namespace Erlang.Differential;

public static class RuntimeStatisticsCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    public static string? FixtureName(string function, int arity) => function == RuntimeStatisticsNames.Function && arity == 1 ? "erlang-statistics-export-statistics-1" : null;

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body, string helpers = "") => cases.Add(new(
            "erlang-statistics-" + name,
            "-module(statistics_contract). -export([run/0]). run()->" + body + ",done. " + helpers,
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));
        Add(
            "export-statistics-1",
            "{T,D}=erlang:statistics(wall_clock),true=is_integer(T),true=is_integer(D),true=T>=0,true=D>=0,true=D=<T"
        );
        Add(
            "wall-telescope",
            "{A,_}=erlang:statistics(wall_clock),{B,DB}=erlang:statistics(wall_clock),{C,DC}=erlang:statistics(wall_clock),true=B>=A,true=C>=B,true=DB=:=B-A,true=DC=:=C-B,true=(DB+DC)=:=C-A"
        );
        Add(
            "wall-actual-wait-progress",
            "{A,_}=erlang:statistics(wall_clock),receive after 20->ok end,{B,D}=erlang:statistics(wall_clock),true=B>A,true=D>0,true=D=:=B-A"
        );
        Add(
            "wall-global-cross-caller",
            "{A,_}=erlang:statistics(wall_clock),S=self(),P=spawn(fun()->receive sample->{B,D}=erlang:statistics(wall_clock),S!{sample,self(),B,D} end end),P!sample,receive {sample,P,B,DB}->{C,DC}=erlang:statistics(wall_clock),true=DB=:=B-A,true=DC=:=C-B,true=(DB+DC)=:=C-A end"
        );
        Add(
            "runtime-user-shape",
            "{T,D}=erlang:statistics(runtime),true=is_integer(T),true=is_integer(D),true=T>=0,true=D>=0,true=D=<T"
        );
        Add(
            "runtime-user-work-nondecreasing",
            "{A,_}=erlang:statistics(runtime),ok=burn(10000),{B,D}=erlang:statistics(runtime),true=B>=A,true=D>=0,true=D=:=B-A",
            "burn(0)->ok;burn(N)->burn(N-1)."
        );
        Add(
            "runtime-user-telescope",
            "{A,_}=erlang:statistics(runtime),{B,DB}=erlang:statistics(runtime),{C,DC}=erlang:statistics(runtime),true=B>=A,true=C>=B,true=DB=:=B-A,true=DC=:=C-B,true=(DB+DC)=:=C-A"
        );
        Add(
            "runtime-user-global-cross-caller",
            "{A,_}=erlang:statistics(runtime),S=self(),P=spawn(fun()->receive sample->{B,D}=erlang:statistics(runtime),S!{sample,self(),B,D} end end),P!sample,receive {sample,P,B,DB}->{C,DC}=erlang:statistics(runtime),true=DB=:=B-A,true=DC=:=C-B,true=(DB+DC)=:=C-A end"
        );
        Add(
            "wall-cursor-independent-of-runtime",
            "{A,_}=erlang:statistics(wall_clock),receive after 10->ok end,erlang:statistics(runtime),{B,D}=erlang:statistics(wall_clock),true=D=:=B-A,true=D>0"
        );
        Add(
            "runtime-cursor-independent-of-wall",
            "{A,_}=erlang:statistics(runtime),erlang:statistics(wall_clock),{B,D}=erlang:statistics(runtime),true=D=:=B-A"
        );
        foreach (string argument in new[] { "unknown_statistics_key", "0", "1.0", "[]", "[wall_clock]", "[wall_clock|tail]", "{}", "#{key=>wall_clock}", "<<1>>", "self()", "make_ref()", "fun()->ok end" })
            Add(
                "invalid-" + cases.Count,
                "A = " + argument + ",try erlang:statistics(A) of _->error(unexpected_success) catch error:badarg:Stack->[{erlang,statistics,[A],Info}|_]=Stack,true=lists:member({error_info,#{module=>erl_erts_errors}},Info) end"
            );
        Add(
            "invalid-does-not-reset-wall-cursor",
            "{A,_}=erlang:statistics(wall_clock),receive after 10->ok end,{'EXIT',{badarg,_}}=(catch erlang:statistics(unknown_statistics_key)),{B,D}=erlang:statistics(wall_clock),true=D=:=B-A,true=D>0"
        );
        Add(
            "invalid-does-not-reset-runtime-cursor",
            "{A,_}=erlang:statistics(runtime),{'EXIT',{badarg,_}}=(catch erlang:statistics({runtime})),{B,D}=erlang:statistics(runtime),true=D=:=B-A"
        );

        return cases;
    }
}
