using System.Text;

namespace Erlang.Differential;

/// <summary>Keep the native command line ASCII; Erlang scans decoded Unicode codepoints.</summary>
public static class OracleProtocol
{
    public static string EvaluationCommand(string expression)
    {
        string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(expression + "."));

        return "{ok,OracleTokens,_}=erl_scan:string(unicode:characters_to_list(base64:decode(\"" + encoded +
            "\"))),{ok,OracleExpressions}=erl_parse:parse_exprs(OracleTokens)," +
            "{value,OracleResult,_}=erl_eval:exprs(OracleExpressions,erl_eval:new_bindings())," +
            "io:format(\"~s\",[base64:encode(term_to_binary(OracleResult))]),halt().";
    }
}
