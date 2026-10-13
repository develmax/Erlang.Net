namespace Erlang.Differential;

public static class PortLineCases
{
    public static IReadOnlyList<CompiledModuleCase> Modules { get; } = Build();

    private static IReadOnlyList<CompiledModuleCase> Build()
    {
        var result = new List<CompiledModuleCase>();

        void Add(string name, string body) => result.Add(new(
            "erlang-port-line-" + name,
            "-module(port_line_contract). -export([run/0]). run()->" + body + ",done. " +
            "lines(_,0,A)->lists:reverse(A);lines(P,N,A)->receive {P,{data,D}}->lines(P,N-1,[D|A]) after 5000->error(missing_line) end.",
            Term.Tuple(Term.A(CompiledContractOutcomeNames.Success), Term.A(CompiledContractOutcomeNames.Completion))
        ));
        foreach (int capacity in new[] { 1, 2, 3, 4, 5, 32 })
        {
            string[] chunks = capacity switch
            {
                1 => new[] { "noeol,a", "noeol,b", "eol,c", "noeol,1", "noeol,2", "noeol,3", "eol,4", "eol,", "noeol,e", "noeol,n", "noeol,d", "noeol,\r" },
                2 => new[] { "noeol,ab", "eol,c", "noeol,12", "eol,34", "eol,", "noeol,en", "noeol,d\r" },
                3 => new[] { "eol,abc", "noeol,123", "eol,4", "eol,", "noeol,end", "noeol,\r" },
                _ => new[] { "eol,abc", "eol,1234", "eol,", "noeol,end\r" }
            };
            foreach (bool binary in new[] { false, true })
            {
                string expected = "[" + string.Join(
                    ',',
                    chunks.Select(chunk =>
                {
                    int comma = chunk.IndexOf(',');
                    string text = "\"" + chunk[(comma + 1)..].Replace("\r", "\\r") + "\"";

                    return "{" + chunk[..comma] + "," + (binary ? "<<" + text + ">>" : text) + "}";
                })
                ) + "]";
                Add(
                    "capacity-" + capacity + (binary ? "-binary" : "-list"),
                    "P=open_port({spawn,\"dotnet tools/Erlang.PortProbe/bin/Debug/net10.0/Erlang.PortProbe.dll lines\"},[{line," + capacity + "},eof" + (binary ? ",binary" : string.Empty) + "])," +
                    expected + "=lines(P," + chunks.Length + ",[]),receive {P,eof}->ok after 5000->error(no_eof) end,true=erlang:port_close(P)"
                );
            }
        }
        Add(
            "echo-crlf",
            "P=open_port({spawn,\"dotnet tools/Erlang.PortProbe/bin/Debug/net10.0/Erlang.PortProbe.dll\"},[binary,{line,3}]),true=erlang:port_command(P,<<\"abc\">>),true=erlang:port_command(P,<<13>>),true=erlang:port_command(P,<<10>>),[{eol,<<\"abc\">>}]=lines(P,1,[]),true=erlang:port_close(P)"
        );
        foreach (string option in new[] { "{line,0}", "{line,-1}", "{line,atom}", "{line,1.0}", "{line,[]}", "{line,3},{packet,1}", "{packet,2},{line,3}" })
            Add(
                "bad-option-" + result.Count,
                "try open_port({spawn,\"unused\"},[" + option + "]) of _->error(accepted) catch error:badarg->ok end"
            );

        Add(
            "explicit-close-discards-partial",
            "P=open_port({spawn,\"dotnet tools/Erlang.PortProbe/bin/Debug/net10.0/Erlang.PortProbe.dll\"},[binary,{line,16}]),true=erlang:port_command(P,<<\"prefix\\nTail\">>),[{eol,<<\"prefix\">>}]=lines(P,1,[]),true=erlang:port_close(P),receive {P,{data,_}}->error(unexpected_flush) after 0->ok end"
        );

        return result;
    }
}
