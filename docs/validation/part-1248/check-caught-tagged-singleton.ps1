param(
    [Parameter(Mandatory)] [string] $OtpRoot,
    [Parameter(Mandatory)] [string] $GeneratedRoot
)

$env:ERL_CRASH_DUMP_SECONDS = '0'
$check = @'
[R,G] = init:get_plain_arguments(),
P = "lib/snmp-5.20.5/ebin/snmpa_error",
Check = fun(D) ->
    {module,snmpa_error} = code:load_abs(filename:join(D,P)),
    ok = snmpa_error:user_err("",[]),
    T = ets:new(snmp_agent_table,[named_table,bag]),
    ok = snmpa_error:user_err("",[]),
    true = ets:insert(T,{error_report_mod,lists,extra}),
    ok = snmpa_error:user_err("",[]),
    true = ets:delete_all_objects(T),
    true = ets:insert(T,{error_report_mod,lists}),
    {'EXIT',{undef,[{lists,user_err,["",[]],_}|_]}} = snmpa_error:user_err("",[]),
    true = ets:insert(T,{error_report_mod,maps}),
    ok = snmpa_error:user_err("",[]),
    true = ets:delete(T),
    true = code:delete(snmpa_error),
    code:purge(snmpa_error),
    ok
end,
ok = Check(R),
ok = Check(G),
io:format("Reference and generated images pass missing table, empty lookup, wrong arity, singleton dispatch and multiple rows.~n"),
halt().
'@

& "$OtpRoot/bin/erl.exe" -noshell -eval $check -extra $OtpRoot $GeneratedRoot
exit $LASTEXITCODE
