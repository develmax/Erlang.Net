-module(record_defaults313_consumer).
-export([make/0,payload/1]).
-import_record(record_defaults313_owner,[r]).
make() -> #r{optional=present}.
payload(R) -> R#r.payload.
