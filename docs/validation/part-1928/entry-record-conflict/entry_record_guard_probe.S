{module, entry_record_guard_probe}.  %% version = 0

{exports, [{calls_both,1},{module_info,0},{module_info,1},{run,1}]}.

{attributes, []}.

{labels, 18}.


{function, run, 1, 2}.
  {label,1}.
    {line,[{location,"d:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/nested-list-generators-and-heap-roots-1928/entry-record-conflict/entry_record_guard_probe.erl",
                     5}]}.
    {func_info,{atom,entry_record_guard_probe},{atom,run},1}.
  {label,2}.
    {test,is_tagged_tuple,{f,5},[{x,0},3,{atom,row}]}.
    {get_tuple_element,{x,0},1,{x,1}}.
    {test,is_eq_exact,{f,4},[{x,1},{atom,selected}]}.
    {allocate,1,1}.
    {move,{x,0},{y,0}}.
    {line,[{location,"d:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/nested-list-generators-and-heap-roots-1928/entry-record-conflict/entry_record_guard_probe.erl",
                     8}]}.
    {call,1,{f,11}}. % selected/1
    {'%',{var_info,{x,0},[{type,{t_atom,[false,true]}}]}}.
    {test,is_eq_exact,{f,3},[{x,0},{atom,true}]}.
    {test_heap,3,0}.
    {get_tuple_element,{y,0},2,{x,0}}.
    {put_tuple2,{x,0},{list,[{atom,proceed},{x,0}]}}.
    {deallocate,1}.
    return.
  {label,3}.
    {move,{y,0},{x,0}}.
    {call_last,1,{f,13},1}. % finish/1
  {label,4}.
    {test_heap,3,1}.
    {get_tuple_element,{x,0},2,{x,0}}.
    {put_tuple2,{x,0},{list,[{atom,proceed},{x,0}]}}.
    return.
  {label,5}.
    {line,[{location,"d:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/nested-list-generators-and-heap-roots-1928/entry-record-conflict/entry_record_guard_probe.erl",
                     6}]}.
    {badrecord,{x,0}}.


{function, calls_both, 1, 7}.
  {label,6}.
    {line,[{location,"d:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/nested-list-generators-and-heap-roots-1928/entry-record-conflict/entry_record_guard_probe.erl",
                     15}]}.
    {func_info,{atom,entry_record_guard_probe},{atom,calls_both},1}.
  {label,7}.
    {test,is_tagged_tuple,{f,9},[{x,0},3,{atom,row}]}.
    {get_tuple_element,{x,0},1,{x,1}}.
    {test,is_eq_exact,{f,8},[{x,1},{atom,selected}]}.
    {allocate,1,1}.
    {move,{x,0},{y,0}}.
    {line,[{location,"d:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/nested-list-generators-and-heap-roots-1928/entry-record-conflict/entry_record_guard_probe.erl",
                     17}]}.
    {call,1,{f,11}}. % selected/1
    {'%',{var_info,{x,0},[{type,{t_atom,[false,true]}}]}}.
    {test_heap,4,1}.
    {get_tuple_element,{y,0},2,{x,1}}.
    {put_tuple2,{x,0},{list,[{atom,selected},{x,0},{x,1}]}}.
    {deallocate,1}.
    return.
  {label,8}.
    {allocate,1,1}.
    {move,{x,0},{y,0}}.
    {line,[{location,"d:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/nested-list-generators-and-heap-roots-1928/entry-record-conflict/entry_record_guard_probe.erl",
                     18}]}.
    {call,1,{f,11}}. % selected/1
    {'%',{var_info,{x,0},[{type,{t_atom,[false,true]}}]}}.
    {test_heap,4,1}.
    {get_tuple_element,{y,0},2,{x,1}}.
    {put_tuple2,{x,0},{list,[{atom,rejected},{x,0},{x,1}]}}.
    {deallocate,1}.
    return.
  {label,9}.
    {line,[{location,"d:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/nested-list-generators-and-heap-roots-1928/entry-record-conflict/entry_record_guard_probe.erl",
                     16}]}.
    {badrecord,{x,0}}.


{function, selected, 1, 11}.
  {label,10}.
    {line,[{location,"d:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/nested-list-generators-and-heap-roots-1928/entry-record-conflict/entry_record_guard_probe.erl",
                     21}]}.
    {func_info,{atom,entry_record_guard_probe},{atom,selected},1}.
  {label,11}.
    {'%',{var_info,{x,0},[{type,{t_tuple,3,true,#{1 => {t_atom,[row]}}}}]}}.
    {allocate,0,1}.
    {get_tuple_element,{x,0},2,{x,1}}.
    {move,{atom,false},{x,2}}.
    {move,{atom,flag},{x,0}}.
    {line,[{location,"d:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/nested-list-generators-and-heap-roots-1928/entry-record-conflict/entry_record_guard_probe.erl",
                     22}]}.
    {call_ext,3,{extfunc,proplists,get_value,3}}.
    {bif,'=:=',{f,0},[{x,0},{atom,true}],{x,0}}.
    {deallocate,0}.
    return.


{function, finish, 1, 13}.
  {label,12}.
    {line,[{location,"d:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/nested-list-generators-and-heap-roots-1928/entry-record-conflict/entry_record_guard_probe.erl",
                     24}]}.
    {func_info,{atom,entry_record_guard_probe},{atom,finish},1}.
  {label,13}.
    {'%',{var_info,{x,0},
                   [{type,{t_tuple,3,true,
                                   #{1 => {t_atom,[row]},
                                     2 => {t_atom,[selected]}}}}]}}.
    {test_heap,3,1}.
    {get_tuple_element,{x,0},2,{x,0}}.
    {put_tuple2,{x,0},{list,[{atom,finished},{x,0}]}}.
    return.


{function, module_info, 0, 15}.
  {label,14}.
    {line,[]}.
    {func_info,{atom,entry_record_guard_probe},{atom,module_info},0}.
  {label,15}.
    {move,{atom,entry_record_guard_probe},{x,0}}.
    {call_ext_only,1,{extfunc,erlang,get_module_info,1}}.


{function, module_info, 1, 17}.
  {label,16}.
    {line,[]}.
    {func_info,{atom,entry_record_guard_probe},{atom,module_info},1}.
  {label,17}.
    {move,{x,0},{x,1}}.
    {move,{atom,entry_record_guard_probe},{x,0}}.
    {call_ext_only,2,{extfunc,erlang,get_module_info,2}}.
