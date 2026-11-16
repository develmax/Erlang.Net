{module, beam_body_fallback_probe}.  %% version = 0

{exports, [{absval,1},
           {bytes,1},
           {head,1},
           {module_info,0},
           {module_info,1},
           {number,1},
           {owner,1},
           {size,1}]}.

{attributes, []}.

{labels, 17}.


{function, size, 1, 2}.
  {label,1}.
    {line,[{location,"beam_body_fallback_probe.erl",3}]}.
    {func_info,{atom,beam_body_fallback_probe},{atom,size},1}.
  {label,2}.
    {bif,tuple_size,{f,0},[{x,0}],{x,0}}.
    return.


{function, head, 1, 4}.
  {label,3}.
    {line,[{location,"beam_body_fallback_probe.erl",4}]}.
    {func_info,{atom,beam_body_fallback_probe},{atom,head},1}.
  {label,4}.
    {bif,hd,{f,0},[{x,0}],{x,0}}.
    return.


{function, bytes, 1, 6}.
  {label,5}.
    {line,[{location,"beam_body_fallback_probe.erl",5}]}.
    {func_info,{atom,beam_body_fallback_probe},{atom,bytes},1}.
  {label,6}.
    {gc_bif,byte_size,{f,0},1,[{x,0}],{x,0}}.
    return.


{function, owner, 1, 8}.
  {label,7}.
    {line,[{location,"beam_body_fallback_probe.erl",6}]}.
    {func_info,{atom,beam_body_fallback_probe},{atom,owner},1}.
  {label,8}.
    {bif,node,{f,0},[{x,0}],{x,0}}.
    return.


{function, number, 1, 10}.
  {label,9}.
    {line,[{location,"beam_body_fallback_probe.erl",7}]}.
    {func_info,{atom,beam_body_fallback_probe},{atom,number},1}.
  {label,10}.
    {bif,is_number,{f,0},[{x,0}],{x,0}}.
    return.


{function, absval, 1, 12}.
  {label,11}.
    {line,[{location,"beam_body_fallback_probe.erl",8}]}.
    {func_info,{atom,beam_body_fallback_probe},{atom,absval},1}.
  {label,12}.
    {gc_bif,abs,{f,0},1,[{x,0}],{x,0}}.
    return.


{function, module_info, 0, 14}.
  {label,13}.
    {line,[]}.
    {func_info,{atom,beam_body_fallback_probe},{atom,module_info},0}.
  {label,14}.
    {move,{atom,beam_body_fallback_probe},{x,0}}.
    {call_ext_only,1,{extfunc,erlang,get_module_info,1}}.


{function, module_info, 1, 16}.
  {label,15}.
    {line,[]}.
    {func_info,{atom,beam_body_fallback_probe},{atom,module_info},1}.
  {label,16}.
    {move,{x,0},{x,1}}.
    {move,{atom,beam_body_fallback_probe},{x,0}}.
    {call_ext_only,2,{extfunc,erlang,get_module_info,2}}.
