-module(beam_guarded_stat_probe).
-export([callback/2]).
callback(Socket, Controller) ->
    fun(Ctrl) when Ctrl =:= Controller ->
        case beam_stat_transport:getstat(Socket, [recv_cnt, send_cnt, send_pend]) of
            {ok, Stat} -> split_stat(Stat, 0, 0, 0);
            Other -> Other
        end
    end.

split_stat(Stat, _Recv, _Send, _Pending) -> {Stat, 0, 0, 0}.
