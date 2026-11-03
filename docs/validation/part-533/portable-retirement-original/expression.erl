W=fun(N,V)->ok=file:write_file(N,term_to_binary(V,[{minor_version,2}])) end,
try
 Expected=filename:absname("D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed"),Expected=filename:absname(code:root_dir()),8=erlang:system_info(wordsize),
 C=[compile:file(F,[return_errors,return_warnings])||F<-["native_resource_family527.erl","resource_queue593.erl"]],W("compile.etf",C),
 true=lists:all(fun({ok,_,_})->true;({ok,_})->true;(_)->false end,C),
 Mods=[native_resource_family527,resource_queue593,timer],
 [code:ensure_loaded(M)||M<-Mods],
 Paths=[{native_resource_family527,filename:absname("native_resource_family527.beam")},{resource_queue593,filename:absname("resource_queue593.beam")},{timer,filename:absname("D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed/lib/stdlib-8.1/ebin/timer.beam")}],
 Owners=fun()->[begin P=filename:absname(code:which(M)),Running=M:module_info(md5),{ok,{M,Running}}=beam_lib:md5(P),{M,P,Running,M:module_info(compile),M:module_info(attributes),M:module_info(exports)} end||{M,P}<-Paths] end,
 Before=Owners(),W("owners-before.etf",Before),
 ok=native_resource_family527:load("D:/Distr/Work/Codex/Erlang.Net/w518repair/artifacts/nif-resource529/part533-retirement-final-original/build-family1/resource529",0),V=resource_queue593:run(),
 W("observed.etf",V),ok=file:write_file("observed-deterministic.etf",term_to_binary(V,[deterministic,{minor_version,2}])),
 W("owners-after.etf",Owners()),Before=Owners(),
 true=lists:all(fun({_,{{returned593,_,_,_,_},[{sender_cleanup593,ok},{receiver_cleanup593,ok}]}})->true;(_)->false end,V),
 [consume,abandon]=[K||{K,_}<-V],
 Cleanup=[{M,code:delete(M),code:purge(M),code:is_loaded(M)}||M<-[resource_queue593,native_resource_family527]],W("cleanup.etf",Cleanup),
 true=lists:all(fun({_,true,_,false})->true;(_)->false end,Cleanup),halt(0)
catch Class:Reason:Stack->W("failure.etf",{Class,Reason,Stack}),halt(3) end.