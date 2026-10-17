-module(fullsweep_process_domain).
-export([run/0]).
run() ->
    Previous=process_flag(fullsweep_after,17),
    try
        17=process_flag(fullsweep_after,65536),
        {fullsweep_after,0}=process_info(self(),fullsweep_after),
        0=process_flag(fullsweep_after,65537),
        {fullsweep_after,1}=process_info(self(),fullsweep_after),
        lists:foreach(fun(V)->
            try process_flag(fullsweep_after,V) of _->error(accepted_invalid)
            catch error:badarg->ok end,
            {fullsweep_after,1}=process_info(self(),fullsweep_after)
        end,[-1,1.0,wrong,[],1 bsl 100]), done
    after process_flag(fullsweep_after,Previous) end.
