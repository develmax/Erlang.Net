{module, beam_try_after_one_line}.  %% version = 0

{exports, [{module_info,0},{module_info,1},{run,0}]}.

{attributes, []}.

{labels, 8}.


{function, run, 0, 2}.
  {label,1}.
    {line,[{location,"beam_try_after_one_line.erl",3}]}.
    {func_info,{atom,beam_try_after_one_line},{atom,run},0}.
  {label,2}.
    {allocate,2,0}.
    {init_yregs,{list,[{y,0}]}}.
    {'try',{y,1},{f,3}}.
    {call_ext,0,{extfunc,probe,body,0}}.
    {move,{x,0},{y,0}}.
    {try_end,{y,1}}.
    {call_ext,0,{extfunc,probe,cleanup,0}}.
    {move,{y,0},{x,0}}.
    {deallocate,2}.
    return.
  {label,3}.
    {try_case,{y,1}}.
    {move,{x,1},{y,0}}.
    {move,{x,2},{y,1}}.
    {line,[{scope,[3]},{location,"beam_try_after_one_line.erl",3}]}.
    {call_ext,0,{extfunc,probe,cleanup,0}}.
    {bif,raise,{f,0},[{y,1},{y,0}],{x,0}}.


{function, module_info, 0, 5}.
  {label,4}.
    {line,[]}.
    {func_info,{atom,beam_try_after_one_line},{atom,module_info},0}.
  {label,5}.
    {move,{atom,beam_try_after_one_line},{x,0}}.
    {call_ext_only,1,{extfunc,erlang,get_module_info,1}}.


{function, module_info, 1, 7}.
  {label,6}.
    {line,[]}.
    {func_info,{atom,beam_try_after_one_line},{atom,module_info},1}.
  {label,7}.
    {move,{x,0},{x,1}}.
    {move,{atom,beam_try_after_one_line},{x,0}}.
    {call_ext_only,2,{extfunc,erlang,get_module_info,2}}.
