namespace Erlang.Differential;

public static class RuntimeNodeQueryCases
{
    public static string? FixtureName(string function, int arity) => function switch
    {
        RuntimeNodeQueryNames.IsAlive when arity == 0 => "erlang-node-query-alive",
        RuntimeNodeQueryNames.Nodes when arity == 0 => "erlang-node-query-default",
        RuntimeNodeQueryNames.Nodes when arity == 1 => "erlang-node-query-this",
        RuntimeNodeQueryNames.Nodes when arity == 2 => "erlang-node-query-this-info",
        _ => null
    };

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body) => cases.Add(new(
            "erlang-node-query-" + name,
            "-module(node_query_contract). -export([run/0]). run()->" + body + ",done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));
        Add("alive", "false=erlang:is_alive()");
        Add("default", "[]=erlang:nodes()");
        Add("this", "N=node(),[N]=erlang:nodes(this)");
        Add(
            "this-info",
            "N=node(),[{N,#{connection_id:=undefined,node_type:=this}}]=erlang:nodes(this,#{connection_id=>true,node_type=>true})"
        );
        Add("empty-types", "[]=erlang:nodes([]),[]=erlang:nodes([],#{node_type=>true})");
        Add(
            "empty-info",
            "N=node(),[{N,#{}}]=erlang:nodes(this,#{}),[{N,#{}}]=erlang:nodes(this,#{node_type=>false,connection_id=>false})"
        );
        Add("union", "N=node(),[N]=erlang:nodes([this,this,visible,hidden,connected])");
        Add("known-local", "true=lists:member(node(),erlang:nodes(known))");
        foreach (string type in new[] { "visible", "hidden", "connected" })
            Add("none-" + type, "[]=erlang:nodes(" + type + "),[]=erlang:nodes(" + type + ",#{node_type=>true,connection_id=>true})");
        foreach (string value in new[] { "wrong", "42", "{}", "#{}", "[this|tail]", "[this,42]", "[wrong]", "<<>>" })
            Add("bad-type-" + cases.Count, "try erlang:nodes(" + value + ") of _->error(accepted) catch error:badarg->ok end");
        foreach (string options in new[] { "[]", "wrong", "42", "#{unknown=>true}", "#{node_type=>wrong}", "#{connection_id=>1}", "#{1=>true}", "#{connection_id=>true,other=>false}" })
            Add("bad-info-" + cases.Count, "try erlang:nodes([]," + options + ") of _->error(accepted) catch error:badarg->ok end");
        Add(
            "error-frame",
            "try erlang:nodes(this,#{node_type=>wrong}) of _->error(accepted) catch error:badarg:S->[{erlang,nodes,[this,#{node_type:=wrong}],L}|_]=S,{error_info,#{module:=erl_erts_errors}}=lists:keyfind(error_info,1,L) end"
        );
        const string foreignPid = "P=binary_to_term(<<131,88,119,12,\"foreign@node\",0:32,0:32,1:32>>),N=node(P),put(keep_node,P),";
        Add("foreign-pid", foreignPid + "true=lists:member(N,erlang:nodes(known)),false=lists:member(N,erlang:nodes(connected))");
        Add(
            "foreign-info",
            foreignPid + "{N,#{node_type:=known,connection_id:=undefined}}=lists:keyfind(N,1,erlang:nodes(known,#{node_type=>true,connection_id=>true}))"
        );
        Add(
            "known-distinct",
            foreignPid + "_=binary_to_term(term_to_binary({P,P,P})),Known=erlang:nodes(known),1=length([X||X<-Known,X=:=N])"
        );
        Add(
            "foreign-port",
            "P=binary_to_term(<<131,120,119,10,\"port@other\",1:64,1:32>>),put(keep_port,P),true=lists:member(node(P),erlang:nodes(known))"
        );
        Add(
            "foreign-reference",
            "R=binary_to_term(<<131,90,0,1,119,9,\"ref@other\",1:32,1:32>>),put(keep_ref,R),true=lists:member(node(R),erlang:nodes(known))"
        );

        return cases;
    }
}
