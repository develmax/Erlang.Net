-module(beam_unicode_match_observer392).
-export([cases/0,run/0,run_stacks/0,worker/1,fixed/1,offset/1]).

cases() -> [fixed8192, fixed8192_short, fixed8192_tail, fixed8192_unaligned, original_utf16_chunk, original_utf16_split_surrogate, original_latin_output_chunk].

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

value(fixed8192) -> fixed(binary:copy(<<65>>,8192));
value(fixed8192_short) -> fixed(binary:copy(<<65>>,8191));
value(fixed8192_tail) -> fixed(binary:copy(<<65>>,8193));
value(fixed8192_unaligned) -> offset(binary:copy(<<65>>,8192));
value(original_utf16_chunk) -> unicode:characters_to_binary(binary:copy(<<0,65>>,4097),{utf16,big});
value(original_utf16_split_surrogate) -> unicode:characters_to_list([binary:copy(<<0,65>>,4095),<<216,61,222>>,<<0>>],{utf16,big});
value(original_latin_output_chunk) -> unicode:characters_to_binary(binary:copy(<<194,128>>,4097),utf8,latin1).

fixed(Binary) ->
    case Binary of
        <<Chunk:8192/binary,Tail/bitstring>> -> {Chunk,Tail};
        _ -> no_match
    end.

offset(Binary) ->
    Bits = <<5:3,Binary/binary,1:1>>,
    case Bits of
        <<_:3,Chunk:8192/binary,Tail/bitstring>> -> {Chunk,Tail};
        _ -> no_match
    end.
