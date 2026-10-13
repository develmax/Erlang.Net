namespace Erlang.Differential;

public static class MatchSpecCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    public static string? FixtureName(string function, int arity) => function == MatchSpecNames.Test && arity == 3 ? "erlang-match-spec-table-basic" : null;

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body) => cases.Add(new(
            "erlang-match-spec-" + name,
            "-module(match_spec_contract). -export([run/0]). run()->" + body + ",done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));
        Add(
            "table-basic",
            "{ok,42,[],[]}=erlang:match_spec_test({item,21},[{{item,'$1'},[{is_integer,'$1'}],[{'*','$1',2}]}],table)"
        );
        Add("table-no-match", "{ok,false,[],[]}=erlang:match_spec_test({item,21},[{{other,'$1'},[],['$1']}],table)");
        Add(
            "table-repeated",
            "{ok,hit,[],[]}=erlang:match_spec_test({7,7},[{{'$1','$1'},[],[hit]}],table),{ok,false,[],[]}=erlang:match_spec_test({7,7.0},[{{'$1','$1'},[],[hit]}],table)"
        );
        Add(
            "table-fallback",
            "{ok,second,[],[]}=erlang:match_spec_test({a,2},[{{a,'$1'},[{'>','$1',5}],[first]},{{a,'$2'},[],[second]}],table)"
        );
        Add("table-map-subset", "{ok,8,[],[]}=erlang:match_spec_test(#{k=>8,extra=>9},[{#{k=>'$1'},[],['$1']}],table)");
        Add("table-list-tail", "{ok,[2,3],[],[]}=erlang:match_spec_test([1,2,3],[{[1|'$1'],[],['$1']}],table)");
        Add(
            "table-sparse-variables",
            "{ok,[first,last],[],[]}=erlang:match_spec_test({last,first},[{{'$1000000','$0'},[],['$$']}],table)"
        );
        Add("table-leading-zero-literal", "{ok,'$01',[],[]}=erlang:match_spec_test({'$01'},[{{'$01'},[],['$01']}],table)");
        Add("table-whole", "{ok,{a,1},[],[]}=erlang:match_spec_test({a,1},[{'_',[],['$_']}],table)");
        Add(
            "table-construct",
            "{ok,{value,7,[7|tail]},[],[]}=erlang:match_spec_test({7},[{{'$1'},[],[{{value,'$1',['$1'|tail]}}]}],table)"
        );
        Add("table-constant", "{ok,{'$1',[]},[],[]}=erlang:match_spec_test(any,[{'_',[],[{const,{'$1',[]}}]}],table)");
        Add(
            "table-empty-body",
            "{error,[{error,\"Body clause does not return anything.\"}]}=erlang:match_spec_test(any,[{'_',[],[]}],table)"
        );
        Add("table-body-exit", "{ok,'EXIT',[],[]}=erlang:match_spec_test(any,[{'_',[],[{'div',1,0}]}],table)");
        Add("table-body-recover", "{ok,kept,[],[]}=erlang:match_spec_test(any,[{'_',[],[{'div',1,0},kept]}],table)");
        Add(
            "table-guard-error",
            "{ok,fallback,[],[]}=erlang:match_spec_test(any,[{'_',[{'div',1,0}],[bad]},{'_',[],[fallback]}],table)"
        );
        Add("table-guard-nonbool", "{ok,false,[],[]}=erlang:match_spec_test(any,[{'_',[1],[bad]}],table)");
        Add(
            "table-short-circuit",
            "{ok,kept,[],[]}=erlang:match_spec_test(any,[{'_',[],[{'andalso',false,{'div',1,0}},{'orelse',true,{'div',1,0}},kept]}],table)"
        );
        Add(
            "table-nary-booleans",
            "{ok,{true,false,42},[],[]}=erlang:match_spec_test(any,[{'_',[],[{{{'and',true,true,true},{'or',false,false,false},{'andalso',true,true,42}}}]}],table)"
        );
        Add("table-self", "{ok,P,[],[]}=erlang:match_spec_test(any,[{'_',[],[{self}]}],table),P=self()");
        Add("table-map-expression", "{ok,#{value:=7},[],[]}=erlang:match_spec_test({7},[{{'$1'},[],[#{value=>'$1'}]}],table)");
        Add("trace-default", "{ok,true,[],[]}=erlang:match_spec_test([7],[{['$1'],[],[ignored]}],trace)");
        Add("trace-message", "{ok,{value,7},[],[]}=erlang:match_spec_test([7],[{['$1'],[],[{message,{{value,'$1'}}}]}],trace)");
        Add("trace-message-false", "{ok,false,[],[]}=erlang:match_spec_test([7],[{['_'],[],[{message,false}]}],trace)");
        Add(
            "trace-flags",
            "{ok,true,[return_trace,exception_trace],[]}=erlang:match_spec_test([7],[{['_'],[],[{exception_trace},{return_trace}]}],trace)"
        );
        Add("trace-no-match", "{ok,false,[],[]}=erlang:match_spec_test([7],[{[8],[],[{return_trace}]}],trace)");
        Add("trace-whole-variable", "{ok,[7,8],[],[]}=erlang:match_spec_test([7,8],[{'$2',[],[{message,'$2'}]}],trace)");
        Add("trace-prefix", "{ok,7,[],[]}=erlang:match_spec_test([7,8],[{['$1'|'_'],[],[{message,'$1'}]}],trace)");
        Add("trace-tuple-head", "{ok,7,[],[]}=erlang:match_spec_test([7],[{{'$1'},[],[{message,'$1'}]}],trace)");
        Add("trace-improper-subject", "{ok,[7],[],[]}=erlang:match_spec_test([7|ignored],[{'_',[],[{message,'$_'}]}],trace)");
        Add("trace-empty-subject", "{ok,true,[],[]}=erlang:match_spec_test([],[{[],[],[]}],trace)");
        Add("trace-caller", "{ok,undefined,[],[]}=erlang:match_spec_test([],[{'_',[],[{message,{caller}}]}],trace)");
        Add("bad-type", "{'EXIT',{badarg,_}}=catch erlang:match_spec_test(any,[],other)");
        Add("bad-trace-subject", "{'EXIT',{badarg,_}}=catch erlang:match_spec_test({},[],trace)");
        Add("empty-spec", "{error,[{error,\"Match programs are not in a list.\"}]}=erlang:match_spec_test(any,[],table)");
        Add(
            "bad-clause",
            "{error,[{error,\"Match program part is not a tuple of arity 3.\"}]}=erlang:match_spec_test(any,[bad],table)"
        );
        Add("unbound", "{error,[{error,\"Variable $2 is unbound.\"}]}=erlang:match_spec_test(any,[{'_',[],['$2']}],table)");
        Add(
            "unknown-operation",
            "{error,[{error,\"Function unknown_operation/1 does_not_exist.\"}]}=erlang:match_spec_test(any,[{'_',[],[{unknown_operation,1}]}],table)"
        );
        Add(
            "wrong-dialect",
            "{error,[{error,\"Special form 'message' used in wrong dialect.\"}]}=erlang:match_spec_test(any,[{'_',[],[{message,true}]}],table)"
        );
        Add(
            "guard-message",
            "{error,[{error,\"Special form 'message' called in guard context.\"}]}=erlang:match_spec_test([],[{'_',[{message,true}],[]}],trace)"
        );
        Add(
            "variable-map-key",
            "{error,[{error,\"Variable found in map key.\"}]}=erlang:match_spec_test(#{},[{#{'$1'=>a},[],[]}],table)"
        );
        Add(
            "underscore-map-key",
            "{error,[{error,\"Underscore found in map key.\"}]}=erlang:match_spec_test(#{},[{#{'_'=>a},[],[]}],table)"
        );
        Add(
            "invalid-trace-head",
            "{error,[{error,\"Match head is invalid in this context.\"}]}=erlang:match_spec_test([], [{bad,[],[]}],trace)"
        );
        Add(
            "list-action-order",
            "{ok,second,[],[]}=erlang:match_spec_test([],[{'_',[],[[{message,first},{message,second}]]}],trace)"
        );
        Add(
            "tuple-action-order",
            "{ok,first,[],[]}=erlang:match_spec_test([],[{'_',[],[{{{message,first},{message,second}}}]}],trace)"
        );
        Add(
            "map-action-order",
            "{ok,first,[],[]}=erlang:match_spec_test([],[{'_',[],[#{a=>{message,first},b=>{message,second}}]}],trace)"
        );
        Add(
            "unbound-argument-order",
            "{error,[{error,\"Variable $2 is unbound.\"},{error,\"Variable $1 is unbound.\"}]}=erlang:match_spec_test(any,[{'_',[],[{'+','$1','$2'}]}],table)"
        );
        Add(
            "unbound-short-circuit-order",
            "{error,[{error,\"Variable $1 is unbound.\"},{error,\"Variable $2 is unbound.\"}]}=erlang:match_spec_test(any,[{'_',[],[{'andalso','$1','$2'}]}],table)"
        );
        Add(
            "unbound-list-order",
            "{error,[{error,\"Variable $1 is unbound.\"},{error,\"Variable $2 is unbound.\"}]}=erlang:match_spec_test(any,[{'_',[],[['$1','$2']]}],table)"
        );
        Add(
            "table-numeric-equality",
            "{ok,{true,false},[],[]}=erlang:match_spec_test({7},[{{'$1'},[],[{{{'==','$1',7.0},{'=:=','$1',7.0}}}]}],table)"
        );
        Add(
            "table-type-guards",
            "{ok,7,[],[]}=erlang:match_spec_test({7},[{{'$1'},[{is_integer,'$1',0,10},{is_tuple,'$_'}],[{element,1,'$_'}]}],table)"
        );
        Add(
            "table-binary-guards",
            "{ok,<<2>>,[],[]}=erlang:match_spec_test({<<1,2,3>>},[{{'$1'},[{is_binary,'$1'},{'==',{byte_size,'$1'},3}],[{binary_part,'$1',1,1}]}],table)"
        );
        Add(
            "table-map-guards",
            "{ok,7,[],[]}=erlang:match_spec_test(#{k=>7},[{'$1',[{is_map,'$1'},{is_map_key,k,'$1'}],[{map_get,k,'$1'}]}],table)"
        );
        Add("table-improper-list-type", "{ok,true,[],[]}=erlang:match_spec_test([1|tail],[{'$1',[],[{is_list,'$1'}]}],table)");
        Add(
            "table-map-duplicate-keys",
            "{ok,#{k:=second},[],[]}=erlang:match_spec_test({k,k},[{{'$1','$2'},[],[#{'$1'=>first,'$2'=>second}]}],table)"
        );
        Add(
            "table-float-pattern-bits",
            "{ok,false,[],[]}=erlang:match_spec_test({0.0},[{{-0.0},[],[wrong]}],table),{ok,hit,[],[]}=erlang:match_spec_test({-0.0},[{{-0.0},[],[hit]}],table)"
        );

        return cases.AsReadOnly();
    }
}
