namespace Erlang.Differential;

public static class ReferenceTextCases
{
    public static string? FixtureName(string function, int arity) => (function, arity) switch
    {
        (ReferenceTextContractNames.ToList, 1) => ReferenceTextContractNames.Roundtrip,
        (ReferenceTextContractNames.FromList, 1) => ReferenceTextContractNames.Synthetic,
        _ => null
    };

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body) => cases.Add(new(
            "erlang-reference-text-" + name,
            "-module(reference_text_contract). -export([run/0]). run()->" + body + ",done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));

        Add("roundtrip", "R=make_ref(),S=erlang:ref_to_list(R),true=is_list(S),R=erlang:list_to_ref(S)");
        Add("synthetic", "S=\"#Ref<0.3.0.1>\",R=erlang:list_to_ref(S),true=is_reference(R),S=erlang:ref_to_list(R)");
        Add(
            "alias",
            "A=alias(),S=erlang:ref_to_list(A),A=erlang:list_to_ref(S),A!kept,receive kept->ok after 1000->error(missing) end,true=unalias(A)"
        );
        Add("deactivated", "A=alias(),S=erlang:ref_to_list(A),true=unalias(A),A=erlang:list_to_ref(S),false=unalias(A)");
        Add(
            "dead-owner",
            "Parent=self(),{P,M}=spawn_monitor(fun()->A=alias(),Parent!{self(),A,erlang:ref_to_list(A)} end),receive {P,A,S}->receive {'DOWN',M,process,P,normal}->ok after 1000->error(missing) end,A=erlang:list_to_ref(S) after 1000->error(missing) end"
        );
        Add("normalizes-five", "\"#Ref<0.3.0.1>\"=erlang:ref_to_list(erlang:list_to_ref(\"#Ref<0.55.44.3.0.1>\"))");
        Add("leading-zeroes", "\"#Ref<0.3.0.1>\"=erlang:ref_to_list(erlang:list_to_ref(\"#Ref<000.003.000.001>\"))");
        Add(
            "unsigned-wrap-channel",
            "\"#Ref<0.3.0.1>\"=erlang:ref_to_list(erlang:list_to_ref(\"#Ref<18446744073709551616.3.0.1>\"))"
        );
        Add("unsigned-wrap-word", "\"#Ref<0.3.0.1>\"=erlang:ref_to_list(erlang:list_to_ref(\"#Ref<0.18446744073709551619.0.1>\"))");
        Add("nul", "\"#Ref<0.3.0.1>\"=erlang:ref_to_list(erlang:list_to_ref(\"#Ref<0.3.0.1>\"++[0,255,0,120]))");
        int index = 0;
        foreach (string text in new[] { "#Ref<0.0.0.0>", "#Ref<0.4294967295.4294705152.262143>", "#Ref<0.0.1.1>", "#Ref<0.0.65536.1>", "#Ref<0.0.131072.1>", "#Ref<0.0.3.0.65536.1>", "#Ref<0.1.3.0.65536.1>", "#Ref<0.0.3.0.196608.1>" })
            Add("accepted-" + index++, "S=\"" + text + "\",S=erlang:ref_to_list(erlang:list_to_ref(S))");
        string padded = "#Ref<" + new string('0', 59) + ".0.0.1>";
        Add("maximum-buffer", "\"#Ref<0.0.0.1>\"=erlang:ref_to_list(erlang:list_to_ref(\"" + padded + "\"))");
        index = 0;
        foreach (string text in new[] { "", "#Ref<>", "#ref<0.0.0.1>", "Ref<0.0.0.1>", "#Ref<0>", "#Ref<0.1>", "#Ref<0.1.2>", "#Ref<0.1.2.3.4>", "#Ref<0.1.2.3.4.5.6>", "#Ref<1.0.0.1>", "#Ref<4294967296.0.0.1>", "#Ref<0.4294967296.0.1>", "#Ref<0.0.0.262144>", "#Ref<0.0.65535.1>", "#Ref<0.0.2.0.65536.1>", "#Ref<0.0.0.0.65536.1>", "#Ref<0.-1.0.1>", "#Ref<-0.0.0.1>", "#Ref<+0.0.0.1>", "#Ref< 0.0.0.1>", "#Ref<0..0.1>", "#Ref<0.0.0.1>>", "#Ref<0.0.0.1> ", "#Ref<0.0.0.1", "#Ref<0.0.0.1.>" })
        {
            Add("bad-text-" + index++, "ok=try erlang:list_to_ref(\"" + text + "\") of _->unexpected catch error:badarg->ok end");
        }
        Add(
            "too-long",
            "ok=try erlang:list_to_ref(\"#Ref<" + new string('0', 60) + ".0.0.1>\") of _->unexpected catch error:badarg->ok end"
        );
        index = 0;
        foreach (string value in new[] { "none", "0", "1.0", "<<\"#Ref<0.0.0.1>\">>", "{}", "#{a=>1}", "self()", "make_ref()", "fun()->ok end", "[256]", "[-1]", "[1.0]", "[$#|none]", "[[35]]", "\"#Ref<0.0.0.1>\"++[0,256]", "\"#Ref<0.0.0.1>\"++[0|none]" })
            Add("bad-list-" + index++, "ok=try erlang:list_to_ref(" + value + ") of _->unexpected catch error:badarg->ok end");
        index = 0;
        foreach (string value in new[] { "none", "0", "1.0", "[]", "[1]", "<<>>", "{}", "#{a=>1}", "self()", "fun()->ok end" })
            Add("bad-reference-" + index++, "ok=try erlang:ref_to_list(" + value + ") of _->unexpected catch error:badarg->ok end");

        return cases;
    }
}
