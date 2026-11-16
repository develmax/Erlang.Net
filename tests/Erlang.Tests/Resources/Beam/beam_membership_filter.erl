-module(beam_membership_filter).
-export([only_modes/1, only_keys/1]).
only_modes(Values) -> [Value || Value <- Values, lists:member(Value, [write,append,exclusive])].
only_keys(Values) -> [Value || Value <- Values, lists:keymember(Value, 1, [{a,1},{b,2}])].
