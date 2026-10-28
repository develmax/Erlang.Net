-module(contract_proc_subject509).
-export([held/2, fail/2, hibernate/3, woke/3]).

held(Parent, Token) ->
    put({report_key509, Token}, {report_value509, self()}),
    Parent ! {proc_ready509, Token, self(), get('$ancestors'), get('$initial_call')},
    receive {finish509, Token} -> ok end.

fail(Class, Reason) ->
    put(report_payload509, {Class, Reason}),
    self() ! {unhandled_report_message509, Reason},
    erlang:raise(Class, Reason, []).

hibernate(Parent, Token, Mode) ->
    put(hibernate_kept509, {Token, Mode}),
    Parent ! {pre_hibernate509, Token, self(), get('$ancestors'), get('$initial_call')},
    proc_lib:hibernate(?MODULE, woke, [Parent, Token, Mode]).

woke(Parent, Token, Mode) ->
    receive {wake509, Token} -> ok end,
    Parent ! {woke509, Token, self(), get(hibernate_kept509), get('$ancestors'), get('$initial_call')},
    case Mode of
        return509 -> held(Parent, Token);
        {Class, Reason} -> fail(Class, Reason)
    end.
