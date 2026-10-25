namespace Erlang.Differential;

public static class NativeRecordCompletionCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(
            string name,
            string fields,
            string body,
            bool parentheses = false
        ) => cases.Add(new(
            "erlang-native-record-completion-" + name,
            "-module(native_record_completion_contract). -export([run/0]). -record " + (parentheses ? "(" : "") + "#r{" + fields + "}" + (parentheses ? ")" : "") + ". run()->" + body + ",done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));
        Add("typed-required", "x::integer(),y=0::integer(),z=[]", "R=#r{x=wrong_type},wrong_type=R#r.x,0=R#r.y,[]=R#r.z");
        Add(
            "typed-export-default",
            "x=not_a_number::integer()",
            "R=#r{},not_a_number=R#r.x,{#{is_exported:=false},[{x,not_a_number}]}=records:get_definition(native_record_completion_contract,r)"
        );
        Add("typed-union-range", "x=3::0..255|undefined", "R=#r{},3=R#r.x");
        Add(
            "typed-containers",
            "x=[]::[integer(),...],y=#{}::#{atom()=>integer()},z={}::{atom(),integer()}",
            "R=#r{},[]=R#r.x,#{}=R#r.y,{}=R#r.z"
        );
        Add(
            "typed-remote-fun",
            "x=undefined::other:opaque(integer()),y=undefined::fun((integer(),atom())->boolean()),z=undefined::fun((...)->term())",
            "R=#r{},undefined=R#r.x,undefined=R#r.y,undefined=R#r.z"
        );
        Add("typed-binary", "x=undefined::<<_:8,_:_*16>>,y=undefined::<<>>", "R=#r{},undefined=R#r.x,undefined=R#r.y");
        Add(
            "parenthesized",
            "x=4::integer()",
            "R=#r{},4=R#r.x",
            true
        );
        Add(
            "boolean-operators",
            "x=true and false,y=true or false,z=true xor true,n=not false",
            "R=#r{},false=R#r.x,true=R#r.y,false=R#r.z,true=R#r.n"
        );
        Add(
            "comparison-operators",
            "x=1==1.0,y=1=:=1.0,z={1,[2]}<{1,[3]},n=[]=/=atom",
            "R=#r{},true=R#r.x,false=R#r.y,true=R#r.z,true=R#r.n"
        );
        Add("bitwise-operators", "x=(bnot 1) band 15,y=1 bsl 8,z=16 bsr 2,n=7 bxor 3", "R=#r{},14=R#r.x,256=R#r.y,4=R#r.z,4=R#r.n");
        Add("scalar-append", "x=[]++tail,y=([1]++[2])=:=[1,2],z=([1,2]--[1])=:=[2]", "R=#r{},tail=R#r.x,true=R#r.y,true=R#r.z");
        Add("nested-scalar-fold", "x={1<2,[1+2,not false],#{(1 bsl 3)=>(2=:=2)}}", "R=#r{},{true,[3,true],#{8:=true}}=R#r.x");
        Add("string-binary", "x= <<\"abc\">>,y=\"abc\"", "R=#r{},<<97,98,99>>=R#r.x,[97,98,99]=R#r.y");

        return cases;
    }
}
