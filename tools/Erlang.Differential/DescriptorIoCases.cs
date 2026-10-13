namespace Erlang.Differential;

public static class DescriptorIoCases
{
    public static IReadOnlyList<DescriptorIoCase> All { get; } = Build();

    private static IReadOnlyList<DescriptorIoCase> Build()
    {
        const string open = "P=open_port({fd,$INPUT$,$OUTPUT$},[";
        const string receive = "receive {P,{data,<<65,66>>}}->ok after 5000->error(no_data) end,";
        const string eof = "receive {P,eof}->ok after 5000->error(no_eof) end,";
        const string close = "true=erlang:port_close(P)";
        var cases = new List<DescriptorIoCase>();
        foreach ((string name, string settings, string input, string output) in new[]
        {
            ("stream", "binary,eof", "4142", "4344"),
            ("packet-one", "binary,eof,{packet,1}", "024142", "024344"),
            ("packet-two", "binary,eof,{packet,2}", "00024142", "00024344"),
            ("packet-four", "binary,eof,{packet,4}", "000000024142", "000000024344")
        })
            cases.Add(new(
                name,
                open + settings + "])," + receive + eof + "[]=erlang:port_info(P,os_pid),true=erlang:port_command(P,<<67,68>>)," + close,
                input,
                output,
                settings
            ));
        cases.Add(new(
            "line",
            open + "binary,eof,{line,2}]),receive {P,{data,{eol,<<65,66>>}}}->ok after 5000->error(no_line) end," + eof + "true=erlang:port_command(P,<<67,68>>)," + close,
            "41420A",
            "4344",
            "binary,eof,{line,2}"
        ));
        cases.Add(new(
            "list",
            open + "eof]),receive {P,{data,[65,66]}}->ok after 5000->error(no_list) end," + eof + "true=erlang:port_command(P,[67,68])," + close,
            "4142",
            "4344",
            "eof"
        ));
        cases.Add(new(
            "ignored-exit-status",
            open + "binary,eof,exit_status])," + receive + eof + "receive {P,{exit_status,_}}->error(unexpected_status) after 20->ok end," + close,
            "4142",
            "",
            "binary,eof,exit_status"
        ));
        cases.Add(new(
            "eof-closes",
            "process_flag(trap_exit,true)," + open + "binary])," + receive + "receive {'EXIT',P,normal}->ok after 5000->error(no_exit) end,undefined=erlang:port_info(P)",
            "4142",
            "",
            "binary"
        ));

        return cases;
    }
}
