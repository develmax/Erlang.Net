-module(gen_status_callback391).
-export([format_status/1]).

format_status(#{mode := crash}) -> erlang:error(format_status_failure);
format_status(#{mode := bad_return}) -> invalid_return;
format_status(#{mode := unknown_key}) -> #{unexpected => forbidden};
format_status(Status) -> Status.
