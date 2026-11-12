{module, beam_apply_value_probe}.  %% version = 0

{exports, [{const,1},
           {identity,2},
           {module_info,0},
           {module_info,1},
           {repeated,2},
           {swap,3},
           {zero,1}]}.

{attributes, []}.

{labels, 15}.


{function, zero, 1, 2}.
  {label,1}.
    {line,[{location,"beam_apply_value_probe.erl",3}]}.
    {func_info,{atom,beam_apply_value_probe},{atom,zero},1}.
  {label,2}.
    {allocate,0,1}.
    {call_fun,0}.
    {deallocate,0}.
    return.


{function, swap, 3, 4}.
  {label,3}.
    {line,[{location,"beam_apply_value_probe.erl",4}]}.
    {func_info,{atom,beam_apply_value_probe},{atom,swap},3}.
  {label,4}.
    {allocate,0,3}.
    {swap,{x,0},{x,2}}.
    {call_fun,2}.
    {deallocate,0}.
    return.


{function, identity, 2, 6}.
  {label,5}.
    {line,[{location,"beam_apply_value_probe.erl",5}]}.
    {func_info,{atom,beam_apply_value_probe},{atom,identity},2}.
  {label,6}.
    {allocate,0,2}.
    {call_fun,1}.
    {deallocate,0}.
    return.


{function, const, 1, 8}.
  {label,7}.
    {line,[{location,"beam_apply_value_probe.erl",6}]}.
    {func_info,{atom,beam_apply_value_probe},{atom,const},1}.
  {label,8}.
    {allocate,0,1}.
    {move,{x,0},{x,1}}.
    {move,{atom,ok},{x,0}}.
    {call_fun,1}.
    {deallocate,0}.
    return.


{function, repeated, 2, 10}.
  {label,9}.
    {line,[{location,"beam_apply_value_probe.erl",7}]}.
    {func_info,{atom,beam_apply_value_probe},{atom,repeated},2}.
  {label,10}.
    {allocate,0,2}.
    {move,{x,0},{x,2}}.
    {move,{x,1},{x,0}}.
    {call_fun,2}.
    {deallocate,0}.
    return.


{function, module_info, 0, 12}.
  {label,11}.
    {line,[]}.
    {func_info,{atom,beam_apply_value_probe},{atom,module_info},0}.
  {label,12}.
    {move,{atom,beam_apply_value_probe},{x,0}}.
    {call_ext_only,1,{extfunc,erlang,get_module_info,1}}.


{function, module_info, 1, 14}.
  {label,13}.
    {line,[]}.
    {func_info,{atom,beam_apply_value_probe},{atom,module_info},1}.
  {label,14}.
    {move,{x,0},{x,1}}.
    {move,{atom,beam_apply_value_probe},{x,0}}.
    {call_ext_only,2,{extfunc,erlang,get_module_info,2}}.
