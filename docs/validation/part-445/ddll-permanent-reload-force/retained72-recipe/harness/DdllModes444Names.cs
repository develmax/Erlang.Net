namespace Erlang;
internal static class DdllModes444Names
{
 internal const string Module="beam_ddll_modes444",Beam="beam_ddll_modes444.beam";
 internal const string Phase="ddll-modes444",Test="ddll/load-reload-monitor-owner-modes444";
 internal const string Receipt="comparison.json",Values="values.etf",Stacks="stacks.etf",Runtime="erlang",Encode="term_to_binary",Run="capture",RunStacks="capture_stacks";
 internal const int Cases=12,Rows=72;
 internal static readonly string[] CaseNames=["duplicate_ref_unload","duplicate_ref_reload_pending_driver","foreign_owner_unload_reload","unload_monitor_pending","unload_monitor_pending_driver","unload_cancel_regular_vs_only","loaded_monitor_during_unload","normal_load_during_reload","reload_repeat_while_pending","reload_cancel_by_final_unloads","foreign_demonitor_cannot_remove","monitor_owner_death"];
}
