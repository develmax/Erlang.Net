-module(bif_result_chain).
-export([time/1, observed/1]).
time(T) ->
    Utc = calendar:now_to_universal_time(T),
    Seconds = calendar:datetime_to_gregorian_seconds(Utc) - 62105714048,
    Seconds band 4294967295.
observed(T) ->
    Utc = calendar:now_to_universal_time(T),
    Seconds = calendar:datetime_to_gregorian_seconds(Utc) - 62105714048,
    {Seconds band 4294967295, Seconds}.