-module(beam_diameter_records).
-compile({parse_transform,diameter_exprecs}).
-export_records([sample]).
-record(sample,{first=initial,second=2}).
-export([run/0]).
run() -> '#new-'(sample).
