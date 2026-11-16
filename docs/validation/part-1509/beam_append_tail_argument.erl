-module(beam_append_tail_argument).
-export([prefixed/2, suffixed/2, nested/2, literal_join/2, shared/1, finish/2, extension/0]).

prefixed(Format, Arguments) ->
    ?MODULE:finish("prefix: " ++ Format, Arguments).

suffixed(Format, Arguments) ->
    ?MODULE:finish(Arguments, Format ++ " suffix").

nested(Directory, Name) ->
    ?MODULE:finish(Directory, atom_to_list(Name) ++ ?MODULE:extension()).

literal_join(Directory, Value) ->
    ?MODULE:finish(Directory, ?MODULE:finish("literal" ++ " suffix", Value)).

shared(Value) ->
    ?MODULE:finish(Value ++ Value, Value).

finish(fail, _) ->
    erlang:error(finish_failed);
finish(First, Second) ->
    {First, Second}.

extension() ->
    ".beam".
