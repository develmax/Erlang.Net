-module(beam_record_wildcard_probe).
-compile([report_warnings,warn_unused_vars]).
-export([run/1]).
-record(symbol,{key,value}).
run(Key) -> #symbol{key=Key,_='_'}.
