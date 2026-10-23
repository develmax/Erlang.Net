from pathlib import Path
import shutil
out=Path('artifacts/library448-probe1');assert not out.exists();out.mkdir()
old=Path('artifacts/library447-probe4')
worker=(old/'beam_ddll_identity447.erl').read_text().replace('beam_ddll_identity447','beam_ddll_distribution448')
worker=worker.replace('retain_wire/1,save_ref/1','remote/3,seen_node/1,watch_node/1,await_down/1,retain_wire/1,save_ref/1')
worker+='''
remote(Node,F,A) ->
    Ref=make_ref(),{ddll448_worker,Node}!{call,self(),Ref,F,A},
    receive {reply,Ref,{ok,V}}->V;{reply,Ref,Other}->error({remote_failure,Other})
    after 10000->error(remote_timeout) end.
seen_node(Node) -> lists:member(Node,nodes()).
watch_node(Node) -> monitor_node(Node,true).
await_down(Node) -> receive {nodedown,Node}->down after 10000->error(node_down_timeout) end.
'''
(out/'beam_ddll_distribution448.erl').write_text(worker)
server=(old/'ddll_tcp447.erl').read_text().replace('ddll_tcp447','ddll_tcp448').replace('beam_ddll_identity447','beam_ddll_distribution448')
server=server.replace('loop(S,beam_ddll_distribution448:new()).','Worker=beam_ddll_distribution448:new(),true=register(ddll448_worker,Worker),loop(S,Worker).')
(out/'ddll_tcp448.erl').write_text(server)
shutil.copyfile(old/'Peer447.cs',out/'Peer448.cs')
s=(out/'Peer448.cs').read_text().replace('Peer447','Peer448');(out/'Peer448.cs').write_text(s)
