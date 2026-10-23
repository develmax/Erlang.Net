-module(ddll_tcp449).
-export([start/1]).
start([Ready]) ->
    {ok,L}=gen_tcp:listen(0,[binary,{packet,4},{active,false},{ip,{127,0,0,1}},{reuseaddr,false}]),
    {ok,{_,Port}}=inet:sockname(L),
    ok=file:write_file(Ready,integer_to_binary(Port),[exclusive]),
    {ok,S}=gen_tcp:accept(L,60000),ok=gen_tcp:close(L),
    Worker=beam_ddll_distribution449:new(),true=register(ddll449_worker,Worker),loop(S,Worker).
loop(S,Worker) ->
    case gen_tcp:recv(S,0,60000) of
        {ok,Bytes} ->
            case binary_to_term(Bytes) of
                stop -> M=monitor(process,Worker),Worker!stop,
                    receive {'DOWN',M,process,Worker,normal}->ok after 5000->error(worker_stop_timeout) end,
                    ok=gen_tcp:send(S,term_to_binary(stopped)),gen_tcp:close(S),halt();
                {Function,Arguments} ->
                    Result=case {Function,Arguments} of
                        {connect_peer,[Node]} -> {ok,net_kernel:connect_node(Node)};
                        {set_peer_cookie,[Node,Cookie]} -> {ok,erlang:set_cookie(Node,Cookie)};
                        _ -> beam_ddll_distribution449:request(Worker,Function,Arguments)
                    end,
                    ok=gen_tcp:send(S,term_to_binary(Result,[{minor_version,2}])),loop(S,Worker)
            end;
        {error,closed}->halt(2);
        Other->error({socket_failure,Other})
    end.
