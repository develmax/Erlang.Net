{module, beam_callback_probe}.  %% version = 0

{exports, [{behaviour_info,1},{module_info,0},{module_info,1},{size,1}]}.

{attributes, []}.

{labels, 14}.


{function, size, 1, 2}.
  {label,1}.
    {line,[{location,"beam_callback_probe.erl",7}]}.
    {func_info,{atom,beam_callback_probe},{atom,size},1}.
  {label,2}.
    return.


{function, behaviour_info, 1, 4}.
  {label,3}.
    {line,[]}.
    {func_info,{atom,beam_callback_probe},{atom,behaviour_info},1}.
  {label,4}.
    {select_val,{x,0},
                {f,7},
                {list,[{atom,callbacks},
                       {f,6},
                       {atom,optional_callbacks},
                       {f,5}]}}.
  {label,5}.
    {move,{literal,[{z,1}]},{x,0}}.
    return.
  {label,6}.
    {move,{literal,[{z,1},{a,0}]},{x,0}}.
    return.
  {label,7}.
    {call_only,1,{f,13}}. % '-inlined-behaviour_info/1-'/1


{function, module_info, 0, 9}.
  {label,8}.
    {line,[]}.
    {func_info,{atom,beam_callback_probe},{atom,module_info},0}.
  {label,9}.
    {move,{atom,beam_callback_probe},{x,0}}.
    {call_ext_only,1,{extfunc,erlang,get_module_info,1}}.


{function, module_info, 1, 11}.
  {label,10}.
    {line,[]}.
    {func_info,{atom,beam_callback_probe},{atom,module_info},1}.
  {label,11}.
    {move,{x,0},{x,1}}.
    {move,{atom,beam_callback_probe},{x,0}}.
    {call_ext_only,2,{extfunc,erlang,get_module_info,2}}.


{function, '-inlined-behaviour_info/1-', 1, 13}.
  {label,12}.
    {line,[]}.
    {func_info,{atom,beam_callback_probe},
               {atom,'-inlined-behaviour_info/1-'},
               1}.
  {label,13}.
    {jump,{f,12}}.
