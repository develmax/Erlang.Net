-module(beam_adjacent_numeric).
        %% options
        -export([value/2,power/2]).
        value(Tag, Input) -> power(Tag, Input) + 1.
        power(left, Input) -> 1 bsl Input;
        power(right, Input) -> 1 bsr Input;
        power(error, Input) -> erlang:error({declined, Input}).
