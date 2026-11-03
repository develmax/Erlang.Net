{module, register_capacity273_wide}.
{exports, [{run,0}]}.
{attributes, []}.
{labels, 9}.
{function, run, 0, 2}.
  {label,1}.
  {func_info,{atom,register_capacity273_wide},{atom,run},0}.
  {label,2}.
  {allocate,0,0}.
  {move,{atom,wide_payload},{x,0}}.
  {call,1,{f,4}}.
  {deallocate,0}.
  return.
{function, callee, 1, 4}.
  {label,3}.
  {func_info,{atom,register_capacity273_wide},{atom,callee},1}.
  {label,4}.
  {move,{x,0},{x,63}}.
  {move,{x,63},{x,0}}.
  {call_only,1,{f,6}}.
{function, tail_target, 1, 6}.
  {label,5}.
  {func_info,{atom,register_capacity273_wide},{atom,tail_target},1}.
  {label,6}.
  {allocate,0,1}.
  {call_last,1,{f,8},0}.
{function, last_target, 1, 8}.
  {label,7}.
  {func_info,{atom,register_capacity273_wide},{atom,last_target},1}.
  {label,8}.
  return.
