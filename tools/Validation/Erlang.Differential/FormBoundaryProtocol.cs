using System.Text;

namespace Erlang.Differential;

public static class FormBoundaryProtocol
{
    public static string Command(string source)
    {
        string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(source));

        return "{ok,Ts,_}=erl_scan:string(unicode:characters_to_list(base64:decode(\"" + encoded +
            "\"))),Split=fun Split([],[],Fs)->{ok,lists:reverse(Fs)};" +
            "Split([],Acc,_)->case erl_parse:parse_form(lists:reverse(Acc)) of {error,{_,_,R}}->{error,[R]};{ok,_}->{ok,[]} end;" +
            "Split([T|Rest],Acc,Fs)->case T of {dot,_}->case erl_parse:parse_form(lists:reverse([T|Acc])) of " +
            "{ok,F}->Split(Rest,[],[F|Fs]);{error,{_,_,R}}->{error,[R]} end;_->Split(Rest,[T|Acc],Fs) end end," +
            "Result=case Split(Ts,[],[]) of {error,_}=Error->Error;{ok,_}->{ok,[]} end," +
            OracleResultProtocol.Command("Result");
    }
}
