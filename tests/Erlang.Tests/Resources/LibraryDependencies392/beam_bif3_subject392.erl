-module(beam_bif3_subject392).
-export([worker/2,probe/1]).

worker(Parent,Case) ->
    Outcome=try probe(Case) of Value->{returned,Value}
            catch Class:Reason:Stack->{caught,Class,Reason,Stack} end,
    Parent ! {Case,Outcome},
    ok.

probe(low) -> range(2,2,36);
probe(high) -> range(36,2,36);
probe(outside) -> range(37,2,36);
probe(reversed) -> range(7,36,2);
probe(noninteger) -> range(7.0,2,36);
probe(noninteger_bad_min) -> range(not_integer,not_bound,36);
probe(bad_min) -> range(7,not_bound,36);
probe(bad_max) -> range(7,2,not_bound);
probe(large_integer) -> range(1 bsl 4096,1 bsl 4095,1 bsl 4097);
probe(guard_bad_bounds) -> guard_range(7,not_bound,36);
probe(fread_upper) -> io_lib_fread:fread("~16u","AF");
probe(fread_lower) -> io_lib_fread:fread("~16u","af");
probe(fread_invalid_base) -> io_lib_fread:fread("~1u","0").

range(Value,Min,Max) -> erlang:is_integer(Value,Min,Max).
guard_range(Value,Min,Max) when erlang:is_integer(Value,Min,Max) -> inside;
guard_range(_,_,_) -> outside.
