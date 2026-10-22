{module,frame_join394}.
{exports,[{branch_badmatch,1},{branch_case,1},{branch_if,1},{failure_last,1},{valid_last,1},{module_info,0},{module_info,1}]}.
{attributes,[]}.
{labels,21}.

{function,branch_badmatch,1,2}.
  {label,1}.
  {func_info,{atom,frame_join394},{atom,branch_badmatch},1}.
  {label,2}.
  {test,is_eq_exact,{f,3},[{x,0},{atom,small}]}.
  {allocate,1,1}.
  {move,{x,0},{y,0}}.
  {jump,{f,4}}.
  {label,3}.
  {allocate,2,1}.
  {init_yregs,{list,[{y,0},{y,1}]}}.
  {label,4}.
  {badmatch,{x,0}}.

{function,branch_case,1,6}.
  {label,5}.
  {func_info,{atom,frame_join394},{atom,branch_case},1}.
  {label,6}.
  {test,is_eq_exact,{f,7},[{x,0},{atom,small}]}.
  {allocate,0,1}.
  {jump,{f,8}}.
  {label,7}.
  {allocate,2,1}.
  {init_yregs,{list,[{y,0},{y,1}]}}.
  {label,8}.
  {case_end,{x,0}}.

{function,branch_if,1,10}.
  {label,9}.
  {func_info,{atom,frame_join394},{atom,branch_if},1}.
  {label,10}.
  {test,is_eq_exact,{f,11},[{x,0},{atom,small}]}.
  {allocate,1,1}.
  {init_yregs,{list,[{y,0}]}}.
  {jump,{f,12}}.
  {label,11}.
  {allocate,3,1}.
  {init_yregs,{list,[{y,0},{y,1},{y,2}]}}.
  {label,12}.
  if_end.

{function,failure_last,1,14}.
  {label,13}.
  {func_info,{atom,frame_join394},{atom,failure_last},1}.
  {label,14}.
  {allocate,1,1}.
  {move,{x,0},{y,0}}.
  {call_ext_last,1,{extfunc,erlang,error,1},3}.

{function,valid_last,1,16}.
  {label,15}.
  {func_info,{atom,frame_join394},{atom,valid_last},1}.
  {label,16}.
  {allocate,1,1}.
  {move,{x,0},{y,0}}.
  {call_ext_last,1,{extfunc,erlang,length,1},1}.

{function,module_info,0,18}.
  {label,17}.
  {func_info,{atom,frame_join394},{atom,module_info},0}.
  {label,18}.
  {move,{atom,frame_join394},{x,0}}.
  {call_ext_only,1,{extfunc,erlang,get_module_info,1}}.

{function,module_info,1,20}.
  {label,19}.
  {func_info,{atom,frame_join394},{atom,module_info},1}.
  {label,20}.
  {move,{x,0},{x,1}}.
  {move,{atom,frame_join394},{x,0}}.
  {call_ext_only,2,{extfunc,erlang,get_module_info,2}}.
