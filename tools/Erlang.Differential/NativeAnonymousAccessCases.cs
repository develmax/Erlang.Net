namespace Erlang.Differential;

public static class NativeAnonymousAccessCases
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
            "erlang-native-record-access-" + name,
            "-module(native_record_access_contract). -export([run/0]). " + declarations + " run()->" + body + ",done. " + helpers,
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));
        Add(
            "anonymous-foreign-private",
            "",
            "R=records:create(other,r,[{x,1}],#{is_exported=>false}),1=R#_.x,false=records:is_exported(R),yes=empty(R),no=field(R)",
            "empty(#_{})->yes;empty(_)->no. field(#_{x=_})->yes;field(_)->no."
        );
        Add(
            "anonymous-missing-field-actual-identity",
            "-record #r{x=9}.",
            "R=records:create(other,s,[{x,1}],#{is_exported=>false}),{'EXIT',{{badfield,{{other,s},y}},_}}=(catch R#_.y)"
        );
        Add(
            "anonymous-non-record-types",
            "",
            "[ok,ok,ok,ok,ok]=[begin {'EXIT',{{badrecord,V},_}}=(catch V#_.x),ok end || V <- [42,{},[],#{},fun()->ok end]]"
        );
        Add(
            "anonymous-local-private-update-and-pattern",
            "-record #r{x=1}.",
            "R=#r{},1=R#_.x,U=R#_{x=2},2=U#_.x,#_{x=2}=U,false=records:is_exported(U),{'EXIT',{{badrecord,R},_}}=(catch R#native_record_access_contract:r.x)"
        );
        Add(
            "anonymous-qualified-identity-and-export",
            "",
            "R=records:create(other,r,[{x,1}],#{is_exported=>true}),1=R#_.x,1=R#other:r.x,{'EXIT',{{badrecord,R},_}}=(catch R#other:s.x),{'EXIT',{{badrecord,R},_}}=(catch R#third:r.x),{'EXIT',{{badfield,{{other,r},y}},_}}=(catch R#other:r.y)"
        );
        Add(
            "anonymous-read-keeps-update-permission",
            "",
            "R=records:create(other,r,[{x,1}],#{is_exported=>false}),1=R#_.x,{'EXIT',{{badrecord,R},_}}=(catch R#_{x=2}),{'EXIT',{{badrecord,R},_}}=(catch R#_{}),{'EXIT',{{badrecord,R},_}}=(catch R#other:r.x)"
        );
        Add(
            "local-name-foreign-private-access-only",
            "-record #r{x=9}.",
            "R=records:create(other,r,[{x,1}],#{is_exported=>false}),1=R#r.x,1=R#_.x,no=pick(R),{'EXIT',{{badrecord,R},_}}=(catch R#r{x=2}),{'EXIT',{{badrecord,R},_}}=(catch R#other:r.x)",
            "pick(#r{x=_})->yes;pick(_)->no."
        );
        Add(
            "local-name-mismatch-and-actual-field-identity",
            "-record #r{x=9}.",
            "R=records:create(other,s,[{x,1}],#{is_exported=>false}),{'EXIT',{{badrecord,R},_}}=(catch R#r.x),S=records:create(other,r,[{y,2}],#{is_exported=>false}),2=S#r.y,{'EXIT',{{badfield,{{other,r},x}},_}}=(catch S#r.x)"
        );

        return cases;
    }
}
