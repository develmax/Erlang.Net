{module, beam_fixed_bits_probe}.  %% version = 0

{exports, [{module_info,0},{module_info,1},{read,1}]}.

{attributes, []}.

{labels, 8}.


{function, read, 1, 2}.
  {label,1}.
    {line,[{location,"beam_fixed_bits_probe.erl",3}]}.
    {func_info,{atom,beam_fixed_bits_probe},{atom,read},1}.
  {label,2}.
    {'%',{var_info,{x,0},[accepts_match_context]}}.
    {test,bs_start_match3,{f,3},1,[{x,0}],{x,0}}.
    {bs_match,{f,3},
              {x,0},
              {commands,[{ensure_exactly,16},
                         {integer,1,{literal,[signed,little]},16,1,{x,1}}]}}.
    {move,{x,1},{x,0}}.
    return.
  {label,3}.
    {move,{atom,no},{x,0}}.
    return.


{function, module_info, 0, 5}.
  {label,4}.
    {line,[]}.
    {func_info,{atom,beam_fixed_bits_probe},{atom,module_info},0}.
  {label,5}.
    {move,{atom,beam_fixed_bits_probe},{x,0}}.
    {call_ext_only,1,{extfunc,erlang,get_module_info,1}}.


{function, module_info, 1, 7}.
  {label,6}.
    {line,[]}.
    {func_info,{atom,beam_fixed_bits_probe},{atom,module_info},1}.
  {label,7}.
    {move,{x,0},{x,1}}.
    {move,{atom,beam_fixed_bits_probe},{x,0}}.
    {call_ext_only,2,{extfunc,erlang,get_module_info,2}}.
