-module(queue_core_representation).
-export([run/0]).
run()->{[a],[]}=queue:from_list([a]), {[b],[a]}=queue:from_list([a,b]), {[c,b],[a]}=queue:from_list([a,b,c]), {[d,c],[a,b]}=queue:from_list([a,b,c,d]), {[e,d,c],[a,b]}=queue:from_list([a,b,c,d,e]), done.