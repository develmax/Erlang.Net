{module, beam_map_emit_probe}.  %% version = 0

{exports, [{empty,1},
           {exact,2},
           {insert,2},
           {make,2},
           {module_info,0},
           {module_info,1}]}.

{attributes, []}.

{labels, 16}.


{function, make, 2, 2}.
  {label,1}.
    {line,[{location,"beam_map_emit_probe.erl",3}]}.
    {func_info,{atom,beam_map_emit_probe},{atom,make},2}.
  {label,2}.
    {put_map_assoc,{f,0},
                   {literal,#{}},
                   {x,0},
                   2,
                   {list,[{atom,a},{x,1},{atom,z},{x,0}]}}.
    return.


{function, insert, 2, 4}.
  {label,3}.
    {line,[{location,"beam_map_emit_probe.erl",4}]}.
    {func_info,{atom,beam_map_emit_probe},{atom,insert},2}.
  {label,4}.
    {test,is_map,{f,5},[{x,0}]}.
    {put_map_assoc,{f,0},{x,0},{x,0},2,{list,[{atom,key},{x,1}]}}.
    return.
  {label,5}.
    {test_heap,3,1}.
    {put_tuple2,{x,0},{list,[{atom,badmap},{x,0}]}}.
    {call_ext_only,1,{extfunc,erlang,error,1}}.


{function, exact, 2, 7}.
  {label,6}.
    {line,[{location,"beam_map_emit_probe.erl",5}]}.
    {func_info,{atom,beam_map_emit_probe},{atom,exact},2}.
  {label,7}.
    {test,is_map,{f,8},[{x,0}]}.
    {put_map_exact,{f,0},{x,0},{x,0},2,{list,[{atom,key},{x,1}]}}.
    return.
  {label,8}.
    {test_heap,3,1}.
    {put_tuple2,{x,0},{list,[{atom,badmap},{x,0}]}}.
    {call_ext_only,1,{extfunc,erlang,error,1}}.


{function, empty, 1, 10}.
  {label,9}.
    {line,[{location,"beam_map_emit_probe.erl",6}]}.
    {func_info,{atom,beam_map_emit_probe},{atom,empty},1}.
  {label,10}.
    {test,is_map,{f,11},[{x,0}]}.
    return.
  {label,11}.
    {test_heap,3,1}.
    {put_tuple2,{x,0},{list,[{atom,badmap},{x,0}]}}.
    {call_ext_only,1,{extfunc,erlang,error,1}}.


{function, module_info, 0, 13}.
  {label,12}.
    {line,[]}.
    {func_info,{atom,beam_map_emit_probe},{atom,module_info},0}.
  {label,13}.
    {move,{atom,beam_map_emit_probe},{x,0}}.
    {call_ext_only,1,{extfunc,erlang,get_module_info,1}}.


{function, module_info, 1, 15}.
  {label,14}.
    {line,[]}.
    {func_info,{atom,beam_map_emit_probe},{atom,module_info},1}.
  {label,15}.
    {move,{x,0},{x,1}}.
    {move,{atom,beam_map_emit_probe},{x,0}}.
    {call_ext_only,2,{extfunc,erlang,get_module_info,2}}.
