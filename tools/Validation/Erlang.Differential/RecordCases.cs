namespace Erlang.Differential;

public static class RecordCases
{
    private static CompiledModuleCase Case(string name, string body, string defaults = "a=first,b=second,c") => new(
        "records-" + name,
        "-module(oracle_records). -export([run/0]). -record(state,{" + defaults + "}). run()->" + body + ",done.",
        Term.Tuple(Term.A("ok"), Term.A("done"))
    );

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } =
    [
        Case("defaults", "{state,first,second,undefined}=#state{}"),
        Case("construction", "{state,new,second,value}=#state{c=value,a=new}"),
        Case("access", "R=#state{a=42},42=R#state.a,second=R#state.b"),
        Case("index", "2=#state.a,3=#state.b,4=#state.c"),
        Case("update", "R=#state{},S=R#state{b=new,c=42},first=S#state.a,new=S#state.b,42=S#state.c,second=R#state.b"),
        Case("empty-update", "R=#state{},R=R#state{}"),
        Case("pattern-unspecified-wildcard", "#state{a=A}=#state{a=42,b=nondefault,c=other},42=A"),
        Case("pattern-tag", "other=case {other,first,second,undefined} of #state{}->unexpected;_->other end"),
        Case("pattern-arity", "other=case {state,first} of #state{}->unexpected;_->other end"),
        Case("wildcard-construction", "{state,new,same,same}=#state{_=same,a=new}"),
        Case("wildcard-pattern", "R=#state{a=one,b=two,c=two},#state{a=one,_=X}=R,two=X"),
        Case("record-info", "[a,b,c]=record_info(fields,state),4=record_info(size,state)"),
        Case("badrecord-access", "R={other,first,second,undefined},try R#state.a of _->unexpected catch error:{badrecord,R}->ok end"),
        Case("badrecord-size", "R={state,first},try R#state.a of _->unexpected catch error:{badrecord,R}->ok end"),
        Case("badrecord-update", "R=wrong,try R#state{a=new} of _->unexpected catch error:{badrecord,wrong}->ok end"),
        Case("badrecord-empty-update", "R=wrong,try R#state{} of _->unexpected catch error:{badrecord,wrong}->ok end"),
        Case("guard-valid", "R=#state{a=42},yes=case R of X when X#state.a=:=42->yes;_->no end"),
        Case("guard-invalid", "no=case {other,42,second,undefined} of X when X#state.a=:=42->yes;_->no end"),
        Case("guard-construct", "yes=case #state{} of X when X=:=#state{}->yes;_->no end"),
        new(
            "records-guard-orelse-invalid-left",
            "-module(oracle_records). -export([run/0]). -record(state,{a=first,b=second,c}). run()->yes=case wrong of X when X#state.a=:=42 orelse true->yes;_->no end,done.",
            Term.Tuple(Term.A("error"), Term.A("error"), Term.Tuple(Term.A("badmatch"), Term.A("no")))
        ),
        Case("guard-orelse-short-circuit", "yes=case wrong of X when true orelse X#state.a=:=42->yes;_->no end"),
        Case(
            "construction-field-order",
            "put(order,[]),#state{b=begin put(order,[b|get(order)]),two end,a=begin put(order,[a|get(order)]),one end},[b,a]=get(order)"
        ),
        Case(
            "default-field-order",
            "put(order,[]),{state,first,second,undefined}=#state{},[b,a]=get(order)",
            "a=begin put(order,[a|get(order)]),first end,b=begin put(order,[b|get(order)]),second end,c"
        ),
        Case(
            "update-field-before-base",
            "put(order,[]),(begin put(order,[base|get(order)]),#state{} end)#state{b=begin put(order,[b|get(order)]),two end,a=begin put(order,[a|get(order)]),one end},[base,a,b]=get(order)"
        ),
        Case(
            "update-before-badrecord",
            "put(called,false),try wrong#state{a=begin put(called,true),one end} of _->unexpected catch error:{badrecord,wrong}->ok end,true=get(called)"
        ),
        Case("access-base-once", "put(counter,0),first=(begin put(counter,get(counter)+1),#state{} end)#state.a,1=get(counter)"),
        Case("nested", "R=#state{a=#state{a=inner}},inner=(R#state.a)#state.a"),
        Case("pattern-comprehension", "[1,2]=[V || #state{a=V} <- [#state{a=1},wrong,#state{a=2}]]"),
        Case("quoted-field", "'value'=(#state{a='value'})#state.'a'"),
        Case("empty-record", "{state}=#state{},1=record_info(size,state),[]=record_info(fields,state)", "")
    ];
}
