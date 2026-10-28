-module(contract_nif_subject509).
-export([load/2, ping/0, plain/0, hold/2, version/0]).
-nifs([ping/0]).
-ifndef(CONTRACT509_VERSION).
-define(CONTRACT509_VERSION, 1).
-endif.
load(Path, Information) -> erlang:load_nif(Path, Information).
ping() -> erlang:nif_error(not_loaded509).
plain() -> plain509.
version() -> ?CONTRACT509_VERSION.
hold(Parent, Token) ->
    Parent ! {old_code_ready509, Token, self(), version()},
    receive
        {old_code_load509, From, Ref, Path} ->
            Value = contract_control509:capture(fun() -> erlang:load_nif(Path, From) end),
            From ! {old_code_result509, Ref, self(), Value}
    end.
