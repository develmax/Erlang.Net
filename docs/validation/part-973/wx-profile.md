# wx profile cohort

Read the compile_info chunk of every installed wx-2.7/ebin image using OTP 29.1.1 beam_lib. Group by compiler version, option list and whether source equals /daily_build/otp_src/lib/wx/src/gen/<module>.erl.

| Files | Compiler | Source |
| --- | --- | --- |
| 235 | 10.0.4 | Generated under /daily_build/otp_src/lib/wx/src/gen |
| 5 | 10.0.4 | Directly under /daily_build/otp_src/lib/wx/src |
| 1 | 10.0.6 | gl under the Windows Jenkins path |

The first two groups record the same option list already used by WxGenerated. The gl module records the equivalent options with Windows Jenkins include paths.

The Part 972 corpus counted 96 Equal and 140 Different for the 236 generated sources. The 140 mismatches all differ in Code. The compile information findings establish a shared profile candidate, with separate instruction comparison still required. The wx documentation examples account for another 33 differences and are outside this profile cohort.

```erlang
D = hd(init:get_plain_arguments()),
Paths = filelib:wildcard(filename:join(D, "*.beam")),
Groups = lists:foldl(fun(P, A) ->
    {ok,{M,[{compile_info,I}]}} = beam_lib:chunks(P,[compile_info]),
    Expected = "/daily_build/otp_src/lib/wx/src/gen/" ++ atom_to_list(M) ++ ".erl",
    Key = {proplists:get_value(source,I) =:= Expected,
           proplists:get_value(version,I), proplists:get_value(options,I)},
    maps:update_with(Key, fun(N) -> N+1 end, 1, A)
end, #{}, Paths),
io:format("~p~n", [Groups]), halt().
```

Run the expression with the installed erl -noshell -eval and pass the installed wx-2.7/ebin path through -extra.
