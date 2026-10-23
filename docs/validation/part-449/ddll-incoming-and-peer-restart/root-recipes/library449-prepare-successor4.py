from pathlib import Path
import shutil,json
out=Path('artifacts/library449-probe4');assert not out.exists();shutil.copytree('artifacts/library449-probe3',out)
p=out/'beam_ddll_distribution449.erl';s=p.read_text().replace('ping_node/1,set_peer_cookie/2,','').replace('\nping_node(Node) -> net_adm:ping(Node).\nset_peer_cookie(Node,Cookie) -> erlang:set_cookie(Node,Cookie).\n','\n');p.write_text(s)
p=out/'ddll_tcp449.erl';s=p.read_text().replace('Result=beam_ddll_distribution449:request(Worker,Function,Arguments),','Result=case {Function,Arguments} of\n                        {connect_peer,[Node]} -> {ok,net_kernel:connect_node(Node)};\n                        {set_peer_cookie,[Node,Cookie]} -> {ok,erlang:set_cookie(Node,Cookie)};\n                        _ -> beam_ddll_distribution449:request(Worker,Function,Arguments)\n                    end,');p.write_text(s)
p=out/'Program.cs';s=p.read_text().replace('"ping_node"','"connect_peer"').replace('Term.A("pang")','Term.A("false")').replace('Term.A("pong")','Term.A("true")');p.write_text(s)
(out/'reviewed-successor.json').write_text(json.dumps({'Previous':'artifacts/library449-probe3','Reason':'Original-only connection bootstrap lives in auxiliary original server, not imported into common managed worker. Actual net_kernel connect_node requires no remote net_adm is_auth service. Preserve unsupported import failure, no runtime bypass or synthetic pong.'},indent=2)+'\n')
print('Successor4 prepared')
