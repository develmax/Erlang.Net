-module(otp_crashdump482_observer).
-export([worker/1]).
worker(P) ->
 Cases=[
  {empty_list,to_value_list,[[]]},
  {list,to_value_list,[[one,two]]},
  {record,to_value_list,[{record,a,2}]},
  {tag_only,to_value_list,[{record}]},
  {empty_tuple,to_value_list,[{}]},
  {atom,to_value_list,[hello]},
  {integer,to_value_list,[17]},
  {map,to_value_list,[#{a=>1}]},
  {binary,to_value_list,[<<1>>]},
  {fields,to_proplist,[[a,b],{record,1,2}]},
  {short_fields,to_proplist,[[a],{record,1,2}]},
  {short_record,to_proplist,[[a,b],{record,1}]},
  {empty_fields,to_proplist,[[],{record}]},
  {list_record,to_proplist,[[a,b],[1,2]]},
  {bad_record,to_proplist,[[a],wrong]},
  {bad_fields,to_proplist,[bad,{record,1}]},
  {map_field,to_proplist,[[#{k=>v}],{record,{nested,[1,2]}}]},
  {change_atom,code_change,[old,state,extra]},
  {change_map,code_change,[{old,1},#{queue=>[a,b],version=>2},[]]},
  {change_integer,code_change,[[],17,#{extra=>true}]}
 ],
 R=[one(Name,F,Args)||{Name,F,Args}<-Cases],P!{finished,R}.
one(Name,F,Args) ->
 R=try erlang:apply(crashdump_viewer,F,Args) of Value->{returned,Value}
   catch Class:Reason:Stack->{caught,Class,Reason,Stack} end,
 {Name,F,Args,R}.
