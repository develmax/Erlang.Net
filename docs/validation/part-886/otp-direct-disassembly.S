{module, beam_div_list_ops_probe}.  %% version = 0

{exports, [{float,2},
           {guard,2},
           {int,2},
           {lists,2},
           {module_info,0},
           {module_info,1},
           {remainder,2},
           {shift,2}]}.

{attributes, []}.

{labels, 18}.


{function, int, 2, 2}.
  {label,1}.
    {line,[{location,"beam_div_list_ops_probe.erl",3}]}.
    {func_info,{atom,beam_div_list_ops_probe},{atom,int},2}.
  {label,2}.
    {gc_bif,'div',{f,0},2,[{x,0},{x,1}],{x,0}}.
    return.


{function, float, 2, 4}.
  {label,3}.
    {line,[{location,"beam_div_list_ops_probe.erl",4}]}.
    {func_info,{atom,beam_div_list_ops_probe},{atom,float},2}.
  {label,4}.
    {fconv,{x,0},{fr,0}}.
    {fconv,{x,1},{fr,1}}.
    {bif,fdiv,{f,0},[{fr,0},{fr,1}],{fr,0}}.
    {test_heap,{alloc,[{words,0},{floats,1},{funs,0}]},0}.
    {fmove,{fr,0},{x,0}}.
    return.


{function, lists, 2, 6}.
  {label,5}.
    {line,[{location,"beam_div_list_ops_probe.erl",5}]}.
    {func_info,{atom,beam_div_list_ops_probe},{atom,lists},2}.
  {label,6}.
    {call_ext_only,2,{extfunc,erlang,'--',2}}.


{function, guard, 2, 8}.
  {label,7}.
    {line,[{location,"beam_div_list_ops_probe.erl",6}]}.
    {func_info,{atom,beam_div_list_ops_probe},{atom,guard},2}.
  {label,8}.
    {gc_bif,'div',{f,9},2,[{x,0},{x,1}],{x,0}}.
    {test,is_ge,{f,9},[{tr,{x,0},{t_integer,any}},{integer,1}]}.
    {move,{atom,yes},{x,0}}.
    return.
  {label,9}.
    {move,{atom,no},{x,0}}.
    return.


{function, shift, 2, 11}.
  {label,10}.
    {line,[{location,"beam_div_list_ops_probe.erl",8}]}.
    {func_info,{atom,beam_div_list_ops_probe},{atom,shift},2}.
  {label,11}.
    {gc_bif,'bsl',{f,0},2,[{x,0},{x,1}],{x,0}}.
    return.


{function, remainder, 2, 13}.
  {label,12}.
    {line,[{location,"beam_div_list_ops_probe.erl",9}]}.
    {func_info,{atom,beam_div_list_ops_probe},{atom,remainder},2}.
  {label,13}.
    {gc_bif,'rem',{f,0},2,[{x,0},{x,1}],{x,0}}.
    return.


{function, module_info, 0, 15}.
  {label,14}.
    {line,[]}.
    {func_info,{atom,beam_div_list_ops_probe},{atom,module_info},0}.
  {label,15}.
    {move,{atom,beam_div_list_ops_probe},{x,0}}.
    {call_ext_only,1,{extfunc,erlang,get_module_info,1}}.


{function, module_info, 1, 17}.
  {label,16}.
    {line,[]}.
    {func_info,{atom,beam_div_list_ops_probe},{atom,module_info},1}.
  {label,17}.
    {move,{x,0},{x,1}}.
    {move,{atom,beam_div_list_ops_probe},{x,0}}.
    {call_ext_only,2,{extfunc,erlang,get_module_info,2}}.
