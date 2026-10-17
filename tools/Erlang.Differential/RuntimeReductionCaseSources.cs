namespace Erlang.Differential;

internal static class RuntimeReductionCaseSources
{
    internal const string Monotonic = """
        -module(stats_reduction_work).
        -export([run/0,work/1]).
        run() ->
            {A,DA}=erlang:statistics(reductions), true=is_integer(A), true=(A>=0), true=(DA>=0),
            ok=work(10000), true=erlang:yield(),
            {B,DB}=erlang:statistics(reductions), true=(B>A), true=(DB=:=B-A), done.
        work(0)->ok;
        work(N)->work(N-1).

        """;

    internal const string Independent = """
        -module(stats_reduction_cursors).
        -export([run/0,work/1]).
        run() ->
            {R1,_}=erlang:statistics(reductions), {E1,_}=erlang:statistics(exact_reductions),
            ok=work(10000), true=erlang:yield(),
            {R2,DR}=erlang:statistics(reductions), {E2,DE}=erlang:statistics(exact_reductions),
            true=(R2>=R1), true=(E2>=E1), true=(DR=:=R2-R1), true=(DE=:=E2-E1), done.
        work(0)->ok;
        work(N)->work(N-1).

        """;

    internal const string Retired = """
        -module(stats_reduction_retired).
        -export([run/0,work/1]).
        run() ->
            {A,_}=erlang:statistics(exact_reductions),
            {P,R}=spawn_monitor(stats_reduction_retired,work,[10000]),
            receive {'DOWN',R,process,P,normal}->ok end,
            {B,D}=erlang:statistics(exact_reductions), true=(B>A), true=(D=:=B-A), done.
        work(0)->ok;
        work(N)->work(N-1).

        """;

}
