-module(raw_unix_filename_contract).
-export([run/1,echo/1]).
run(Path) ->
    ok = erlang:load_nif(Path, []),
    Value = {raw_filename_echo, <<255,0,254>>, [1,2,3]},
    Value = raw_unix_filename_contract:echo(Value),
    {ok,Value}.
echo(_) -> erlang:nif_error(not_loaded).
