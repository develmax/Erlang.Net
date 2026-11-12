{module, beam_bit_emit_probe}.  %% version = 0

{exports, [{byte,1},
           {floats,1},
           {mixed,2},
           {module_info,0},
           {module_info,1},
           {size,2},
           {suffix,1},
           {utf,2}]}.

{attributes, []}.

{labels, 17}.


{function, byte, 1, 2}.
  {label,1}.
    {line,[{location,"beam_bit_emit_probe.erl",3}]}.
    {func_info,{atom,beam_bit_emit_probe},{atom,byte},1}.
  {label,2}.
    {bs_create_bin,{f,0},
                   0,1,8,
                   {x,0},
                   {list,[{atom,integer},1,1,nil,{x,0},{integer,8}]}}.
    return.


{function, size, 2, 4}.
  {label,3}.
    {line,[{location,"beam_bit_emit_probe.erl",4}]}.
    {func_info,{atom,beam_bit_emit_probe},{atom,size},2}.
  {label,4}.
    {bs_create_bin,{f,0},
                   0,2,2,
                   {x,0},
                   {list,[{atom,integer},1,2,{literal,[little]},{x,0},{x,1}]}}.
    return.


{function, mixed, 2, 6}.
  {label,5}.
    {line,[{location,"beam_bit_emit_probe.erl",5}]}.
    {func_info,{atom,beam_bit_emit_probe},{atom,mixed},2}.
  {label,6}.
    {bs_create_bin,{f,0},
                   0,2,1,
                   {x,0},
                   {list,[{atom,integer},
                          0,1,nil,
                          {integer,17},
                          {integer,5},
                          {atom,integer},
                          2,1,
                          {literal,[little]},
                          {x,0},
                          {integer,12},
                          {atom,binary},
                          3,1,nil,
                          {x,1},
                          {atom,all}]}}.
    return.


{function, utf, 2, 8}.
  {label,7}.
    {line,[{location,"beam_bit_emit_probe.erl",6}]}.
    {func_info,{atom,beam_bit_emit_probe},{atom,utf},2}.
  {label,8}.
    {bs_create_bin,{f,0},
                   0,2,8,
                   {x,0},
                   {list,[{atom,utf8},
                          1,0,nil,
                          {x,0},
                          {atom,undefined},
                          {atom,utf16},
                          2,0,
                          {literal,[little]},
                          {x,1},
                          {atom,undefined}]}}.
    return.


{function, suffix, 1, 10}.
  {label,9}.
    {line,[{location,"beam_bit_emit_probe.erl",7}]}.
    {func_info,{atom,beam_bit_emit_probe},{atom,suffix},1}.
  {label,10}.
    {bs_create_bin,{f,0},
                   0,1,8,
                   {x,0},
                   {list,[{atom,append},
                          1,8,nil,
                          {x,0},
                          {atom,all},
                          {atom,string},
                          0,8,nil,
                          {string,<<"x">>},
                          {integer,1}]}}.
    return.


{function, floats, 1, 12}.
  {label,11}.
    {line,[{location,"beam_bit_emit_probe.erl",8}]}.
    {func_info,{atom,beam_bit_emit_probe},{atom,floats},1}.
  {label,12}.
    {bs_create_bin,{f,0},
                   0,1,32,
                   {x,0},
                   {list,[{atom,float},
                          1,1,
                          {literal,[little]},
                          {x,0},
                          {integer,32}]}}.
    return.


{function, module_info, 0, 14}.
  {label,13}.
    {line,[]}.
    {func_info,{atom,beam_bit_emit_probe},{atom,module_info},0}.
  {label,14}.
    {move,{atom,beam_bit_emit_probe},{x,0}}.
    {call_ext_only,1,{extfunc,erlang,get_module_info,1}}.


{function, module_info, 1, 16}.
  {label,15}.
    {line,[]}.
    {func_info,{atom,beam_bit_emit_probe},{atom,module_info},1}.
  {label,16}.
    {move,{x,0},{x,1}}.
    {move,{atom,beam_bit_emit_probe},{x,0}}.
    {call_ext_only,2,{extfunc,erlang,get_module_info,2}}.
