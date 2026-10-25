-module(otp_fgraph479_observer).
-export([worker/1,run/0,basic/0,layout/3,default_point/1]).
-include("reltool_fgraph.hrl").
-file("otp_fgraph479_observer.erl",1).
worker(Parent)->Parent!{finished,run()}.
run()->[observe(Id,F,A)||{Id,F,A}<-cases()].
observe(Id,F,A)->clear(),try apply(?MODULE,F,A) of V->{Id,{returned,V}} catch C:R:S->{Id,{caught,C,R,S}} after clear() end.
clear()->[erase(K)||K<-[first,second,{first,second},visited]],ok.
basic()->
 G0=reltool_fgraph:new(),
 G1=reltool_fgraph:add(first,1,G0),G2=reltool_fgraph:add(second,2,G1),
 Before={reltool_fgraph:size(G2),reltool_fgraph:is_defined(first,G2),reltool_fgraph:get(first,G2),reltool_fgraph:is_defined(missing,G2)},
 G3=reltool_fgraph:set(first,3,G2),
 reltool_fgraph:foreach(fun({K,V})->put(visited,[{K,V}|case get(visited) of undefined->[];X->X end]) end,G3),
 Visited=get(visited),
 G4=reltool_fgraph:map(fun({K,V})->{K,V*2} end,G3),
 Folded=reltool_fgraph:foldl(fun({K,V},Acc)->[{K,V}|Acc] end,[],G4),
 {G5,Acc}=reltool_fgraph:mapfoldl(fun({K,V},N)->{{K,V+N},N+1} end,0,G4),
 G6=reltool_fgraph:del(second,G5),
 {G0,G1,G2,Before,Visited,G4,Folded,G5,Acc,G6,reltool_fgraph:get(first,G6),reltool_fgraph:get(second,G6)}.
graph(empty)->{[],[]};
graph(single)->{reltool_fgraph:add(first,#fg_v{p={20.0,30.0},v={1.0,-1.0}},[]),[]};
graph(static)->{reltool_fgraph:add(first,#fg_v{p={20.0,30.0},type=static},[]),[]};
graph(Mode)->
 G=reltool_fgraph:add(second,#fg_v{p={-10.0,15.0},type=case Mode of mixed->static;_->dynamic end},reltool_fgraph:add(first,#fg_v{p={20.0,30.0}},[])),
 E=reltool_fgraph:add({first,second},#fg_e{},[]),{G,E}.
layout(Mode,N,Point)->{G,E}=graph(Mode),Final=steps(N,G,E,Point),snapshot(Final).
steps(0,G,_,_)->G;
steps(N,G,E,P)->steps(N-1,reltool_fgraph:step(G,E,P),E,P).
snapshot(G)->{G,[{K,reltool_fgraph:get(K,G)}||K<-G]}.
default_point(Mode)->{G,E}=graph(Mode),snapshot(reltool_fgraph:step(G,E)).
cases()->[{basic,basic,[]},{{layout,empty,0,{0.0,0.0}},layout,[empty,0,{0.0,0.0}]},{{layout,empty,0,{10.0,-4.0}},layout,[empty,0,{10.0,-4.0}]},{{layout,empty,0,{0,0}},layout,[empty,0,{0,0}]},{{layout,empty,1,{0.0,0.0}},layout,[empty,1,{0.0,0.0}]},{{layout,empty,1,{10.0,-4.0}},layout,[empty,1,{10.0,-4.0}]},{{layout,empty,1,{0,0}},layout,[empty,1,{0,0}]},{{layout,empty,2,{0.0,0.0}},layout,[empty,2,{0.0,0.0}]},{{layout,empty,2,{10.0,-4.0}},layout,[empty,2,{10.0,-4.0}]},{{layout,empty,2,{0,0}},layout,[empty,2,{0,0}]},{{layout,empty,5,{0.0,0.0}},layout,[empty,5,{0.0,0.0}]},{{layout,empty,5,{10.0,-4.0}},layout,[empty,5,{10.0,-4.0}]},{{layout,empty,5,{0,0}},layout,[empty,5,{0,0}]},{{layout,single,0,{0.0,0.0}},layout,[single,0,{0.0,0.0}]},{{layout,single,0,{10.0,-4.0}},layout,[single,0,{10.0,-4.0}]},{{layout,single,0,{0,0}},layout,[single,0,{0,0}]},{{layout,single,1,{0.0,0.0}},layout,[single,1,{0.0,0.0}]},{{layout,single,1,{10.0,-4.0}},layout,[single,1,{10.0,-4.0}]},{{layout,single,1,{0,0}},layout,[single,1,{0,0}]},{{layout,single,2,{0.0,0.0}},layout,[single,2,{0.0,0.0}]},{{layout,single,2,{10.0,-4.0}},layout,[single,2,{10.0,-4.0}]},{{layout,single,2,{0,0}},layout,[single,2,{0,0}]},{{layout,single,5,{0.0,0.0}},layout,[single,5,{0.0,0.0}]},{{layout,single,5,{10.0,-4.0}},layout,[single,5,{10.0,-4.0}]},{{layout,single,5,{0,0}},layout,[single,5,{0,0}]},{{layout,static,0,{0.0,0.0}},layout,[static,0,{0.0,0.0}]},{{layout,static,0,{10.0,-4.0}},layout,[static,0,{10.0,-4.0}]},{{layout,static,0,{0,0}},layout,[static,0,{0,0}]},{{layout,static,1,{0.0,0.0}},layout,[static,1,{0.0,0.0}]},{{layout,static,1,{10.0,-4.0}},layout,[static,1,{10.0,-4.0}]},{{layout,static,1,{0,0}},layout,[static,1,{0,0}]},{{layout,static,2,{0.0,0.0}},layout,[static,2,{0.0,0.0}]},{{layout,static,2,{10.0,-4.0}},layout,[static,2,{10.0,-4.0}]},{{layout,static,2,{0,0}},layout,[static,2,{0,0}]},{{layout,static,5,{0.0,0.0}},layout,[static,5,{0.0,0.0}]},{{layout,static,5,{10.0,-4.0}},layout,[static,5,{10.0,-4.0}]},{{layout,static,5,{0,0}},layout,[static,5,{0,0}]},{{layout,two,0,{0.0,0.0}},layout,[two,0,{0.0,0.0}]},{{layout,two,0,{10.0,-4.0}},layout,[two,0,{10.0,-4.0}]},{{layout,two,0,{0,0}},layout,[two,0,{0,0}]},{{layout,two,1,{0.0,0.0}},layout,[two,1,{0.0,0.0}]},{{layout,two,1,{10.0,-4.0}},layout,[two,1,{10.0,-4.0}]},{{layout,two,1,{0,0}},layout,[two,1,{0,0}]},{{layout,two,2,{0.0,0.0}},layout,[two,2,{0.0,0.0}]},{{layout,two,2,{10.0,-4.0}},layout,[two,2,{10.0,-4.0}]},{{layout,two,2,{0,0}},layout,[two,2,{0,0}]},{{layout,two,5,{0.0,0.0}},layout,[two,5,{0.0,0.0}]},{{layout,two,5,{10.0,-4.0}},layout,[two,5,{10.0,-4.0}]},{{layout,two,5,{0,0}},layout,[two,5,{0,0}]},{{layout,mixed,0,{0.0,0.0}},layout,[mixed,0,{0.0,0.0}]},{{layout,mixed,0,{10.0,-4.0}},layout,[mixed,0,{10.0,-4.0}]},{{layout,mixed,0,{0,0}},layout,[mixed,0,{0,0}]},{{layout,mixed,1,{0.0,0.0}},layout,[mixed,1,{0.0,0.0}]},{{layout,mixed,1,{10.0,-4.0}},layout,[mixed,1,{10.0,-4.0}]},{{layout,mixed,1,{0,0}},layout,[mixed,1,{0,0}]},{{layout,mixed,2,{0.0,0.0}},layout,[mixed,2,{0.0,0.0}]},{{layout,mixed,2,{10.0,-4.0}},layout,[mixed,2,{10.0,-4.0}]},{{layout,mixed,2,{0,0}},layout,[mixed,2,{0,0}]},{{layout,mixed,5,{0.0,0.0}},layout,[mixed,5,{0.0,0.0}]},{{layout,mixed,5,{10.0,-4.0}},layout,[mixed,5,{10.0,-4.0}]},{{layout,mixed,5,{0,0}},layout,[mixed,5,{0,0}]},{{default_point,empty},default_point,[empty]},{{default_point,single},default_point,[single]},{{default_point,static},default_point,[static]},{{default_point,two},default_point,[two]},{{default_point,mixed},default_point,[mixed]}].
