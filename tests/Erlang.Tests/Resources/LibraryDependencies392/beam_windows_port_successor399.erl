-module(beam_windows_port_successor399).
-export([cases/0,run/0,run_stacks/0,worker/1]).

cases() -> [bare_executable, full_executable, ordinary_spawn, full_empty_environment, invalid_option, invalid_specification].

run() ->
    {Child, Ref} = erlang:spawn_monitor(?MODULE, worker, [self()]),
    receive
        {Child, Values} ->
            receive
                {'DOWN', Ref, process, Child, normal} -> Values;
                {'DOWN', Ref, process, Child, Reason} -> erlang:error({child_exit, Reason})
            after 5000 -> erlang:error(child_not_settled)
            end;
        {'DOWN', Ref, process, Child, Reason} -> erlang:error({child_exit, Reason})
    after 30000 -> erlang:exit(Child, kill), erlang:error(child_timeout)
    end.

worker(Owner) ->
    Owner ! {self(), rows(cases())},
    ok.

rows([]) -> [];
rows([Case | Rest]) ->
    Outcome = observe(Case),
    [{Case, Outcome} | rows(Rest)].

run_stacks() ->
    Old = erlang:system_flag(backtrace_depth, 8),
    erlang:system_flag(backtrace_depth, Old),
    try
        Default = {default, run()},
        Zero = at_depth(0),
        One = at_depth(1),
        Two = at_depth(2),
        Eight = at_depth(8),
        [Default, Zero, One, Two, Eight]
    after erlang:system_flag(backtrace_depth, Old)
    end.

at_depth(Depth) ->
    erlang:system_flag(backtrace_depth, Depth),
    {Depth, run()}.

observe(Case) ->
    try value(Case) of Value -> {returned, Value}
    catch Class:Reason:Stack -> {caught, Class, Reason, Stack}
    end.

value(bare_executable) ->
    port({spawn_executable,"cmd.exe"}, [{args,["/d","/c","echo port_resolve_399"]}]);
value(full_executable) ->
    port({spawn_executable,executable()}, [{args,["/d","/c","echo port_resolve_399"]}]);
value(ordinary_spawn) ->
    port({spawn,"cmd.exe /d /c echo port_resolve_399"}, []);
value(full_empty_environment) ->
    port({spawn_executable,executable()}, [{args,["/d","/c","echo port_resolve_399"]},{env,[]}]);
value(invalid_option) ->
    erlang:open_port({spawn_executable,executable()}, [invalid_port_option_399]);
value(invalid_specification) ->
    erlang:open_port({invalid_port_specification_399,"unused"}, []).

executable() -> os:getenv("SystemRoot") ++ "\\System32\\cmd.exe".

port(Specification, Options) ->
    Port = erlang:open_port(Specification,[binary,eof,exit_status|Options]),
    try
        {Chunks, Status} = collect(Port, [], pending, false),
        Closed = erlang:port_close(Port),
        {erlang:iolist_to_binary(lists:reverse(Chunks)),Status,true,Closed,erlang:port_info(Port)}
    after
        case erlang:port_info(Port) of undefined -> ok; _ -> erlang:port_close(Port) end
    end.

collect(_Port, Chunks, Status, true) when Status =/= pending -> {Chunks,Status};
collect(Port, Chunks, Status, Eof) ->
    receive
        {Port,{data,Data}} -> collect(Port,[Data|Chunks],Status,Eof);
        {Port,{exit_status,Code}} -> collect(Port,Chunks,Code,Eof);
        {Port,eof} -> collect(Port,Chunks,Status,true)
    after 5000 -> erlang:error(port_not_settled)
    end.
