namespace Erlang.Differential;

public static class TermOrderCaseSources
{
    public const string DeepFunEnvironment = """
-module(order_deep_fun_environment).
-export([run/0]).
run()->A=build(4096,0),B=build(4096,0),C=build(4096,1),true=(A=:=B),true=(A==B),true=(A<C),true=(C>A),done.
build(0,X)->X;
build(N,X)->build(N-1,fun()->X end).
""";

    public const string DeepListHead = """
-module(order_deep_list_head).
-export([run/0]).
run()->A=build(4096,0),B=build(4096,0),C=build(4096,1),true=(A=:=B),true=(A==B),true=(A<C),true=(C>A),done.
build(0,X)->X;
build(N,X)->build(N-1,[X]).
""";

    public const string MapKeyPhases = """
-module(order_map_key_phases).
-export([run/0]).
run()->true=(#{1=>9,2=>0}<#{1=>0,3=>0}),true=(#{1=>1}==#{1=>1.0}),false=(#{1=>1}=:=#{1=>1.0}),false=(#{1=>a}==#{1.0=>a}),done.
""";

    public const string DeepMapValue = """
-module(order_deep_map_value).
-export([run/0]).
run()->A=build(4096,0),B=build(4096,0),C=build(4096,1),true=(A=:=B),true=(A==B),true=(A<C),true=(C>A),done.
build(0,X)->X;
build(N,X)->build(N-1,#{key=>X}).
""";

    public const string DeepTuple = """
-module(order_deep_tuple).
-export([run/0]).
run()->A=build(4096,0),B=build(4096,0),C=build(4096,1),true=(A=:=B),true=(A==B),true=(A<C),true=(C>A),done.
build(0,X)->X;
build(N,X)->build(N-1,{X}).
""";

}
