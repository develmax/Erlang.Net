{module, beam_map_pattern_probe}.  %% version = 0

{exports, [{module_info,0},{module_info,1},{read,1}]}.

{attributes, []}.

{labels, 8}.


{function, read, 1, 2}.
  {label,1}.
    {line,[{location,"beam_map_pattern_probe.erl",3}]}.
    {func_info,{atom,beam_map_pattern_probe},{atom,read},1}.
  {label,2}.
    {test,is_map,{f,3},[{x,0}]}.
    {get_map_elements,{f,3},
                      {tr,{x,0},{t_map,any,any}},
                      {list,[{atom,key},{x,0}]}}.
    return.
  {label,3}.
    {move,{atom,missing},{x,0}}.
    return.


{function, module_info, 0, 5}.
  {label,4}.
    {line,[]}.
    {func_info,{atom,beam_map_pattern_probe},{atom,module_info},0}.
  {label,5}.
    {move,{atom,beam_map_pattern_probe},{x,0}}.
    {call_ext_only,1,{extfunc,erlang,get_module_info,1}}.


{function, module_info, 1, 7}.
  {label,6}.
    {line,[]}.
    {func_info,{atom,beam_map_pattern_probe},{atom,module_info},1}.
  {label,7}.
    {move,{x,0},{x,1}}.
    {move,{atom,beam_map_pattern_probe},{x,0}}.
    {call_ext_only,2,{extfunc,erlang,get_module_info,2}}.
