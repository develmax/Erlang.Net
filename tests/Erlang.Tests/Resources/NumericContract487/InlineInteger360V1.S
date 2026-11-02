{module, inline_integer_literal360}.
{exports, [{value,0},{same,0}]}.
{attributes, []}.
{labels, 5}.
{function, value, 0, 2}.
  {label,1}.
  {func_info,{atom,inline_integer_literal360},{atom,value},0}.
  {label,2}.
  {move,{integer,123456789012345678901234567890},{x,0}}.
  return.
{function, same, 0, 4}.
  {label,3}.
  {func_info,{atom,inline_integer_literal360},{atom,same},0}.
  {label,4}.
  {move,{integer,123456789012345678901234567890},{x,0}}.
  {move,{integer,123456789012345678901234567890},{x,1}}.
  {call_ext_only,2,{extfunc,erts_debug,same,2}}.
