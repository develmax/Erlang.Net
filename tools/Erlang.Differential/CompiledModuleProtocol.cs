using System.Text;

namespace Erlang.Differential;

public static class CompiledModuleProtocol
{
    public static string Command(string source)
    {
        string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(source));

        return "{ok,Ts,_}=erl_scan:string(unicode:characters_to_list(base64:decode(\"" + encoded +
            "\"))), Split=fun Split([],[],Fs)->lists:reverse(Fs); " +
            "Split([T|Rest],Acc,Fs)->case T of {dot,_}->{ok,F}=erl_parse:parse_form(lists:reverse([T|Acc])),Split(Rest,[],[F|Fs]); _->Split(Rest,[T|Acc],Fs) end end," +
            "Forms=Split(Ts,[],[]),{ok,M,Bin,_}=compile:forms(Forms,[binary,return_errors,return_warnings])," +
            "{module,M}=code:load_binary(M,\"differential.erl\",Bin)," +
            "Result=try apply(M,run,[]) of V->{ok,V} catch C:R->{error,C,R} end," +
            "io:format(\"~s\",[base64:encode(term_to_binary(Result))]),halt().";
    }
}
