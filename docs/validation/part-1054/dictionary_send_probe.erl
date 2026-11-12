-module(dictionary_send_probe).
-export([run/1]).
run(Message) ->
    erlang:get(dictionary_send_probe) ! Message,
    ok.
