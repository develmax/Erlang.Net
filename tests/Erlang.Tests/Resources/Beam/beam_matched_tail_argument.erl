-module(beam_matched_tail_argument).
-export([run/1, selector/1]).
run(Value) ->
    case ?MODULE:selector(Value) of
        first -> finish(first, 1);
        second -> finish(second, 2);
        third -> finish(different, 3)
    end.
selector(Value) -> Value.
finish(Tag, Number) -> {Tag, Number}.
