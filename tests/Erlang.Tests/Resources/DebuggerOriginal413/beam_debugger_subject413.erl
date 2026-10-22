-module(beam_debugger_subject413).
-export([worker/2,probe/1]).

worker(Parent,Case) ->
    Outcome=try probe(Case) of V->{returned,V}
            catch C:R:S->{caught,C,R,S} end,
    Parent ! {Case,Outcome},ok.

probe(supported0) -> erl_debugger:supported();
probe(instrumentations0) -> erl_debugger:instrumentations();
probe(toggle_instrumentations1) -> erl_debugger:toggle_instrumentations(#{line_breakpoint=>true});
probe(register1) -> erl_debugger:register(not_a_pid);
probe(unregister2) -> erl_debugger:unregister(not_a_pid,not_a_session);
probe(whereis0) -> erl_debugger:whereis();
probe(breakpoint3) -> erl_debugger:breakpoint(?MODULE,1,true);
probe(breakpoints1) -> erl_debugger:breakpoints(?MODULE);
probe(breakpoints3) -> erl_debugger:breakpoints(?MODULE,worker,2);
probe(stack_frames2) -> erl_debugger:stack_frames(not_a_pid,0);
probe(peek_stack_frame_slot4) -> erl_debugger:peek_stack_frame_slot(not_a_pid,0,0,0);
probe(xregs_count1) -> erl_debugger:xregs_count(not_a_pid);
probe(peek_xreg3) -> erl_debugger:peek_xreg(not_a_pid,0,0).
