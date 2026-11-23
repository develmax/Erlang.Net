-module(beam_nested_guarded_tail).
-export([project/1, complete/8]).

project(Options) ->
    case proplists:get_value(payload, Options) of
        {ok, {fields, First, Masked, Second, Third, Fourth}}
          when (Masked band -65536) =:= 0 ->
            ?MODULE:complete(Second, First, Masked, Fourth,
                             first_constant, second_constant, Third, third_constant);
        {ok, _} ->
            {error, invalid};
        Other ->
            Other
    end.

complete(A, B, C, D, E, F, G, H) ->
    {A, B, C, D, E, F, G, H}.
