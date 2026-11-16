{module, beam_number_map_head_guard_probe}.  %% version = 0

{exports, [{exact,1},{head,1},{map,1},{module_info,0},{module_info,1},{num,1}]}.

{attributes, []}.

{labels, 17}.


{function, num, 1, 2}.
  {label,1}.
    {line,[{location,"beam_number_map_head_guard_probe.erl",3}]}.
    {func_info,{atom,beam_number_map_head_guard_probe},{atom,num},1}.
  {label,2}.
    {test,is_number,{f,3},[{x,0}]}.
    {move,{atom,yes},{x,0}}.
    return.
  {label,3}.
    {move,{atom,no},{x,0}}.
    return.


{function, map, 1, 5}.
  {label,4}.
    {line,[{location,"beam_number_map_head_guard_probe.erl",5}]}.
    {func_info,{atom,beam_number_map_head_guard_probe},{atom,map},1}.
  {label,5}.
    {test,is_map,{f,6},[{x,0}]}.
    {move,{atom,yes},{x,0}}.
    return.
  {label,6}.
    {move,{atom,no},{x,0}}.
    return.


{function, head, 1, 8}.
  {label,7}.
    {line,[{location,"beam_number_map_head_guard_probe.erl",7}]}.
    {func_info,{atom,beam_number_map_head_guard_probe},{atom,head},1}.
  {label,8}.
    {bif,hd,{f,9},[{x,0}],{x,0}}.
    {test,is_eq_exact,{f,9},[{x,0},{atom,true}]}.
    {move,{atom,yes},{x,0}}.
    return.
  {label,9}.
    {move,{atom,no},{x,0}}.
    return.


{function, exact, 1, 11}.
  {label,10}.
    {line,[{location,"beam_number_map_head_guard_probe.erl",9}]}.
    {func_info,{atom,beam_number_map_head_guard_probe},{atom,exact},1}.
  {label,11}.
    {bif,hd,{f,12},[{x,0}],{x,0}}.
    {test,is_eq_exact,{f,12},[{x,0},{atom,ok}]}.
    {move,{atom,yes},{x,0}}.
    return.
  {label,12}.
    {move,{atom,no},{x,0}}.
    return.


{function, module_info, 0, 14}.
  {label,13}.
    {line,[]}.
    {func_info,{atom,beam_number_map_head_guard_probe},{atom,module_info},0}.
  {label,14}.
    {move,{atom,beam_number_map_head_guard_probe},{x,0}}.
    {call_ext_only,1,{extfunc,erlang,get_module_info,1}}.


{function, module_info, 1, 16}.
  {label,15}.
    {line,[]}.
    {func_info,{atom,beam_number_map_head_guard_probe},{atom,module_info},1}.
  {label,16}.
    {move,{x,0},{x,1}}.
    {move,{atom,beam_number_map_head_guard_probe},{x,0}}.
    {call_ext_only,2,{extfunc,erlang,get_module_info,2}}.
