-module(beam_record_wildcard_literal).
-export([literal/0, dynamic/1, nested/0]).
-record(row, {a = first, b = second, c = third}).

literal() -> #row{_ = fill, b = fixed}.

dynamic(Value) -> #row{_ = Value, b = fixed}.

nested() -> [#row{a = 5, _ = {fill, 7}}, #row{}].
