-module(error_logger_suite_cases).
-export([write/1,worker/2,run/1]).
cases()->[off_heap,error_report,info_report,error,info,tty,logfile,add,delete,format_depth].
write(D)->
    logger:set_primary_config(level,all),
    Rows=[{C,run(C)}||C<-cases()],
    ok=file:write_file(filename:join(D,"upstream.etf"),term_to_binary(Rows)),
    io:format("~p upstream cases~n",[length(Rows)]),ok.
worker(C,P)->P!{self(),run(C)}.
run(C)->
    catch logger:remove_handler(error_logger),catch error_logger:stop(),
    try
        error_logger_SUITE:init_per_suite([]),
        {ok,apply(error_logger_SUITE,C,[[]])}
    catch K:R:S->{K,R,S}
    after error_logger_SUITE:end_per_suite([]),
        catch application:unset_env(kernel,error_logger_format_depth),
        file:delete("dummy_logfile.log") end.
