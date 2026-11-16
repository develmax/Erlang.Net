{module, beam_guard_pid_size_probe}.  %% version = 0

{exports, [{module_info,0},{module_info,1},{pid,1},{size,1}]}.

{attributes, []}.

{labels, 11}.


{function, pid, 1, 2}.
  {label,1}.
    {line,[{location,"beam_guard_pid_size_probe.erl",3}]}.
    {func_info,{atom,beam_guard_pid_size_probe},{atom,pid},1}.
  {label,2}.
    {test,is_pid,{f,3},[{x,0}]}.
    {move,{atom,accepted},{x,0}}.
    return.
  {label,3}.
    {move,{atom,rejected},{x,0}}.
    return.


{function, size, 1, 5}.
  {label,4}.
    {line,[{location,"beam_guard_pid_size_probe.erl",5}]}.
    {func_info,{atom,beam_guard_pid_size_probe},{atom,size},1}.
  {label,5}.
    {test,is_tuple,{f,6},[{x,0}]}.
    {test,test_arity,{f,6},[{x,0},2]}.
    {move,{atom,accepted},{x,0}}.
    return.
  {label,6}.
    {move,{atom,rejected},{x,0}}.
    return.


{function, module_info, 0, 8}.
  {label,7}.
    {line,[]}.
    {func_info,{atom,beam_guard_pid_size_probe},{atom,module_info},0}.
  {label,8}.
    {move,{atom,beam_guard_pid_size_probe},{x,0}}.
    {call_ext_only,1,{extfunc,erlang,get_module_info,1}}.


{function, module_info, 1, 10}.
  {label,9}.
    {line,[]}.
    {func_info,{atom,beam_guard_pid_size_probe},{atom,module_info},1}.
  {label,10}.
    {move,{x,0},{x,1}}.
    {move,{atom,beam_guard_pid_size_probe},{x,0}}.
    {call_ext_only,2,{extfunc,erlang,get_module_info,2}}.
