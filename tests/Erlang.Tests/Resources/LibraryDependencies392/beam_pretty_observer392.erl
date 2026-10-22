-module(beam_pretty_observer392).
-export([cases/0,run/0,run_stacks/0,worker/1,prefix16/1,prefix32/1,prefix_offset/1,need_prefix/1]).

cases() -> [prefix16_match, prefix16_mismatch, prefix16_short, prefix16_unaligned, prefix16_nonbyte_tail, prefix16_error, prefix32_match, prefix32_mismatch, original_pretty_narrow, original_pretty_wide].

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

value(prefix16_match) -> prefix16(<<16#3c,16#3c,88>>);
value(prefix16_mismatch) -> prefix16(<<16#3c,16#3e,88>>);
value(prefix16_short) -> prefix16(<<16#3c>>);
value(prefix16_unaligned) -> prefix_offset(<<0:3,16#3c,16#3c,88>>);
value(prefix16_nonbyte_tail) -> prefix16(<<16#3c,16#3c,1:1>>);
value(prefix16_error) -> need_prefix(<<16#3c,16#3e>>);
value(prefix32_match) -> prefix32(<<16#12345678:32,88>>);
value(prefix32_mismatch) -> prefix32(<<16#12345679:32,88>>);
value(original_pretty_narrow) -> io_lib_pretty:print_bin(binary:copy(<<0,1,255>>,12),[{line_length,12},{strings,false},{encoding,latin1}]);
value(original_pretty_wide) -> io_lib_pretty:print_bin(<<0,1,255>>,[{line_length,80},{strings,false},{encoding,latin1}]).

prefix16(Binary) ->
    case Binary of
        <<16#3c3c:16, Rest/binary>> -> {matched,Rest};
        _ -> nomatch
    end.

prefix32(Binary) ->
    case Binary of
        <<16#12345678:32, Rest/binary>> -> {matched,Rest};
        _ -> nomatch
    end.

prefix_offset(Bits) ->
    case Bits of
        <<_:3,16#3c3c:16, Rest/bitstring>> -> {matched,Rest};
        _ -> nomatch
    end.

need_prefix(Binary) ->
    case Binary of
        <<16#3c3c:16, Rest/binary>> -> Rest;
        _ -> erlang:error({prefix_failed,Binary})
    end.
