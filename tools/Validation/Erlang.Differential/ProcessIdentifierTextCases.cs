namespace Erlang.Differential;

public static class ProcessIdentifierTextCases
{
    public static string? FixtureName(string function, int arity) => arity == 1 ? function switch
    {
        ProcessIdentifierTextNames.PidToList => "erlang-identifier-text-pid-round-trip",
        ProcessIdentifierTextNames.ListToPid => "erlang-identifier-text-pid-local-value",
        ProcessIdentifierTextNames.PortToList => "erlang-identifier-text-port-round-trip",
        ProcessIdentifierTextNames.ListToPort => "erlang-identifier-text-port-local-value",
        _ => null
    } : null;

    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var cases = new List<CompiledModuleCase>();

        void Add(string name, string body) => cases.Add(new(
            "erlang-identifier-text-" + name,
            "-module(identifier_text_contract). -export([run/0]). run()->" + body + ",done.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));

        Add("pid-round-trip", "P=self(),P=list_to_pid(pid_to_list(P))");
        Add("pid-local-value", "P=list_to_pid(\"<0.17.23>\"),true=is_pid(P),\"<0.17.23>\"=pid_to_list(P)");
        Add("pid-maximum-fields", "P=list_to_pid(\"<0.268435455.4294967295>\"),\"<0.268435455.4294967295>\"=pid_to_list(P)");
        Add("pid-leading-zeroes", "P=list_to_pid(\"<000.0017.0023>\"),\"<0.17.23>\"=pid_to_list(P)");
        Add(
            "pid-64-byte-bound",
            "Text=\"<\"++lists:duplicate(57,$0)++\"0.1.0>\",64=length(Text),P=list_to_pid(Text),\"<0.1.0>\"=pid_to_list(P)"
        );
        Add("pid-number-unsigned-wrap", "P=list_to_pid(\"<0.18446744073709551633.23>\"),\"<0.17.23>\"=pid_to_list(P)");
        Add("pid-serial-unsigned-wrap", "P=list_to_pid(\"<0.17.18446744073709551639>\"),\"<0.17.23>\"=pid_to_list(P)");
        Add("pid-channel-unsigned-wrap", "P=list_to_pid(\"<18446744073709551616.17.23>\"),\"<0.17.23>\"=pid_to_list(P)");
        Add("pid-null-terminator", "P=list_to_pid(\"<0.17.23>\"++[0,255,$x]),\"<0.17.23>\"=pid_to_list(P)");
        Add("port-round-trip", "P=list_to_port(\"#Port<0.17>\"),P=list_to_port(port_to_list(P))");
        Add("port-local-value", "P=list_to_port(\"#Port<0.17>\"),true=is_port(P),\"#Port<0.17>\"=port_to_list(P)");
        Add("port-maximum-id", "P=list_to_port(\"#Port<0.1152921504606846975>\"),\"#Port<0.1152921504606846975>\"=port_to_list(P)");
        Add("port-leading-zeroes", "P=list_to_port(\"#Port<000.0017>\"),\"#Port<0.17>\"=port_to_list(P)");
        Add(
            "port-38-byte-bound",
            "Text=\"#Port<\"++lists:duplicate(28,$0)++\"0.1>\",38=length(Text),P=list_to_port(Text),\"#Port<0.1>\"=port_to_list(P)"
        );
        Add("port-number-unsigned-wrap", "P=list_to_port(\"#Port<0.18446744073709551633>\"),\"#Port<0.17>\"=port_to_list(P)");
        Add("port-channel-unsigned-wrap", "P=list_to_port(\"#Port<4294967296.17>\"),\"#Port<0.17>\"=port_to_list(P)");
        Add("port-null-terminator", "P=list_to_port(\"#Port<0.17>\"++[0,255,$x]),\"#Port<0.17>\"=port_to_list(P)");
        Add(
            "pid-old-local-creation",
            "P=binary_to_term(<<131,88,119,13,\"nonode@nohost\",17:32,23:32,1:32>>),\"<0.17.23>\"=pid_to_list(P),false=(P=:=list_to_pid(pid_to_list(P)))"
        );
        Add(
            "port-old-local-creation",
            "P=binary_to_term(<<131,120,119,13,\"nonode@nohost\",17:64,1:32>>),\"#Port<0.17>\"=port_to_list(P),false=(P=:=list_to_port(port_to_list(P)))"
        );
        Add(
            "pid-disconnected-foreign",
            "P=binary_to_term(<<131,88,119,9,\"peer@node\",17:32,23:32,0:32>>),Text=pid_to_list(P),Text=pid_to_list(P),false=lists:prefix(\"<0.\",Text),try list_to_pid(Text) of _->error(unexpected) catch error:badarg->ok end"
        );
        Add(
            "port-disconnected-foreign",
            "P=binary_to_term(<<131,120,119,9,\"peer@node\",18446744073709551615:64,0:32>>),Text=port_to_list(P),Text=port_to_list(P),false=lists:prefix(\"#Port<0.\",Text),try list_to_port(Text) of _->error(unexpected) catch error:badarg->ok end"
        );
        Add(
            "pid-native-zero-atom-channel",
            "P=binary_to_term(<<131,88,119,5,\"false\",17:32,23:32,0:32>>),\"<0.17.23>\"=pid_to_list(P),false=(P=:=list_to_pid(pid_to_list(P)))"
        );
        Add(
            "port-native-zero-atom-channel",
            "P=binary_to_term(<<131,120,119,5,\"false\",17:64,0:32>>),\"#Port<0.17>\"=port_to_list(P),false=(P=:=list_to_port(port_to_list(P)))"
        );

        int index = 0;
        foreach (string value in new[] { "wrong", "1", "[]", "\"\"", "[256]", "[-1]", "[wrong]", "[48|wrong]", "<<1>>", "\"<0.17.23>\"++[0,256]" })
            foreach (string function in new[] { ProcessIdentifierTextNames.ListToPid, ProcessIdentifierTextNames.ListToPort })
                Add("bad-input-" + index++, "try erlang:" + function + "(" + value + ") of _->error(unexpected) catch error:badarg->ok end");
        index = 0;
        foreach (string value in new[] { "\"<0.17>\"", "\"<0.17.23>extra\"", "\"<0.+17.23>\"", "\"<-0.17.23>\"", "\"<0..23>\"", "\"<0.17.>\"", "\"<0.17.23\"", "\"<1.17.23>\"", "\"<0.268435456.0>\"", "\"<0.0.4294967296>\"", "\"<0.17. 23>\"", "\"<\"++lists:duplicate(58,$0)++\"0.1.0>\"" })
            Add("bad-pid-syntax-" + index++, "try list_to_pid(" + value + ") of _->error(unexpected) catch error:badarg->ok end");
        index = 0;
        foreach (string value in new[] { "\"#port<0.17>\"", "\"#Port<0.17>extra\"", "\"#Port<0.+17>\"", "\"#Port<-0.17>\"", "\"#Port<0.>\"", "\"#Port<0.17\"", "\"#Port<1.17>\"", "\"#Port<0.1152921504606846976>\"", "\"#Port<0. 17>\"", "\"#Port<\"++lists:duplicate(29,$0)++\"0.1>\"" })
            Add("bad-port-syntax-" + index++, "try list_to_port(" + value + ") of _->error(unexpected) catch error:badarg->ok end");
        index = 0;
        foreach (string function in new[] { ProcessIdentifierTextNames.PidToList, ProcessIdentifierTextNames.PortToList })
            foreach (string value in new[] { "wrong", "1", "{}", "[]", "make_ref()", function == ProcessIdentifierTextNames.PidToList ? "list_to_port(\"#Port<0.17>\")" : "self()" })
                Add("bad-print-type-" + index++, "try erlang:" + function + "(" + value + ") of _->error(unexpected) catch error:badarg->ok end");

        return cases;
    }
}
