-module(beam_internal_ports416).
-export([run/0, run_stacks/0, child/3]).

cases() -> [open_nil, open_atom, open_tuple, open_options, command_nil, command_flags, command_data, command_missing, close_nil, close_missing, close_integer, close_tuple, connect_nil, connect_missing, connect_nil_self, connect_bad_pid, info_nil, info_integer, info_missing, info_tuple, info2_nil, info2_missing_item, info2_missing_name, info2_integer, undef_open, undef_command, undef_connect, undef_close, undef_info].

run() -> [capture(Name, default) || Name <- cases()].

run_stacks() ->
    [{Name, [{Depth, capture(Name, Depth)} || Depth <- [default, 0, 1, 2, 8]]}
     || Name <- cases()].

capture(Name, default) -> capture_child(Name);
capture(Name, Depth) ->
    Previous = erlang:system_flag(backtrace_depth, Depth),
    try capture_child(Name)
    after erlang:system_flag(backtrace_depth, Previous)
    end.

capture_child(Name) ->
    {Pid, Ref} = spawn_monitor(?MODULE, child, [self(), Name, undefined]),
    receive
        {Pid, Outcome} ->
            receive {'DOWN', Ref, process, Pid, normal} -> {Name, Outcome} end;
        {'DOWN', Ref, process, Pid, Reason} -> erlang:error({collector_child_failed, Name, Reason})
    after 5000 -> erlang:error({collector_child_timeout, Name})
    end.

child(Parent, Name, _) ->
    Outcome = try probe(Name) of
                  Value -> {value, Value}
              catch Class:Reason:Trace -> {exception, Class, Reason, Trace}
              end,
    Parent ! {self(), Outcome}.

probe(open_nil) -> erts_internal:open_port([], []);
probe(open_atom) -> erts_internal:open_port(missing_internal_driver416, []);
probe(open_tuple) -> erts_internal:open_port({}, []);
probe(open_options) -> erts_internal:open_port({fd,-1,-1}, invalid_options416);
probe(command_nil) -> erts_internal:port_command([], [], []);
probe(command_flags) -> erts_internal:port_command(missing_internal_port416, [], invalid_flags416);
probe(command_data) -> erts_internal:port_command(missing_internal_port416, {}, []);
probe(command_missing) -> erts_internal:port_command(missing_internal_port416, <<1,2>>, []);
probe(close_nil) -> erts_internal:port_close([]);
probe(close_missing) -> erts_internal:port_close(missing_internal_port416);
probe(close_integer) -> erts_internal:port_close(0);
probe(close_tuple) -> erts_internal:port_close({});
probe(connect_nil) -> erts_internal:port_connect([], []);
probe(connect_missing) -> erts_internal:port_connect(missing_internal_port416, self());
probe(connect_nil_self) -> erts_internal:port_connect([], self());
probe(connect_bad_pid) -> erts_internal:port_connect(missing_internal_port416, invalid_pid416);
probe(info_nil) -> erts_internal:port_info([]);
probe(info_integer) -> erts_internal:port_info(0);
probe(info_missing) -> erts_internal:port_info(missing_internal_port416);
probe(info_tuple) -> erts_internal:port_info({});
probe(info2_nil) -> erts_internal:port_info([], name);
probe(info2_missing_item) -> erts_internal:port_info(missing_internal_port416, []);
probe(info2_missing_name) -> erts_internal:port_info(missing_internal_port416, name);
probe(info2_integer) -> erts_internal:port_info(0, []);
probe(undef_open) -> erts_internal:open_port(a,b,c);
probe(undef_command) -> erts_internal:port_command(a,b,c,d);
probe(undef_connect) -> erts_internal:port_connect(a,b,c);
probe(undef_close) -> erts_internal:port_close(a,b);
probe(undef_info) -> erts_internal:port_info(a,b,c).
