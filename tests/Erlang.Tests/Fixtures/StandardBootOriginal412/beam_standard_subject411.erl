-module(beam_standard_subject411).
-export([worker/2,probe/1,start/2,stop/1]).

worker(Parent,Case)->
    Outcome=try probe(Case) of V->{returned,V} catch C:R:S->{caught,C,R,S} end,
    Parent!{Case,Outcome},ok.

probe(status)->init:get_status();
probe(script_id)->init:script_id();
probe(running_apps)->lists:keysort(1,application:which_applications());
probe(loaded_apps)->lists:keysort(1,application:loaded_applications());
probe(services)->[{Name,live(Name)} || Name<-[init,erl_prim_loader,application_controller,
    kernel_sup,logger,code_server,file_server_2,kernel_refc,kernel_config,logger_sup]];
probe(app_supervisors)->
    {ok,Root}=application:get_supervisor(kernel),
    {is_pid(Root) andalso Root=:=whereis(kernel_sup),application:get_supervisor(stdlib)};
probe(code_path)->[filename:basename(P)||P<-code:get_path()];
probe(app_dirs)->[filename:basename(code:lib_dir(App))||App<-[kernel,stdlib]];
probe(modules)->[{M,erlang:module_loaded(M)}||M<-[init,application,application_controller,
    application_master,kernel,supervisor,code_server,file_server,logger_server,?MODULE]];
probe(default_env)->{application:get_env(kernel,logger_level),
    application:get_env(kernel,net_tickintensity),application:get_env(stdlib,missing411)};
probe(wrong_supervisor_argument)->application:get_supervisor([]);
probe(wrong_application_timeout)->application:which_applications(-1);
probe(application_lifecycle)->
    App=beam_standard_tiny411,
    Spec={application,App,[{description,"standard411 tiny real application"},{vsn,"1"},
        {modules,[?MODULE]},{registered,[App]},{applications,[kernel,stdlib]},
        {env,[{marker,owned}]},{mod,{?MODULE,[]}}]},
    try
        ok=application:load(Spec),ok=application:start(App,temporary),
        Pid=whereis(App),true=is_pid(Pid),{ok,owned}=application:get_env(App,marker),
        ok=application:stop(App),false=is_process_alive(Pid),undefined=whereis(App),
        ok=application:unload(App),undefined=application:get_env(App,marker),
        {ok,true,stopped,unloaded}
    after
        application:stop(App),application:unload(App)
    end.

live(Name)->case whereis(Name) of P when is_pid(P)->is_process_alive(P); _->false end.

start(_,[])->
    Parent=self(),Pid=spawn_link(fun()->register(beam_standard_tiny411,self()),
        Parent!{ready,self()},tiny_loop() end),
    receive {ready,Pid}->{ok,Pid} end.
stop(_)->ok.
tiny_loop()->receive _->tiny_loop() end.
