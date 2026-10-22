-module(beam_prim_tty412).
-export([run/0,run_stacks/0,worker/2]).
cases() -> [isatty_stdout,isatty_stderr,isatty_stdin,isatty_unknown,tinfo,tigetnum,tigetflag,tigetstr,tputs,tinfo_invalid_wrapper,width_ascii,width_wide,width_latin,width_bad,init_unicode,init_capability,init_geometry,write_empty,write_bytes,init_stderr,input_closed].
run() -> [{C,observe(C)} || C<-cases()].
run_stacks() -> [depth(D)||D<-[default,0,1,2,8]].
depth(default)->{default,run()};
depth(D)->Old=erlang:system_flag(backtrace_depth,D),try {D,run()} after erlang:system_flag(backtrace_depth,Old) end.
observe(C)->P=self(),R=make_ref(),{Pid,M}=spawn_monitor(?MODULE,worker,[{P,R},C]),receive {R,V}->receive {'DOWN',M,process,Pid,normal}->V end end.
worker({P,R},C)->register(beam_prim_tty412_worker,self()),V=try probe(C) of X->{value,X} catch K:E:S->{caught,K,E,S} end,P!{R,V}.
with_state(Output,F)->S=prim_tty:init(#{input=>disabled,output=>raw,ofd=>Output}),
    %% Exact pinned state record: writer is field4. Used ONLY for real cleanup.
    Writer=element(1,element(4,S)),M=erlang:monitor(process,Writer),
    try F(S) after unlink(Writer),exit(Writer,shutdown),receive {'DOWN',M,process,Writer,shutdown}->ok end end.
write_ack(S,Bytes)->#{write:=WriterRef}=prim_tty:handles(S),{ok,M}=prim_tty:write(S,Bytes,self()),receive {WriterRef,ok}->demonitor(M,[flush]),ok;{'DOWN',M,process,_,E}->{error,E} end.
probe(isatty_stdout)->prim_tty:isatty(stdout);
probe(isatty_stderr)->prim_tty:isatty(stderr);
probe(isatty_stdin)->prim_tty:isatty(stdin);
probe(isatty_unknown)->prim_tty:isatty(prim_tty_unknown412);
probe(tinfo)->prim_tty:tinfo();
probe(tigetnum)->prim_tty:tigetnum("co");
probe(tigetflag)->prim_tty:tigetflag("xn");
probe(tigetstr)->prim_tty:tigetstr("clear");
probe(tputs)->prim_tty:tputs(<<"x">>,[]);
probe(tinfo_invalid_wrapper)->prim_tty:tigetstr({invalid,capability});
probe(width_ascii)->prim_tty:npwcwidth(65);
probe(width_wide)->prim_tty:npwcwidth(16#4e00);
probe(width_latin)->prim_tty:npwcwidth(65,false);
probe(width_bad)->prim_tty:npwcwidth(invalid);
probe(init_unicode)->with_state(stdout,fun(S)->prim_tty:unicode(S) end);
probe(init_capability)->with_state(stdout,fun(S)->{have_termcap,element(6,S)} end);
probe(init_geometry)->with_state(stdout,fun(S)->prim_tty:window_size(S) end);
probe(write_empty)->with_state(stdout,fun(S)->write_ack(S,<<>>) end);
probe(write_bytes)->with_state(stdout,fun(S)->write_ack(S,<<"PRIM_TTY412_WRITE\n">>) end);
probe(init_stderr)->with_state(stderr,fun(S)->prim_tty:unicode(S) end);
probe(input_closed)->with_state(stdout,fun(S)->prim_tty:tty_is_open(element(2,S),stdin) end).
