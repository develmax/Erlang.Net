{module, beam_not_product_probe}.  %% version = 0

{exports, [{field,2},
           {guard,1},
           {module_info,0},
           {module_info,1},
           {neg,1},
           {product,2}]}.

{attributes, []}.

{labels, 15}.


{function, neg, 1, 2}.
  {label,1}.
    {line,[{location,"beam_not_product_probe.erl",3}]}.
    {func_info,{atom,beam_not_product_probe},{atom,neg},1}.
  {label,2}.
    {bif,'not',{f,0},[{x,0}],{x,0}}.
    return.


{function, product, 2, 4}.
  {label,3}.
    {line,[{location,"beam_not_product_probe.erl",4}]}.
    {func_info,{atom,beam_not_product_probe},{atom,product},2}.
  {label,4}.
    {gc_bif,'*',{f,0},2,[{x,0},{x,1}],{x,0}}.
    return.


{function, guard, 1, 6}.
  {label,5}.
    {line,[{location,"beam_not_product_probe.erl",5}]}.
    {func_info,{atom,beam_not_product_probe},{atom,guard},1}.
  {label,6}.
    {test,is_eq_exact,{f,7},[{x,0},{atom,false}]}.
    {move,{atom,yes},{x,0}}.
    return.
  {label,7}.
    {move,{atom,no},{x,0}}.
    return.


{function, field, 2, 9}.
  {label,8}.
    {line,[{location,"beam_not_product_probe.erl",8}]}.
    {func_info,{atom,beam_not_product_probe},{atom,field},2}.
  {label,9}.
    {bif,element,{f,10},[{x,0},{x,1}],{x,0}}.
    {test,is_eq_exact,{f,10},[{x,0},{atom,ok}]}.
    {move,{atom,yes},{x,0}}.
    return.
  {label,10}.
    {move,{atom,no},{x,0}}.
    return.


{function, module_info, 0, 12}.
  {label,11}.
    {line,[]}.
    {func_info,{atom,beam_not_product_probe},{atom,module_info},0}.
  {label,12}.
    {move,{atom,beam_not_product_probe},{x,0}}.
    {call_ext_only,1,{extfunc,erlang,get_module_info,1}}.


{function, module_info, 1, 14}.
  {label,13}.
    {line,[]}.
    {func_info,{atom,beam_not_product_probe},{atom,module_info},1}.
  {label,14}.
    {move,{x,0},{x,1}}.
    {move,{atom,beam_not_product_probe},{x,0}}.
    {call_ext_only,2,{extfunc,erlang,get_module_info,2}}.
