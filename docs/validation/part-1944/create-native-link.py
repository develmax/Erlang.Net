from pathlib import Path
import json,sys
stage,parent,inputs,asn,binary,catch=map(Path,sys.argv[1:])
template=(parent/'verify-linked-runtime.py').read_text()
s=template[:template.index('release_paths=')].replace(
    template.splitlines()[2],f'p=Path({parent.as_posix()!r})').replace(
    template.splitlines()[3],f'stage=Path({stage.as_posix()!r})')
paths={Path(row).stem:row for row in (stage/'pilot-modules.txt').read_text(encoding='utf-8-sig').splitlines()}
names='mod_head httpd_instance_sup ssh_tcpip_forward_client ssh_tcpip_forward_srv mod_trace snmp_verbosity erl_pp'.split()
owners=['PKIX1Implicit-2009']
s+=f'release_paths={ {k:paths[k] for k in names}!r}\nasn_paths={ {k:paths[k] for k in owners}!r}\n'
s+=template[template.index('needed='):template.index('recipes=')]
theirs=inputs/'projected-terminal-branch-root-1957'
paths={
  'head':inputs/'owned-record-call-and-failures-1943/head-native.eval',
  'head-input':inputs/'owned-record-call-and-failures-1943/witness-body.txt',
  'supervisor':inputs/'fixed-list-delayed-call-copies-1952/supervisor-native.eval',
  'inbound':inputs/'owned-record-action-arms-1954/native.eval',
  'action':inputs/'owned-record-action-arms-1954/action-native.eval',
  'ssh-stub':inputs/'owned-record-action-arms-1954/stub/ssh_connection.beam',
  'trace':theirs/'native.eval',
  'httpd-stub':theirs/'stub/httpd_socket.beam',
  'snmp':catch/'snmp-native.eval',
  'pp':binary/'sequential-comprehension-elements-1943/native-format-scope.eval',
  'asn-functions':asn/'implicit-last-functions/native-final-implicit.eval',
  'asn-units':asn/'binary-choice-wire-unit/native-binary-choice-wire.eval',
}
s+=f'recipes={ {k:v.as_posix() for k,v in paths.items()}!r}\n'
s+=r'''
recipes={key:Path(path) for key,path in recipes.items()}
(root/'head-sample.txt').write_bytes(recipes['head-input'].read_bytes())
direct=[
    ('mod_head','head',12,[root/'head-sample.txt']),
    ('httpd_instance_sup','supervisor',10,[]),
    ('mod_trace','trace',10,[recipes['httpd-stub']]),
    ('snmp_verbosity','snmp',4,[]),
    ('erl_pp','pp',3,[]),
]
for module,key,count,extra in direct:
    relative=release_paths[module]
    outcomes={}
    for label,directory in [('Reference',refs),('Candidate',generated)]:
        out=root/f'{module}-{label}.terms'
        args=[directory/relative] if key=='pp' else [directory/relative,*extra,out]
        evaluate(recipes[key],args,root,f'{module}-{label}')
        log=root/f'{module}-{label}.stdout.log'
        outcomes[label]=[sha(log)] if key=='pp' else [sha(out)]
        if key=='snmp':
            outcomes[label].append(sha(log))
    assert outcomes['Reference']==outcomes['Candidate'],module
    witnesses[module]=dict(Beam=sha(generated/relative),Observations=count,Outcomes=outcomes['Candidate'])
'''
s+=template[template.index("ssh=['ssh_tcpip"):template.index("regex=release_paths")]
s+=r'''
asn_before=root/'asn-parent-images'
asn_before.mkdir()
for relative in asn_paths.values():
    target=asn_before/relative
    target.parent.mkdir(parents=True,exist_ok=True)
    shutil.copyfile(prior_image(relative),target)
for key,expected in [('asn-functions',32),('asn-units',49)]:
    evaluate(recipes[key],[refs,asn_before,generated],root,key)
    log=root/f'{key}.stdout.log'
    rows=re.findall(r"^'([^']+)'/.*? (\d+) cases, baseline (\d+), candidate (\d+)\s*$",
                    log.read_text(encoding='utf-8'),re.MULTILINE)
    assert sum(int(row[1]) for row in rows)==expected,(key,rows)
    assert all(row[1]==row[3] for row in rows),rows
    for module,count,_,_ in rows:
        entry=witnesses.setdefault(module,dict(Beam=sha(generated/asn_paths[module]),Observations=0,Outcomes={}))
        entry['Observations']+=int(count)
        entry['Outcomes'][key]=sha(log)
assert sum(row['Observations'] for row in witnesses.values())==156,witnesses
'''
s+=template[template.index("freeze=read(until('source-freeze.json'))"):]
compile(s,str(stage/'verify-linked-runtime.py'),'exec')
(stage/'verify-linked-runtime.py').write_text(s,encoding='utf-8',newline='\n')
shell=(parent/'run-linked-runtime.ps1').read_text().replace(parent.as_posix(),stage.as_posix())
(stage/'run-linked-runtime.ps1').write_text(shell,encoding='utf-8',newline='\n')

