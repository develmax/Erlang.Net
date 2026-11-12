{module, beam_node_size_guards}.  %% version = 0

{exports, [{bytes,1},
           {floating,1},
           {local,1},
           {module_info,0},
           {module_info,1},
           {port,1},
           {remote,1}]}.

{attributes, []}.

{labels, 20}.


{function, port, 1, 2}.
  {label,1}.
    {line,[{location,"beam_node_size_guards.erl",3}]}.
    {func_info,{atom,beam_node_size_guards},{atom,port},1}.
  {label,2}.
    {test,is_port,{f,3},[{x,0}]}.
    {move,{atom,yes},{x,0}}.
    return.
  {label,3}.
    {move,{atom,no},{x,0}}.
    return.


{function, floating, 1, 5}.
  {label,4}.
    {line,[{location,"beam_node_size_guards.erl",5}]}.
    {func_info,{atom,beam_node_size_guards},{atom,floating},1}.
  {label,5}.
    {test,is_float,{f,6},[{x,0}]}.
    {move,{atom,yes},{x,0}}.
    return.
  {label,6}.
    {move,{atom,no},{x,0}}.
    return.


{function, remote, 1, 8}.
  {label,7}.
    {line,[{location,"beam_node_size_guards.erl",7}]}.
    {func_info,{atom,beam_node_size_guards},{atom,remote},1}.
  {label,8}.
    {bif,node,{f,9},[{x,0}],{x,0}}.
    {test,is_eq_exact,{f,9},[{tr,{x,0},{t_atom,any}},{atom,example}]}.
    {move,{atom,yes},{x,0}}.
    return.
  {label,9}.
    {move,{atom,no},{x,0}}.
    return.


{function, bytes, 1, 11}.
  {label,10}.
    {line,[{location,"beam_node_size_guards.erl",9}]}.
    {func_info,{atom,beam_node_size_guards},{atom,bytes},1}.
  {label,11}.
    {gc_bif,byte_size,{f,12},1,[{x,0}],{x,0}}.
    {test,is_eq_exact,
          {f,12},
          [{tr,{x,0},{t_integer,{0,288230376151711743}}},{integer,2}]}.
    {move,{atom,yes},{x,0}}.
    return.
  {label,12}.
    {move,{atom,no},{x,0}}.
    return.


{function, local, 1, 14}.
  {label,13}.
    {line,[{location,"beam_node_size_guards.erl",11}]}.
    {func_info,{atom,beam_node_size_guards},{atom,local},1}.
  {label,14}.
    {bif,node,{f,0},[],{x,1}}.
    {test,is_eq_exact,{f,15},[{tr,{x,1},{t_atom,any}},{x,0}]}.
    {move,{atom,yes},{x,0}}.
    return.
  {label,15}.
    {move,{atom,no},{x,0}}.
    return.


{function, module_info, 0, 17}.
  {label,16}.
    {line,[]}.
    {func_info,{atom,beam_node_size_guards},{atom,module_info},0}.
  {label,17}.
    {move,{atom,beam_node_size_guards},{x,0}}.
    {call_ext_only,1,{extfunc,erlang,get_module_info,1}}.


{function, module_info, 1, 19}.
  {label,18}.
    {line,[]}.
    {func_info,{atom,beam_node_size_guards},{atom,module_info},1}.
  {label,19}.
    {move,{x,0},{x,1}}.
    {move,{atom,beam_node_size_guards},{x,0}}.
    {call_ext_only,2,{extfunc,erlang,get_module_info,2}}.
