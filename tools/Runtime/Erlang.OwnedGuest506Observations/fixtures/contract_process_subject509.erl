-module(contract_process_subject509).
-export([start/2, loop/1]).

start(Parent, Token) ->
    put({tuple_key509, 1}, {dictionary_value509, Token}),
    put(atom_key509, atom_value509),
    Setup = [contract_control509:capture(fun() -> process_flag(trap_exit, true) end),
             contract_control509:capture(fun() -> process_flag(save_calls, 16) end),
             contract_control509:capture(fun() -> proc_lib:set_label({subject509, Token}) end)],
    Parent ! {ready509, Token, self(), Setup},
    loop([]).

loop(Payload) ->
    receive
        {control509, From, Ref, snapshot, Items} ->
            From ! {reply509, Ref, self(), contract_control509:capture(fun() -> process_info(self(), Items) end)},
            loop(Payload);
        {control509, From, Ref, retain, _} ->
            Next = {lists:seq(1, 2048), binary:copy(<<17>>, 16384)},
            put(retained_payload509, Next),
            From ! {reply509, Ref, self(), retained509},
            loop(Next);
        {control509, From, Ref, drop, _} ->
            erase(retained_payload509),
            From ! {reply509, Ref, self(), dropped509},
            loop([]);
        {control509, From, Ref, gc, Type} ->
            Result = contract_control509:capture(fun() -> erlang:garbage_collect(self(), [{type, Type}]) end),
            From ! {reply509, Ref, self(), Result},
            loop(Payload);
        {control509, From, Ref, stop, _} ->
            From ! {reply509, Ref, self(), stopped509},
            ok
    end.
