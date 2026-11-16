{module, beam_guard_extrema}.  %% version = 0

{exports, [{maximum,2},
           {minimum,2},
           {module_info,0},
           {module_info,1},
           {rounded,1}]}.

{attributes, []}.

{labels, 14}.


{function, minimum, 2, 2}.
  {label,1}.
    {line,[{location,"beam_guard_extrema.erl",3}]}.
    {func_info,{atom,beam_guard_extrema},{atom,minimum},2}.
  {label,2}.
    {bif,min,{f,0},[{x,0},{x,1}],{x,1}}.
    {test,is_eq_exact,{f,3},[{x,1},{x,0}]}.
    {move,{atom,yes},{x,0}}.
    return.
  {label,3}.
    {move,{atom,no},{x,0}}.
    return.


{function, maximum, 2, 5}.
  {label,4}.
    {line,[{location,"beam_guard_extrema.erl",5}]}.
    {func_info,{atom,beam_guard_extrema},{atom,maximum},2}.
  {label,5}.
    {bif,max,{f,0},[{x,0},{x,1}],{x,0}}.
    {test,is_eq_exact,{f,6},[{x,0},{x,1}]}.
    {move,{atom,yes},{x,0}}.
    return.
  {label,6}.
    {move,{atom,no},{x,0}}.
    return.


{function, rounded, 1, 8}.
  {label,7}.
    {line,[{location,"beam_guard_extrema.erl",7}]}.
    {func_info,{atom,beam_guard_extrema},{atom,rounded},1}.
  {label,8}.
    {gc_bif,float,{f,9},1,[{x,0}],{x,0}}.
    {test,is_eq_exact,{f,9},[{tr,{x,0},{t_float,any}},{float,1.0}]}.
    {move,{atom,yes},{x,0}}.
    return.
  {label,9}.
    {move,{atom,no},{x,0}}.
    return.


{function, module_info, 0, 11}.
  {label,10}.
    {line,[]}.
    {func_info,{atom,beam_guard_extrema},{atom,module_info},0}.
  {label,11}.
    {move,{atom,beam_guard_extrema},{x,0}}.
    {call_ext_only,1,{extfunc,erlang,get_module_info,1}}.


{function, module_info, 1, 13}.
  {label,12}.
    {line,[]}.
    {func_info,{atom,beam_guard_extrema},{atom,module_info},1}.
  {label,13}.
    {move,{x,0},{x,1}}.
    {move,{atom,beam_guard_extrema},{x,0}}.
    {call_ext_only,2,{extfunc,erlang,get_module_info,2}}.
