namespace Erlang.Differential;

public static class SpawnPriorityCaseSources
{
    public const string FirstObservation = """
        -module(oracle_priority_first_observation).
        -export([run/0, child/1]).
        run() ->
            Parent=self(),
            lists:foreach(fun(Level)->
                {Pid,Ref}=spawn_opt(oracle_priority_first_observation,child,[Parent],[monitor,{priority,Level}]),
                receive {Pid,{priority,Level}} -> ok after 1000 -> error(missing_priority) end,
                receive {'DOWN',Ref,process,Pid,normal} -> ok after 1000 -> error(missing_down) end
            end,[low,normal,high,max]), done.
        child(Parent) -> Parent ! {self(),process_info(self(),priority)}.

        """;

    public const string FlagOldAndDefault = """
        -module(oracle_priority_flag_old_and_default).
        -export([run/0]).
        run() ->
            normal=process_flag(priority,high), {priority,high}=process_info(self(),priority),
            high=process_flag(priority,low), low=process_flag(priority,max), max=process_flag(priority,normal),
            Parent=self(), normal=process_flag(priority,high),
            {Pid,Ref}=spawn_monitor(fun()->Parent!{self(),process_info(self(),priority)} end),
            receive {Pid,{priority,normal}} -> ok after 1000 -> error(inherited_priority) end,
            receive {'DOWN',Ref,process,Pid,normal} -> ok after 1000 -> error(missing_down) end,
            high=process_flag(priority,normal), done.

        """;

    public const string DuplicateInvalid = """
        -module(oracle_priority_duplicate_invalid).
        -export([run/0]).
        run() ->
            Parent=self(),
            {Pid,Ref}=spawn_opt(fun()->Parent!{self(),process_info(self(),priority)} end,[monitor,{priority,high},{priority,low}]),
            receive {Pid,{priority,low}} -> ok after 1000 -> error(wrong_duplicate) end,
            receive {'DOWN',Ref,process,Pid,normal} -> ok after 1000 -> error(missing_down) end,
            lists:foreach(fun(Value)->
                try spawn_opt(fun()->Parent!unexpected_child end,[{priority,Value}]) of _ -> error(expected_badarg) catch error:badarg -> ok end
            end,[wrong,0,true,{},[]]),
            receive unexpected_child -> error(created_invalid_child) after 0 -> done end.

        """;

}
