-module(beam_new_connection_observer400).
-export([cases/0,run/0,run_stacks/0,worker/1]).

cases() -> [valid_ordinary, valid_local_chars, valid_host_unicode, valid_host_punctuation, repeat_pending, nil, integer, tuple, map, binary, empty_atom, no_separator, empty_local, empty_host, second_separator, invalid_local, own_node].

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

value(valid_ordinary) -> probe(newconn400@localhost);
value(valid_local_chars) -> probe('A0_z-@host');
value(valid_host_unicode) -> probe('newconn400@хост');
value(valid_host_punctuation) -> probe('newconn400@a b!');
value(repeat_pending) -> probe(newconn400@localhost);
value(nil) -> erts_internal:new_connection([]);
value(integer) -> erts_internal:new_connection(0);
value(tuple) -> erts_internal:new_connection({node,host});
value(map) -> erts_internal:new_connection(#{node=>host});
value(binary) -> erts_internal:new_connection(<<"node@host">>);
value(empty_atom) -> erts_internal:new_connection('');
value(no_separator) -> erts_internal:new_connection(node);
value(empty_local) -> erts_internal:new_connection('@host');
value(empty_host) -> erts_internal:new_connection('node@');
value(second_separator) -> erts_internal:new_connection('node@host@tail');
value(invalid_local) -> erts_internal:new_connection('bad.name@host');
value(own_node) -> erts_internal:new_connection(node()).

probe(Node) ->
    First = {Id,Ref} = erts_internal:new_connection(Node),
    Again = erts_internal:new_connection(Node),
    {is_integer(Id),is_reference(Ref),Id >= 0 andalso Id =< 16#ffffff,
        First =:= Again,erlang:is_alive()}.
