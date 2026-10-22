-module(gen_status_legacy391).
-export([format_status/2]).

format_status(crash, _Arguments) -> erlang:error(format_status_failure);
format_status(throw_value, Arguments) -> throw({legacy, Arguments});
format_status(Option, Arguments) -> {legacy, Option, Arguments}.
