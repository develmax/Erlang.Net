namespace Erlang.Differential;

public static class NativeRecordSyntaxCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(
            string name,
            string declarations,
            string body,
            string helpers = ""
        ) => cases.Add(new(
            "erlang-native-record-syntax-" + name,
            "-module(native_record_syntax_contract). -export([run/0]). " + declarations + " run()->" + body + ",done. " + helpers,
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));
        Add(
            "local-create",
            "-record #r{x,y=2,z=3}.",
            "R=#r{x=1},true=is_record(R),true=is_record(R,r),native_record_syntax_contract=records:get_module(R),[x,y,z]=records:get_field_names(R),2=R#r.y,false=records:is_exported(R)"
        );
        Add(
            "literal-defaults",
            "-record #r{x=1+2,y={a,[1,2]},z=#{a=>42},u=undefined}.",
            "R=#r{},3=R#r.x,{a,[1,2]}=R#r.y,#{a:=42}=R#r.z,undefined=R#r.u"
        );
        Add(
            "names",
            "-record #State{x=1}. -record #div{}.",
            "S=#State{},1=S#State.x,D=#div{},'State'=records:get_name(S),'div'=records:get_name(D)"
        );
        Add("local-update", "-record #r{x=1,y=2}.", "R=#r{},U=R#r{x=10},1=R#r.x,10=U#r.x,2=U#r.y,R=R#r{}");
        Add(
            "pattern-binding",
            "-record #r{x,y=2}.",
            "R=#r{x=7},#r{x=X,y=2}=R,7=X,7=pick(R)",
            "pick(#r{x=X})->X."
        );
        Add(
            "pattern-captured-fields",
            "-record #r{}.",
            "R=records:create(native_record_syntax_contract,r,[{x,42}],#{is_exported=>false}),42=pick(R)",
            "pick(#r{x=X})->X."
        );
        Add(
            "guard-access",
            "-record #r{x=1}.",
            "yes=check(#r{}),no=check({r,1}),R=records:create(native_record_syntax_contract,r,[],#{is_exported=>false}),no=check(R)",
            "check(R) when R#r.x=:=1->yes;check(_)->no."
        );
        Add(
            "local-predicate",
            "-record #r{}.",
            "true=check(#r{}),false=check(records:create(other,r,[],#{is_exported=>false}))",
            "check(R) when is_record(R,r)->true;check(_)->false."
        );
        Add(
            "definition-private",
            "-record #r{x,y=undefined,z=1+2}.",
            "{#{is_exported:=false},[x,{y,undefined},{z,3}]}=records:get_definition(native_record_syntax_contract,r)"
        );
        Add(
            "definition-exported",
            "-record #r{x=1}. -export_record([r]).",
            "{#{is_exported:=true},[{x,1}]}=records:get_definition(native_record_syntax_contract,r),true=records:is_exported(#r{})"
        );
        Add(
            "external-self-create",
            "-record #r{x=1,y=2}. -export_record([r]).",
            "R=#native_record_syntax_contract:r{y=10},1=R#native_record_syntax_contract:r.x,10=R#native_record_syntax_contract:r.y"
        );
        Add(
            "external-private-create",
            "-record #r{x=1}.",
            "{'EXIT',{{badrecord,{native_record_syntax_contract,r}},_}}=(catch #native_record_syntax_contract:r{})"
        );
        Add(
            "external-create-missing",
            "-record #r{x,y=2}. -export_record([r]).",
            "{'EXIT',{{novalue,{{native_record_syntax_contract,r},x}},_}}=(catch #native_record_syntax_contract:r{})"
        );
        Add(
            "external-create-error-order",
            "-record #r{x}. -export_record([r]).",
            "{'EXIT',{{badfield,{{native_record_syntax_contract,r},z}},_}}=(catch #native_record_syntax_contract:r{z=3})"
        );
        Add(
            "external-exported-access-update",
            "",
            "R=records:create(other,r,[{x,1}],#{is_exported=>true}),1=R#other:r.x,U=R#other:r{x=2},2=U#other:r.x"
        );
        Add(
            "external-private-permissions",
            "",
            "R=records:create(other,r,[{x,1}],#{is_exported=>false}),{'EXIT',{{badrecord,R},_}}=(catch R#other:r.x),{'EXIT',{{badrecord,R},_}}=(catch R#other:r{x=2}),{'EXIT',{{badrecord,R},_}}=(catch R#other:r{})"
        );
        Add(
            "external-empty-match-private",
            "",
            "R=records:create(other,r,[{x,1}],#{is_exported=>false}),yes=empty(R),no=field(R)",
            "empty(#other:r{})->yes;empty(_)->no. field(#other:r{x=_})->yes;field(_)->no."
        );
        Add("anonymous-local-private", "-record #r{x=1}.", "R=#r{},1=R#_.x,U=R#_{x=2},2=U#_.x,#_{x=2}=U");
        Add("anonymous-foreign", "", "R=records:create(other,r,[{x,1}],#{is_exported=>true}),1=R#_.x,U=R#_{x=2},2=U#_.x,#_{x=2}=U");
        Add(
            "anonymous-private-match",
            "",
            "R=records:create(other,r,[{x,1}],#{is_exported=>false}),yes=empty(R),no=field(R),{'EXIT',{{badrecord,R},_}}=(catch R#_.x)",
            "empty(#_{})->yes;empty(_)->no. field(#_{x=_})->yes;field(_)->no."
        );
        Add(
            "definition-errors",
            "",
            "{'EXIT',{badarg,_}}=(catch records:get_definition(other,r)),{'EXIT',{badarg,_}}=(catch records:get_definition(42,r)),{'EXIT',{badarg,_}}=(catch records:get_definition(other,42))"
        );
        Add(
            "captured-layout",
            "-record #r{x=1}.",
            "R=records:create(native_record_syntax_contract,r,[{z,42}],#{is_exported=>false}),42=R#r.z,{'EXIT',{{badfield,{{native_record_syntax_contract,r},x}},_}}=(catch R#r.x),U=R#r{z=43},43=U#r.z"
        );
        Add(
            "source-order-create",
            "-record #r{x,y}.",
            "R=#r{y=begin self()!second,2 end,x=begin self()!first,1 end},2=R#r.y,receive second->ok after 0->error(order) end,receive first->ok after 0->error(order) end"
        );
        Add("closure-pattern", "-record #r{x=1}.", "X=7,F=fun(#r{x=V})->{X,V} end,{7,1}=F(#r{})");
        Add("comprehension-pattern", "-record #r{x=1}.", "[1,2]=[X||#r{x=X}<-[#r{},#r{x=2},other]]");

        Add(
            "imported-access",
            "-import_record(other,[r]).",
            "R=records:create(other,r,[{x,1}],#{is_exported=>true}),1=R#r.x,U=R#r{x=2},2=U#r.x,true=is_record(R,r),no=pick(records:create(other,r,[{x,1}],#{is_exported=>false}))",
            "pick(#r{x=_})->yes;pick(_)->no."
        );
        Add(
            "update-evaluation-order",
            "-record #r{x=1}.",
            "R=(begin self()!base,#r{} end)#r{x=begin self()!field,2 end},2=R#r.x,receive base->ok after 0->error(order) end,receive field->ok after 0->error(order) end"
        );
        Add("default-signed-zero", "-record #r{x=-0.0}.", "R=#r{},true=(R#r.x=:=-0.0),false=(R#r.x=:=0.0)");

        Add("quoted-underscore-name", "-record #'_'{x=1}.", "R=#'_'{},1=R#'_'.x,'_'=records:get_name(R)");

        return cases;
    }
}
