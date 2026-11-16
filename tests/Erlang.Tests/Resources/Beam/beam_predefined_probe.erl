-module(beam_predefined_probe).
-export_type([example/0]).
-type example() :: atom().
-if(?FILE =:= "beam_predefined_probe.erl" andalso ?MODULE_STRING =:= "beam_predefined_probe").
-export([name/0, arity/2, quoted/0]).
-endif.
-define(NAME, name).
-define(ARGS, _, _).
?NAME() -> ?FUNCTION_NAME.
arity(?ARGS) -> ?FUNCTION_ARITY.
quoted() -> {'\x41\102', "\^C\x{1f600}\7777\d"}.
