-module(beam_wide_dispatch).
-compile([report_warnings,report_errors,report]).
-export([run/1]).
run(16#80000000) -> root;
run(16#80000001) -> user;
run(-16#80000001) -> negative.
