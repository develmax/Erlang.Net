-module(beam_timed_monitor_probe).
-export([close_listen/2, close/2]).

-record(socket, {handle, config}).
-record(config, {options}).

close_listen(#socket{handle = {Pid, OldPort}, config = #config{options = Options}}, Timeout) ->
    Address = proplists:get_value(ip, Options, default_address(Options)),
    Port = real_port(Pid, OldPort),
    beam_monitor_transport:register_listener({undefined, Pid}, Address, Port),
    case beam_monitor_transport:close(Pid) of
        stop ->
            erlang:monitor(process, Pid),
            receive
                {'DOWN', _, process, Pid, _} -> ok
            after Timeout ->
                {error, timeout}
            end;
        waiting ->
            ok;
        Error ->
            Error
    end.

default_address(Options) -> beam_monitor_transport:default_address(Options).
real_port(_, Port) -> Port.

close(gen_udp, {_Client, _Socket}) -> ok;
close(Transport, {_Client, Socket}) -> Transport:close(Socket).
