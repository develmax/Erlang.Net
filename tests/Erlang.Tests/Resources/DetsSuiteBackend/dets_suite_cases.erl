%% Calls unchanged selected OTP DETS suite cases. No library or CT replacement.
-module(dets_suite_cases).
-export([run_worker/3,error_worker/2]).

run_worker(Parent,Tag,Dir) ->
    Config = [{priv_dir,Dir},{data_dir,Dir}],
    Cases = [basic,insert_new,update_counter,fold,badarg,repair_continuation,access],
    Results = [{Case,apply(dets_SUITE,Case,[Config])} || Case <- Cases],
    Parent ! {Tag,Results}.

error_worker(Parent,Tag) -> Parent ! {Tag,[]}.
