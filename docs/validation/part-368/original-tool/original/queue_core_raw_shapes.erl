-module(queue_core_raw_shapes).
-export([run/0]).
run()->{{value,1},{[3],[2]}}=queue:out({[3,2,1],[]}), {{value,1},{[5,4],[2,3]}}=queue:out({[5,4,3,2],[1]}), {{value,1},{[],[2,3]}}=queue:out({[],[1,2,3]}), {{value,1},{[],[]}}=queue:out({[],[1]}), done.