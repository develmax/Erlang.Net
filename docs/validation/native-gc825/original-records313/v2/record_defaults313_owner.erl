-module(record_defaults313_owner).
-export([make/0,capture/0,payload/1,make_explicit/1]).
-record #r{payload={new,[three,four],<<"new">>}, optional}.
-export_record([r]).
make() -> #r{optional=present}.
capture() -> fun() -> #r{optional=present} end.
payload(R) -> R#r.payload.
make_explicit(Value) -> #r{payload=Value,optional=present}.
