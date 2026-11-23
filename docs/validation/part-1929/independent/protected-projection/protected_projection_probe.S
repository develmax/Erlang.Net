{module, protected_projection_probe}.  %% version = 0

{exports, [{lookup,2},{module_info,0},{module_info,1},{retain,2}]}.

{attributes, []}.

{labels, 18}.


{function, lookup, 2, 2}.
  {label,1}.
    {line,[{location,"d:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/protected-case-projection-liveness-1929/protected_projection_probe.erl",
                     4}]}.
    {func_info,{atom,protected_projection_probe},{atom,lookup},2}.
  {label,2}.
    {allocate,3,2}.
    {move,{x,1},{y,0}}.
    {move,{x,0},{y,1}}.
    {'try',{y,2},{f,7}}.
    {test,is_tuple,{f,6},[{x,0}]}.
    {test,test_arity,{f,6},[{x,0},2]}.
    {get_tuple_element,{x,0},0,{x,0}}.
    {get_tuple_element,{y,1},1,{x,1}}.
    {select_val,{x,0},{f,6},{list,[{atom,list},{f,4},{atom,map},{f,3}]}}.
  {label,3}.
    {line,[{location,"d:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/protected-case-projection-liveness-1929/protected_projection_probe.erl",
                     7}]}.
    {bif,map_get,{f,0},[{x,1},{y,0}],{x,0}}.
    {jump,{f,5}}.
  {label,4}.
    {move,{x,1},{x,0}}.
    {move,{y,0},{x,1}}.
    {line,[{scope,[2]},
           {location,"d:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/protected-case-projection-liveness-1929/protected_projection_probe.erl",
                     8}]}.
    {call_ext,2,{extfunc,proplists,get_value,2}}.
  {label,5}.
    {try_end,{y,2}}.
    {deallocate,3}.
    return.
  {label,6}.
    {move,{atom,badarg},{x,0}}.
    {line,[{scope,[2]},
           {location,"d:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/protected-case-projection-liveness-1929/protected_projection_probe.erl",
                     9}]}.
    {call_ext,1,{extfunc,erlang,error,1}}.
  {label,7}.
    {try_case,{y,2}}.
    {test,is_eq_exact,
          {f,8},
          [{tr,{x,0},{t_atom,[error,exit,throw]}},{atom,error}]}.
    {test_heap,4,2}.
    {put_tuple2,{x,0},{list,[{x,1},{y,1},{y,0}]}}.
    {deallocate,3}.
    return.
  {label,8}.
    {bif,raise,{f,0},[{x,2},{x,1}],{x,0}}.


{function, retain, 2, 10}.
  {label,9}.
    {line,[{location,"d:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/protected-case-projection-liveness-1929/protected_projection_probe.erl",
                     15}]}.
    {func_info,{atom,protected_projection_probe},{atom,retain},2}.
  {label,10}.
    {allocate,3,2}.
    {move,{x,1},{y,0}}.
    {move,{x,0},{y,1}}.
    {'try',{y,2},{f,12}}.
    {test,is_tagged_tuple,{f,11},[{x,0},2,{atom,map}]}.
    {get_tuple_element,{x,0},1,{x,0}}.
    {line,[{location,"d:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/protected-case-projection-liveness-1929/protected_projection_probe.erl",
                     18}]}.
    {bif,map_get,{f,0},[{x,0},{x,1}],{x,1}}.
    {try_end,{y,2}}.
    {test_heap,3,2}.
    {put_tuple2,{x,0},{list,[{x,1},{x,0}]}}.
    {deallocate,3}.
    return.
  {label,11}.
    {move,{atom,badarg},{x,0}}.
    {line,[{scope,[2]},
           {location,"d:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/protected-case-projection-liveness-1929/protected_projection_probe.erl",
                     19}]}.
    {call_ext,1,{extfunc,erlang,error,1}}.
  {label,12}.
    {try_case,{y,2}}.
    {test,is_eq_exact,
          {f,13},
          [{tr,{x,0},{t_atom,[error,exit,throw]}},{atom,error}]}.
    {test_heap,4,2}.
    {put_tuple2,{x,0},{list,[{x,1},{y,1},{y,0}]}}.
    {deallocate,3}.
    return.
  {label,13}.
    {bif,raise,{f,0},[{x,2},{x,1}],{x,0}}.


{function, module_info, 0, 15}.
  {label,14}.
    {line,[]}.
    {func_info,{atom,protected_projection_probe},{atom,module_info},0}.
  {label,15}.
    {move,{atom,protected_projection_probe},{x,0}}.
    {call_ext_only,1,{extfunc,erlang,get_module_info,1}}.


{function, module_info, 1, 17}.
  {label,16}.
    {line,[]}.
    {func_info,{atom,protected_projection_probe},{atom,module_info},1}.
  {label,17}.
    {move,{x,0},{x,1}}.
    {move,{atom,protected_projection_probe},{x,0}}.
    {call_ext_only,2,{extfunc,erlang,get_module_info,2}}.
