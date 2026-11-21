from pathlib import Path
import json,sys
stage,parent,verified,inputs,asn,catch=map(Path,sys.argv[1:])
template=(parent/'verify-linked-runtime.py').read_text()
s=template[:template.index('release_paths=')].replace(
    template.splitlines()[2],f'p=Path({parent.as_posix()!r})').replace(
    template.splitlines()[3],f'stage=Path({stage.as_posix()!r})')
raw=json.loads((verified/'full-uncached/otp-corpus.json').read_bytes())
paths={Path(row['Beam'].replace(chr(92),'/')).stem:row['Beam'].replace(chr(92),'/') for row in raw['Modules']}
names='mod_head httpd_instance_sup ssh_tcpip_forward_client ssh_tcpip_forward_srv mod_trace snmp_verbosity local_udp mnesia_frag xmerl_html'.split()
owners='PKIXAttributeCertificate-2009 PKIXCMP-2023 PKIX1Implicit-2009 PKIXCRMF-2009'.split()
s+=f'release_paths={ {k:paths[k] for k in names}!r}\nasn_paths={ {k:paths[k] for k in owners}!r}\n'
copy=template[template.index('needed='):template.index('recipes=')]
copy=copy.replace("needed=[*release_paths.values(),*asn_paths.values()]",'''has_xmerl='XmerlSourceOperandOrder' in groups
if not has_xmerl:
    release_paths.pop('xmerl_html')
needed=[*release_paths.values(),*asn_paths.values()]''')
s+=copy
trace=inputs/'projected-terminal-branch-root-1957'
recipes={
  'head':inputs/'owned-record-call-and-failures-1943/head-native.eval',
  'head-input':inputs/'owned-record-call-and-failures-1943/witness-body.txt',
  'supervisor':inputs/'fixed-list-delayed-call-copies-1952/supervisor-native.eval',
  'inbound':inputs/'owned-record-action-arms-1954/native.eval',
  'action':inputs/'owned-record-action-arms-1954/action-native.eval',
  'ssh-stub':inputs/'owned-record-action-arms-1954/stub/ssh_connection.beam',
  'trace':trace/'native.eval','httpd-stub':trace/'stub/httpd_socket.beam',
  'snmp':catch/'snmp-native.eval',
  'mnesia':stage/'mnesia-native-trial/observe-mnesia-frag.eval',
  'local-udp':inputs/'release-near-1941-scout-1958/native-local-udp.eval',
  'asn-try':asn/'named-default-try-residuals/native-protected-constant.eval',
  'asn-remote':asn/'remote-optional-field-lifetime/native-remote-optional.eval',
  'xmerl':stage/'observe-xmerl-html.eval',
}
s+=f'recipes={ {k:v.as_posix() for k,v in recipes.items()}!r}\n'
s+=r'''
recipes={key:Path(path) for key,path in recipes.items()}
(root/'head-sample.txt').write_bytes(recipes['head-input'].read_bytes())
direct=[
    ('mod_head','head',12,[root/'head-sample.txt']),
    ('httpd_instance_sup','supervisor',10,[]),
    ('mod_trace','trace',10,[recipes['httpd-stub']]),
    ('snmp_verbosity','snmp',4,[]),
    ('local_udp','local-udp',5,[]),
    ('mnesia_frag','mnesia',3,[]),
]
xmerl_cases=0
if has_xmerl:
    xmerl_cases=read(stage/'xmerl-native-plan.json')['Observations']
    direct.append(('xmerl_html','xmerl',xmerl_cases,[]))
for module,key,count,extra in direct:
    relative=release_paths[module]
    outputs={}
    for label,role in [('Reference',refs),('Candidate',generated)]:
        out=root/f'{module}-{label}.terms'
        evaluate(recipes[key],[role/relative,*extra,out],root,f'{module}-{label}')
        outputs[label]=[sha(out)]
        if key=='snmp':
            outputs[label].append(sha(root/f'{module}-{label}.stdout.log'))
    assert outputs['Reference']==outputs['Candidate'],module
    witnesses[module]=dict(Beam=sha(generated/relative),Observations=count,Outcomes=outputs['Candidate'])
'''
s+=template[template.index("ssh=['ssh_tcpip"):template.index("asn_before=root")]
s+=r'''
asn_before=root/'asn-parent-images'
asn_before.mkdir()
prior_raw=read(p/'full-uncached/otp-corpus.json')
prior_status={row['Beam'].replace(chr(92),'/'):row['Status'] for row in prior_raw['Modules']}
prior_exact=sorted(relative for relative in asn_paths.values() if prior_status[relative]=='Equal')
if prior_exact:
    capture_before=root/'prior-raw-exact-capture'
    previous=read(p/'source-freeze.json')
    for artifact in ('Compiler','Runtime'):
        assert sha(p/f'corpus-bin/Erlang.{artifact}.dll').lower()==previous[artifact].lower()
    capture_exact(capture_before,prior_exact,p/'corpus-bin')
for relative in asn_paths.values():
    target=asn_before/relative
    target.parent.mkdir(parents=True,exist_ok=True)
    prior=capture_before/'otp-corpus.json.generated'/relative if relative in prior_exact else prior_image(relative)
    shutil.copyfile(prior,target)
for key,expected in [('asn-try',28),('asn-remote',38)]:
    evaluate(recipes[key],[refs,asn_before,generated],root,key)
    log=root/f'{key}.stdout.log'
    rows=re.findall(r"^'([^']+)'.*? (\d+) cases, baseline (\d+), candidate (\d+)\s*$",
                    log.read_text(encoding='utf-8'),re.MULTILINE)
    assert sum(int(row[1]) for row in rows)==expected,(key,rows)
    assert all(row[1]==row[3] for row in rows),rows
    for module,count,_,_ in rows:
        entry=witnesses.setdefault(module,dict(Beam=sha(generated/asn_paths[module]),Observations=0,Outcomes={}))
        entry['Observations']+=int(count)
        entry['Outcomes'][key]=sha(log)
assert sum(row['Observations'] for row in witnesses.values())==146+xmerl_cases,witnesses
'''
s+=template[template.index("freeze=read(until('source-freeze.json'))"):]
compile(s,str(stage/'verify-linked-runtime.py'),'exec')
(stage/'verify-linked-runtime.py').write_text(s,encoding='utf-8',newline='\n')
shell=(parent/'run-linked-runtime.ps1').read_text().replace(parent.as_posix(),stage.as_posix())
(stage/'run-linked-runtime.ps1').write_text(shell,encoding='utf-8',newline='\n')

