{module, beam_guard_rounding}.  %% version = 0

{exports, [{module_info,0},{module_info,1},{run,2}]}.

{attributes, []}.

{labels, 12}.


{function, run, 2, 2}.
  {label,1}.
    {line,[{location,"beam_guard_rounding.erl",3}]}.
    {func_info,{atom,beam_guard_rounding},{atom,run},2}.
  {label,2}.
    {select_val,{x,0},
                {f,7},
                {list,[{atom,ceil},
                       {f,5},
                       {atom,floor},
                       {f,4},
                       {atom,round},
                       {f,3}]}}.
  {label,3}.
    {gc_bif,round,{f,7},2,[{x,1}],{x,0}}.
    {test,is_eq_exact,{f,7},[{tr,{x,0},{t_integer,any}},{integer,-2}]}.
    {jump,{f,6}}.
  {label,4}.
    {gc_bif,floor,{f,7},2,[{x,1}],{x,0}}.
    {test,is_eq_exact,{f,7},[{tr,{x,0},{t_integer,any}},{integer,-2}]}.
    {jump,{f,6}}.
  {label,5}.
    {gc_bif,ceil,{f,7},2,[{x,1}],{x,0}}.
    {test,is_eq_exact,{f,7},[{tr,{x,0},{t_integer,any}},{integer,-1}]}.
  {label,6}.
    {move,{atom,yes},{x,0}}.
    return.
  {label,7}.
    {move,{atom,no},{x,0}}.
    return.


{function, module_info, 0, 9}.
  {label,8}.
    {line,[]}.
    {func_info,{atom,beam_guard_rounding},{atom,module_info},0}.
  {label,9}.
    {move,{atom,beam_guard_rounding},{x,0}}.
    {call_ext_only,1,{extfunc,erlang,get_module_info,1}}.


{function, module_info, 1, 11}.
  {label,10}.
    {line,[]}.
    {func_info,{atom,beam_guard_rounding},{atom,module_info},1}.
  {label,11}.
    {move,{x,0},{x,1}}.
    {move,{atom,beam_guard_rounding},{x,0}}.
    {call_ext_only,2,{extfunc,erlang,get_module_info,2}}.
