{module, beam_length_fun_guard_probe}.  %% version = 0

{exports, [{fixed,1},
           {len,1},
           {module_info,0},
           {module_info,1},
           {one,1},
           {two,2},
           {zero,1}]}.

{attributes, []}.

{labels, 20}.


{function, one, 1, 2}.
  {label,1}.
    {line,[{location,"beam_length_fun_guard_probe.erl",3}]}.
    {func_info,{atom,beam_length_fun_guard_probe},{atom,one},1}.
  {label,2}.
    {test,is_function,{f,3},[{x,0}]}.
    {move,{atom,yes},{x,0}}.
    return.
  {label,3}.
    {move,{atom,no},{x,0}}.
    return.


{function, two, 2, 5}.
  {label,4}.
    {line,[{location,"beam_length_fun_guard_probe.erl",5}]}.
    {func_info,{atom,beam_length_fun_guard_probe},{atom,two},2}.
  {label,5}.
    {bif,is_function,{f,6},[{x,0},{x,1}],{x,0}}.
    {test,is_eq_exact,{f,6},[{x,0},{atom,true}]}.
    {move,{atom,yes},{x,0}}.
    return.
  {label,6}.
    {move,{atom,no},{x,0}}.
    return.


{function, len, 1, 8}.
  {label,7}.
    {line,[{location,"beam_length_fun_guard_probe.erl",7}]}.
    {func_info,{atom,beam_length_fun_guard_probe},{atom,len},1}.
  {label,8}.
    {gc_bif,length,{f,9},1,[{x,0}],{x,0}}.
    {test,is_eq_exact,
          {f,9},
          [{tr,{x,0},{t_integer,{0,288230376151711743}}},{integer,2}]}.
    {move,{atom,yes},{x,0}}.
    return.
  {label,9}.
    {move,{atom,no},{x,0}}.
    return.


{function, zero, 1, 11}.
  {label,10}.
    {line,[{location,"beam_length_fun_guard_probe.erl",9}]}.
    {func_info,{atom,beam_length_fun_guard_probe},{atom,zero},1}.
  {label,11}.
    {gc_bif,length,{f,12},1,[{x,0}],{x,0}}.
    {test,is_eq_exact,
          {f,12},
          [{tr,{x,0},{t_integer,{0,288230376151711743}}},{integer,0}]}.
    {move,{atom,yes},{x,0}}.
    return.
  {label,12}.
    {move,{atom,no},{x,0}}.
    return.


{function, fixed, 1, 14}.
  {label,13}.
    {line,[{location,"beam_length_fun_guard_probe.erl",12}]}.
    {func_info,{atom,beam_length_fun_guard_probe},{atom,fixed},1}.
  {label,14}.
    {test,is_function2,{f,15},[{x,0},{integer,2}]}.
    {move,{atom,yes},{x,0}}.
    return.
  {label,15}.
    {move,{atom,no},{x,0}}.
    return.


{function, module_info, 0, 17}.
  {label,16}.
    {line,[]}.
    {func_info,{atom,beam_length_fun_guard_probe},{atom,module_info},0}.
  {label,17}.
    {move,{atom,beam_length_fun_guard_probe},{x,0}}.
    {call_ext_only,1,{extfunc,erlang,get_module_info,1}}.


{function, module_info, 1, 19}.
  {label,18}.
    {line,[]}.
    {func_info,{atom,beam_length_fun_guard_probe},{atom,module_info},1}.
  {label,19}.
    {move,{x,0},{x,1}}.
    {move,{atom,beam_length_fun_guard_probe},{x,0}}.
    {call_ext_only,2,{extfunc,erlang,get_module_info,2}}.
