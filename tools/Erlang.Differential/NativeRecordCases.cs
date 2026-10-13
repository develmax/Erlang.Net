namespace Erlang.Differential;

public static class NativeRecordCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body, string helpers = "") => cases.Add(new(
            "erlang-native-record-" + name,
            "-module(native_record_contract). -export([run/0]). run()->" + body + ",done. " + helpers,
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));
        Add(
            "predicate-one",
            "R=records:create(test,a,[{x,1}],#{is_exported=>false}),true=is_record(R),false=is_record({a,1}),false=is_record(#{}),false=is_record(42)"
        );
        Add(
            "predicate-two",
            "R=records:create(test,a,[{x,1}],#{is_exported=>false}),true=apply(erlang,is_record,[R,a]),false=apply(erlang,is_record,[R,test]),true=apply(erlang,is_record,[{a},a])"
        );
        Add(
            "predicate-three",
            "R=records:create(test,a,[{x,1}],#{is_exported=>false}),true=is_record(R,test,a),false=is_record(R,other,a),false=is_record(R,test,b),false=is_record(R,a,2),false=is_record({a},test,a)"
        );
        Add(
            "predicate-guard",
            "R=records:create(test,a,[],#{is_exported=>false}),yes=check(R),no=check({a}),yes=tagged(R),no=tagged({a})",
            "check(R) when is_record(R)->yes;check(_)->no. tagged(R) when is_record(R,test,a)->yes;tagged(_)->no."
        );
        Add(
            "introspection",
            "R=records:create(test,a,[{z,3},{x,1},{y,2}],#{is_exported=>true}),test=records:get_module(R),a=records:get_name(R),[z,x,y]=records:get_field_names(R),true=records:is_exported(R),1=records:get(x,R),false=is_tuple(R),false=is_map(R)"
        );
        Add(
            "empty",
            "R=records:create(test,a,[],#{is_exported=>false}),[]=records:get_field_names(R),false=records:is_exported(R),true=is_record(R)"
        );
        Add(
            "update",
            "R=records:create(test,a,[{z,3},{x,1},{y,2}],#{is_exported=>false}),U=records:update(R,test,a,#{x=>10,y=>20}),1=records:get(x,R),10=records:get(x,U),20=records:get(y,U),3=records:get(z,U),[z,x,y]=records:get_field_names(U),true=(R=:=records:update(R,test,a,#{}))"
        );
        Add(
            "update-wildcard",
            "R=records:create(test,a,[{x,1}],#{is_exported=>false}),U=records:update(R,42,'_',#{x=>2}),2=records:get(x,U)"
        );
        Add(
            "create-error-order",
            "{'EXIT',{{badrecord,other},_}}=(catch records:create(42,other,invalid,invalid)),{'EXIT',{{badmap,invalid},_}}=(catch records:create(test,a,invalid,invalid)),{'EXIT',{badarg,_}}=(catch records:create(test,a,[{42,1}|bad],#{is_exported=>false}))"
        );
        Add(
            "create-badfield",
            "{'EXIT',{{badfield,{{test,a},42}},_}}=(catch records:create(test,a,[{42,1}],#{is_exported=>false}))"
        );
        Add(
            "create-invalid-options",
            "{'EXIT',{badarg,_}}=(catch records:create(test,a,[],#{})),{'EXIT',{badarg,_}}=(catch records:create(test,a,[],#{is_exported=>invalid_atom})),{'EXIT',{badarg,_}}=(catch records:create(test,a,[],#{is_exported=>false,extra=>true}))"
        );
        Add(
            "create-duplicates",
            "{'EXIT',{badarg,_}}=(catch records:create(test,a,[{x,1},{x,2}],#{is_exported=>false})),{'EXIT',{badarg,_}}=(catch records:create(test,a,[x],#{is_exported=>false}))"
        );
        Add(
            "introspection-errors",
            "{'EXIT',{badarg,_}}=(catch records:get(x,{a})),{'EXIT',{badarg,_}}=(catch records:get_module({a})),{'EXIT',{badarg,_}}=(catch records:get_name({a})),{'EXIT',{{badrecord,{a}},_}}=(catch records:get_field_names({a})),{'EXIT',{{badrecord,{a}},_}}=(catch records:is_exported({a}))"
        );
        Add(
            "update-errors",
            "R=records:create(test,a,[{x,1}],#{is_exported=>false}),{'EXIT',{{badrecord,other},_}}=(catch records:update(R,other,other,invalid)),{'EXIT',{{badmap,invalid},_}}=(catch records:update(R,test,a,invalid)),{'EXIT',{{badfield,{{test,a},y}},_}}=(catch records:update(R,test,a,#{y=>2}))"
        );
        Add(
            "get-errors",
            "R=records:create(test,a,[{x,1}],#{is_exported=>false}),{'EXIT',{badarg,_}}=(catch records:get(y,R)),{'EXIT',{badarg,_}}=(catch records:get(42,R))"
        );
        Add(
            "order",
            "R=records:create(test,a,[{z,1},{x,2}],#{is_exported=>false}),S=records:create(test,a,[{x,2},{z,1}],#{is_exported=>false}),false=(R=:=S),true=(S<R),true=({}<R),true=(R<#{}),E=records:create(test,a,[{z,1},{x,2}],#{is_exported=>true}),true=(R<E)"
        );
        Add(
            "numeric-equality",
            "R=records:create(test,a,[{x,1}],#{is_exported=>false}),S=records:create(test,a,[{x,1.0}],#{is_exported=>false}),true=(R==S),false=(R=:=S),M=#{R=>integer,S=>floating},2=map_size(M),integer=maps:get(R,M),floating=maps:get(S,M)"
        );
        Add(
            "etf-roundtrip",
            "R=records:create(test,a,[{z,3},{x,{1,2}},{y,<<1,2>>}],#{is_exported=>false}),B=term_to_binary(R),<<131,67,0,0,0,3,0,_/binary>>=B,R=binary_to_term(B),R=binary_to_term(term_to_binary(R,[compressed])),R=binary_to_term(B,[safe]),Size=byte_size(B),Size=erlang:external_size(R)"
        );
        Add(
            "etf-exact-empty",
            "R=records:create(t,a,[],#{is_exported=>true}),<<131,67,0,0,0,0,1,119,1,116,119,1,97>>=term_to_binary(R)"
        );
        Add("etf-invalid-flags", "{'EXIT',{badarg,_}}=(catch binary_to_term(<<131,67,0,0,0,0,2,119,1,116,119,1,97>>))");
        Add(
            "hashes",
            "R=records:create(test,a,[{z,3},{x,1}],#{is_exported=>false}),S=binary_to_term(term_to_binary(R)),true=(erlang:phash2(R)=:=erlang:phash2(S)),true=(erlang:phash(R,4294967296)=:=erlang:phash(S,4294967296)),true=(erlang:phash2({R})=:=erlang:phash2({S}))"
        );

        Add(
            "hash-single-field-numeric",
            "R=records:create(test,a,[{x,1}],#{is_exported=>false}),43462095=erlang:phash2(R),1654074831=erlang:phash2(R,4294967296),2396089818=erlang:phash(R,4294967296)"
        );
        Add(
            "hash-multi-field-numeric",
            "R=records:create(test,a,[{z,3},{x,{1,2}},{y,<<1,2>>}],#{is_exported=>false}),48652259=erlang:phash2(R),457382667=erlang:phash(R,4294967296)"
        );

        return cases;
    }
}
