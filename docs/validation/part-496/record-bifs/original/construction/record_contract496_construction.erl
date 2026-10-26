-module(record_contract496_construction).
-export([run/0,run_stacks/0,probe/1]).
-record #control{required,i=0,a=ok,l=[],t={x,2},m=#{a=>1},b=undefined}.
-record #public{v=7}.
-export_record([public]).
run()->ordsets_core_support:run(?MODULE,cases()).
run_stacks()->ordsets_core_support:profiles(?MODULE,cases()).
cases()->[defaults,definition,exported,public_definition,predicates,alias_constructor].
probe(defaults)->#control{required=42};
probe(definition)->records:get_definition(?MODULE,control);
probe(exported)->#public{};
probe(public_definition)->records:get_definition(?MODULE,public);
probe(predicates)->R=#control{required=42},{erlang:apply(erlang,opaque(is_record),[R]),erlang:apply(erlang,opaque(is_record),[R,control]),erlang:apply(erlang,opaque(is_record),[R,?MODULE,control])};
probe(alias_constructor)->V=opaque({input,42}),R=#control{required=V},{R,V}.
opaque(V)->put(record_contract496_input,V),erase(record_contract496_input).
