{module, beam_terminal_local_try_tail}.  %% version = 0

{exports, [{cleanup,1},
           {module_info,0},
           {module_info,1},
           {projected,1},
           {remote,1},
           {run,1},
           {wrapped,1}]}.

{attributes, []}.

{labels, 36}.


{function, run, 1, 2}.
  {label,1}.
    {line,[{location,"d:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/protected-entry-and-short-returns-1930/protected-tail-control/beam_terminal_local_try_tail.erl",
                     3}]}.
    {func_info,{atom,beam_terminal_local_try_tail},{atom,run},1}.
  {label,2}.
    {allocate,2,1}.
    {move,{x,0},{y,0}}.
    {'try',{y,1},{f,4}}.
    {line,[{location,"d:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/protected-entry-and-short-returns-1930/protected-tail-control/beam_terminal_local_try_tail.erl",
                     4}]}.
    {call,1,{f,24}}. % probe/1
    {try_end,{y,1}}.
    {test,is_eq_exact,{f,3},[{x,0},{atom,undefined}]}.
    {move,{y,0},{x,0}}.
    {call_last,1,{f,29},2}. % fallback/1
  {label,3}.
    {deallocate,2}.
    return.
  {label,4}.
    {try_case,{y,1}}.
    {move,{y,0},{x,0}}.
    {call_last,1,{f,29},2}. % fallback/1


{function, projected, 1, 6}.
  {label,5}.
    {line,[{location,"d:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/protected-entry-and-short-returns-1930/protected-tail-control/beam_terminal_local_try_tail.erl",
                     9}]}.
    {func_info,{atom,beam_terminal_local_try_tail},{atom,projected},1}.
  {label,6}.
    {allocate,2,1}.
    {move,{x,0},{y,0}}.
    {'try',{y,1},{f,8}}.
    {line,[{location,"d:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/protected-entry-and-short-returns-1930/protected-tail-control/beam_terminal_local_try_tail.erl",
                     10}]}.
    {bif,element,{f,0},[{integer,2},{x,0}],{x,0}}.
    {call,1,{f,24}}. % probe/1
    {try_end,{y,1}}.
    {test,is_eq_exact,{f,7},[{x,0},{atom,undefined}]}.
    {move,{y,0},{x,0}}.
    {call_last,1,{f,29},2}. % fallback/1
  {label,7}.
    {deallocate,2}.
    return.
  {label,8}.
    {try_case,{y,1}}.
    {move,{y,0},{x,0}}.
    {call_last,1,{f,29},2}. % fallback/1


{function, wrapped, 1, 10}.
  {label,9}.
    {line,[{location,"d:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/protected-entry-and-short-returns-1930/protected-tail-control/beam_terminal_local_try_tail.erl",
                     15}]}.
    {func_info,{atom,beam_terminal_local_try_tail},{atom,wrapped},1}.
  {label,10}.
    {allocate,2,1}.
    {move,{x,0},{y,0}}.
    {'try',{y,1},{f,12}}.
    {line,[{location,"d:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/protected-entry-and-short-returns-1930/protected-tail-control/beam_terminal_local_try_tail.erl",
                     16}]}.
    {call,1,{f,24}}. % probe/1
    {try_end,{y,1}}.
    {test,is_eq_exact,{f,11},[{x,0},{atom,undefined}]}.
    {move,{y,0},{x,0}}.
    {trim,2,0}.
    {line,[{scope,[1]},
           {location,"d:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/protected-entry-and-short-returns-1930/protected-tail-control/beam_terminal_local_try_tail.erl",
                     17}]}.
    {call,1,{f,29}}. % fallback/1
    {'%',{var_info,{x,0},
                   [{type,{t_tuple,2,true,#{1 => {t_atom,[fallback]}}}}]}}.
    {test_heap,3,1}.
    {put_tuple2,{x,0},{list,[{atom,wrapped},{x,0}]}}.
    {deallocate,0}.
    return.
  {label,11}.
    {test_heap,3,1}.
    {put_tuple2,{x,0},{list,[{atom,wrapped},{x,0}]}}.
    {deallocate,2}.
    return.
  {label,12}.
    {try_case,{y,1}}.
    {move,{y,0},{x,0}}.
    {trim,2,0}.
    {line,[{scope,[3]},
           {location,"d:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/protected-entry-and-short-returns-1930/protected-tail-control/beam_terminal_local_try_tail.erl",
                     19}]}.
    {call,1,{f,29}}. % fallback/1
    {'%',{var_info,{x,0},
                   [{type,{t_tuple,2,true,#{1 => {t_atom,[fallback]}}}}]}}.
    {test_heap,3,1}.
    {put_tuple2,{x,0},{list,[{atom,wrapped},{x,0}]}}.
    {deallocate,0}.
    return.


{function, cleanup, 1, 14}.
  {label,13}.
    {line,[{location,"d:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/protected-entry-and-short-returns-1930/protected-tail-control/beam_terminal_local_try_tail.erl",
                     21}]}.
    {func_info,{atom,beam_terminal_local_try_tail},{atom,cleanup},1}.
  {label,14}.
    {allocate,4,1}.
    {init_yregs,{list,[{y,0},{y,2}]}}.
    {move,{x,0},{y,1}}.
    {'try',{y,3},{f,18}}.
    {'try',{y,2},{f,16}}.
    {line,[{location,"d:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/protected-entry-and-short-returns-1930/protected-tail-control/beam_terminal_local_try_tail.erl",
                     22}]}.
    {call,1,{f,24}}. % probe/1
    {move,{x,0},{y,0}}.
    {try_end,{y,2}}.
    {test,is_eq_exact,{f,15},[{x,0},{atom,undefined}]}.
    {move,{y,1},{x,0}}.
    {init_yregs,{list,[{y,0},{y,1}]}}.
    {line,[{scope,[3]},
           {location,"d:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/protected-entry-and-short-returns-1930/protected-tail-control/beam_terminal_local_try_tail.erl",
                     23}]}.
    {call,1,{f,29}}. % fallback/1
    {'%',{var_info,{x,0},
                   [{type,{t_tuple,2,true,#{1 => {t_atom,[fallback]}}}}]}}.
    {move,{x,0},{y,2}}.
    {jump,{f,17}}.
  {label,15}.
    {move,{y,0},{y,2}}.
    {jump,{f,17}}.
  {label,16}.
    {try_case,{y,2}}.
    {move,{y,1},{x,0}}.
    {init_yregs,{list,[{y,1}]}}.
    {line,[{scope,[5]},
           {location,"d:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/protected-entry-and-short-returns-1930/protected-tail-control/beam_terminal_local_try_tail.erl",
                     25}]}.
    {call,1,{f,29}}. % fallback/1
    {'%',{var_info,{x,0},
                   [{type,{t_tuple,2,true,#{1 => {t_atom,[fallback]}}}}]}}.
    {move,{x,0},{y,2}}.
  {label,17}.
    {try_end,{y,3}}.
    {move,{atom,true},{x,1}}.
    {move,{atom,cleaned},{x,0}}.
    {line,[{scope,[2,3]},
           {location,"d:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/protected-entry-and-short-returns-1930/protected-tail-control/beam_terminal_local_try_tail.erl",
                     26}]}.
    {call_ext,2,{extfunc,erlang,put,2}}.
    {move,{y,2},{x,0}}.
    {deallocate,4}.
    return.
  {label,18}.
    {try_case,{y,3}}.
    {move,{x,1},{y,2}}.
    {move,{x,2},{y,3}}.
    {move,{atom,true},{x,1}}.
    {move,{atom,cleaned},{x,0}}.
    {line,[{scope,[6]},
           {location,"d:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/protected-entry-and-short-returns-1930/protected-tail-control/beam_terminal_local_try_tail.erl",
                     26}]}.
    {call_ext,2,{extfunc,erlang,put,2}}.
    {bif,raise,{f,0},[{y,3},{y,2}],{x,0}}.


{function, remote, 1, 20}.
  {label,19}.
    {line,[{location,"d:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/protected-entry-and-short-returns-1930/protected-tail-control/beam_terminal_local_try_tail.erl",
                     28}]}.
    {func_info,{atom,beam_terminal_local_try_tail},{atom,remote},1}.
  {label,20}.
    {allocate,2,1}.
    {move,{x,0},{y,0}}.
    {'try',{y,1},{f,22}}.
    {line,[{location,"d:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/protected-entry-and-short-returns-1930/protected-tail-control/beam_terminal_local_try_tail.erl",
                     29}]}.
    {call,1,{f,24}}. % probe/1
    {try_end,{y,1}}.
    {test,is_eq_exact,{f,21},[{x,0},{atom,undefined}]}.
    {move,{y,0},{x,0}}.
    {line,[{scope,[1]},
           {location,"d:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/protected-entry-and-short-returns-1930/protected-tail-control/beam_terminal_local_try_tail.erl",
                     30}]}.
    {call_ext_last,1,{extfunc,lists,reverse,1},2}.
  {label,21}.
    {deallocate,2}.
    return.
  {label,22}.
    {try_case,{y,1}}.
    {move,{y,0},{x,0}}.
    {line,[{scope,[3]},
           {location,"d:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/protected-entry-and-short-returns-1930/protected-tail-control/beam_terminal_local_try_tail.erl",
                     32}]}.
    {call_ext_last,1,{extfunc,lists,reverse,1},2}.


{function, probe, 1, 24}.
  {label,23}.
    {line,[{location,"d:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/protected-entry-and-short-returns-1930/protected-tail-control/beam_terminal_local_try_tail.erl",
                     34}]}.
    {func_info,{atom,beam_terminal_local_try_tail},{atom,probe},1}.
  {label,24}.
    {select_val,{x,0},
                {f,27},
                {list,[{atom,catch_fallback_error},
                       {f,26},
                       {atom,fallback_error},
                       {f,25},
                       {atom,throwme},
                       {f,26}]}}.
  {label,25}.
    {move,{atom,undefined},{x,0}}.
    return.
  {label,26}.
    {move,{atom,bad_probe},{x,0}}.
    {line,[{location,"d:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/protected-entry-and-short-returns-1930/protected-tail-control/beam_terminal_local_try_tail.erl",
                     36}]}.
    {call_ext_only,1,{extfunc,erlang,error,1}}.
  {label,27}.
    return.


{function, fallback, 1, 29}.
  {label,28}.
    {line,[{location,"d:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/protected-entry-and-short-returns-1930/protected-tail-control/beam_terminal_local_try_tail.erl",
                     38}]}.
    {func_info,{atom,beam_terminal_local_try_tail},{atom,fallback},1}.
  {label,29}.
    {select_val,{x,0},
                {f,31},
                {list,[{atom,catch_fallback_error},
                       {f,30},
                       {atom,fallback_error},
                       {f,30}]}}.
  {label,30}.
    {move,{atom,unhandled_fallback},{x,0}}.
    {line,[{location,"d:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/protected-entry-and-short-returns-1930/protected-tail-control/beam_terminal_local_try_tail.erl",
                     39}]}.
    {call_ext_only,1,{extfunc,erlang,error,1}}.
  {label,31}.
    {test_heap,3,1}.
    {put_tuple2,{x,0},{list,[{atom,fallback},{x,0}]}}.
    return.


{function, module_info, 0, 33}.
  {label,32}.
    {line,[]}.
    {func_info,{atom,beam_terminal_local_try_tail},{atom,module_info},0}.
  {label,33}.
    {move,{atom,beam_terminal_local_try_tail},{x,0}}.
    {call_ext_only,1,{extfunc,erlang,get_module_info,1}}.


{function, module_info, 1, 35}.
  {label,34}.
    {line,[]}.
    {func_info,{atom,beam_terminal_local_try_tail},{atom,module_info},1}.
  {label,35}.
    {move,{x,0},{x,1}}.
    {move,{atom,beam_terminal_local_try_tail},{x,0}}.
    {call_ext_only,2,{extfunc,erlang,get_module_info,2}}.
