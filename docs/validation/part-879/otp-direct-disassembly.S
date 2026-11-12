{module, beam_record_access_guard_probe}.  %% version = 0

{exports, [{boolean,1},{exact,1},{module_info,0},{module_info,1}]}.

{attributes, []}.

{labels, 11}.


{function, boolean, 1, 2}.
  {label,1}.
    {line,[{location,"beam_record_access_guard_probe.erl",4}]}.
    {func_info,{atom,beam_record_access_guard_probe},{atom,boolean},1}.
  {label,2}.
    {test,is_tagged_tuple,{f,3},[{x,0},2,{atom,row}]}.
    {get_tuple_element,{x,0},1,{x,0}}.
    {test,is_eq_exact,{f,3},[{x,0},{atom,true}]}.
    {move,{atom,accepted},{x,0}}.
    return.
  {label,3}.
    {move,{atom,rejected},{x,0}}.
    return.


{function, exact, 1, 5}.
  {label,4}.
    {line,[{location,"beam_record_access_guard_probe.erl",6}]}.
    {func_info,{atom,beam_record_access_guard_probe},{atom,exact},1}.
  {label,5}.
    {test,is_tagged_tuple,{f,6},[{x,0},2,{atom,row}]}.
    {get_tuple_element,{x,0},1,{x,0}}.
    {test,is_eq_exact,{f,6},[{x,0},{atom,ok}]}.
    {move,{atom,accepted},{x,0}}.
    return.
  {label,6}.
    {move,{atom,rejected},{x,0}}.
    return.


{function, module_info, 0, 8}.
  {label,7}.
    {line,[]}.
    {func_info,{atom,beam_record_access_guard_probe},{atom,module_info},0}.
  {label,8}.
    {move,{atom,beam_record_access_guard_probe},{x,0}}.
    {call_ext_only,1,{extfunc,erlang,get_module_info,1}}.


{function, module_info, 1, 10}.
  {label,9}.
    {line,[]}.
    {func_info,{atom,beam_record_access_guard_probe},{atom,module_info},1}.
  {label,10}.
    {move,{x,0},{x,1}}.
    {move,{atom,beam_record_access_guard_probe},{x,0}}.
    {call_ext_only,2,{extfunc,erlang,get_module_info,2}}.
