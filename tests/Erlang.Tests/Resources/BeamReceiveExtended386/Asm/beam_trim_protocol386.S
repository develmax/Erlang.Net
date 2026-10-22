{module, beam_trim_protocol386}.
{exports, [{run,1}, {nested,1}]}.
{attributes, []}.
{labels, 11}.

{function, run, 1, 2}.
  {label,1}.
    {line,[{location,"beam_trim_protocol386.S",8}]}.
    {func_info,{atom,beam_trim_protocol386},{atom,run},1}.
  {label,2}.
    {allocate,4,1}.
    {init_yregs,{list,[{y,0},{y,1},{y,2},{y,3}]}}.
    {'try',{y,3},{f,5}}.
    {trim,1,3}.
    {test,is_eq_exact,{f,3},[{x,0},{atom,normal}]}.
    {try_end,{y,2}}.
    {move,{atom,trim_normal},{x,0}}.
    {deallocate,3}.
    return.
  {label,3}.
    {test,is_eq_exact,{f,4},[{x,0},{atom,throw}]}.
    {move,{literal,{trim_throw,[first,second]}},{x,0}}.
    {line,[{location,"beam_trim_protocol386.S",24}]}.
    {call_ext,1,{extfunc,erlang,throw,1}}.
  {label,4}.
    {move,{literal,{trim_error,#{detail => preserved}}},{x,0}}.
    {line,[{location,"beam_trim_protocol386.S",28}]}.
    {call_ext,1,{extfunc,erlang,error,1}}.
  {label,5}.
    {try_case,{y,2}}.
    {move,{x,0},{y,0}}.
    {move,{x,1},{y,1}}.
    {move,{x,2},{x,0}}.
    build_stacktrace.
    {test_heap,5,1}.
    {put_tuple2,{x,0},{list,[{atom,caught},{y,0},{y,1},{x,0}]}}.
    {deallocate,3}.
    return.

{function, nested, 1, 7}.
  {label,6}.
    {line,[{location,"beam_trim_protocol386.S",42}]}.
    {func_info,{atom,beam_trim_protocol386},{atom,nested},1}.
  {label,7}.
    {allocate,5,1}.
    {init_yregs,{list,[{y,0},{y,1},{y,2},{y,3},{y,4}]}}.
    {'try',{y,4},{f,10}}.
    {'try',{y,3},{f,9}}.
    {trim,1,4}.
    {test,is_eq_exact,{f,8},[{x,0},{atom,nested_normal}]}.
    {try_end,{y,2}}.
    {try_end,{y,3}}.
    {move,{atom,nested_trim_normal},{x,0}}.
    {deallocate,4}.
    return.
  {label,8}.
    {move,{literal,{inner_trim_throw,[first,second]}},{x,0}}.
    {line,[{location,"beam_trim_protocol386.S",59}]}.
    {call_ext,1,{extfunc,erlang,throw,1}}.
  {label,9}.
    {try_case,{y,2}}.
    {move,{x,0},{y,0}}.
    {move,{x,1},{y,1}}.
    {move,{x,2},{x,0}}.
    build_stacktrace.
    {test_heap,5,1}.
    {put_tuple2,{x,0},{list,[{atom,inner_caught},{y,0},{y,1},{x,0}]}}.
    {line,[{location,"beam_trim_protocol386.S",70}]}.
    {call_ext,1,{extfunc,erlang,error,1}}.
  {label,10}.
    {try_case,{y,3}}.
    {move,{x,0},{y,0}}.
    {move,{x,1},{y,1}}.
    {move,{x,2},{x,0}}.
    build_stacktrace.
    {test_heap,5,1}.
    {put_tuple2,{x,0},{list,[{atom,outer_caught},{y,0},{y,1},{x,0}]}}.
    {deallocate,4}.
    return.
