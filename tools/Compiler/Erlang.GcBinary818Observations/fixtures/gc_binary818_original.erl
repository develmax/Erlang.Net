-module(gc_binary818_original).
-export([run/0]).
run() ->
    try
        {ok,gc_binary818,_}=compile:file("gc_binary818.erl",[debug_info,return_errors,return_warnings]),
        {module,gc_binary818}=code:load_abs("gc_binary818"),
        Disasm={beam_file,gc_binary818,_,_,_,Functions}=beam_disasm:file("gc_binary818.beam"),
        ok=file:write_file("disassembly.etf",term_to_binary(Disasm,[{minor_version,2}])),
        ok=file:write_file("disassembly.txt",io_lib:format("~tp.~n",[Disasm])),
        Ops=[{Name,Bif,Fail,Live,Args,Dst} || {function,Name,_,_,Instructions} <- Functions,
                    {gc_bif,Bif,Fail,Live,Args,Dst} <- Instructions],
        Binary=[O || O={_,binary_part,_,_,[_,_],_} <- Ops],
        Body=lists:any(fun({body,binary_part,{f,0},_,_,_})->true;(_)->false end,Binary),
        Guard=lists:any(fun({guarded,binary_part,{f,N},_,_,_}) when N>0->true;(_)->false end,Binary),
        Left=lists:any(fun({left_alias,binary_part,_,_,[A,_],D})->A=:=D;(_)->false end,Binary),
        Right=lists:any(fun({right_alias,binary_part,_,_,[_,A],D})->A=:=D;(_)->false end,Binary),
        Live=lists:any(fun({live,binary_part,_,N,_,_}) when N>=3->true;(_)->false end,Binary),
        Arith=lists:all(fun(N)->lists:any(fun({_,B,_,_,_,_})->B=:=N end,Ops) end,['+','*','div','bsl']),
        ok=file:write_file("coverage.json",io_lib:format("{\"BodyFailZero\":~p,\"GuardFailNonzero\":~p,\"DestinationAliasesLeft\":~p,\"DestinationAliasesRight\":~p,\"LiveAtLeastThree\":~p,\"ArithmeticGcBifs\":~p}~n",[Body,Guard,Left,Right,Live,Arith])),
        Rows=gc_binary818:capture(),
        38=length(Rows),
        ok=file:write_file("original.etf",term_to_binary(Rows,[{minor_version,2}])),
        ok=file:write_file("original-deterministic.etf",term_to_binary(Rows,[deterministic,{minor_version,2}])),
        io:format("GC_BINARY818_ROWS=~p~n",[length(Rows)]),
        halt(0)
    catch C:R:S -> file:write_file("failure.etf",term_to_binary({C,R,S},[{minor_version,2}])),io:format("~tp~n",[{C,R,S}]),halt(1) end.
