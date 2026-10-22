-module(beam_construct_order391).
-export([cases/0,run/0,run_stacks/0,worker/1,float_value/1,utf8_and_size/2,float_and_size/2]).

cases() -> [float_integer_overflow, utf8_type_then_negative_size, float_type_then_atom_size, valid_utf8_then_integer].

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

value(float_integer_overflow) -> float_value(1 bsl 20000);
value(utf8_type_then_negative_size) -> utf8_and_size(16#d800,-1);
value(float_type_then_atom_size) -> float_and_size(invalid,invalid_size);
value(valid_utf8_then_integer) -> utf8_and_size(16#3bb,8).

float_value(Value) -> <<Value:64/float>>.

utf8_and_size(Code, Size) -> <<Code/utf8, 0:Size>>.

float_and_size(Value, Size) -> <<Value:64/float, 0:Size>>.
