from pathlib import Path
import json,sys
stage,parent,inputs,asn,binary,move=map(Path,sys.argv[1:])
original=(parent/'verify-linked-runtime.py').read_text()
s=original[:original.index('release_paths=')].replace(
    original.splitlines()[2], f'p=Path({parent.as_posix()!r})').replace(
    original.splitlines()[3], f'stage=Path({stage.as_posix()!r})')
found={}
sources=[stage/'pilot-modules.txt',parent/'pilot-modules.txt']
for source in sources:
    for row in source.read_text(encoding='utf-8-sig').splitlines():
        found[Path(row).stem]=row
names='mod_head httpd_instance_sup inet_udp inet6_udp local_udp ssh_tcpip_forward_client ssh_tcpip_forward_srv xmerl_xsd_re_parse reltool_server'.split()
owners=['PKIX1Implicit-2009','PKIXAttributeCertificate-2009']
s+=f'release_paths={ {k:found[k] for k in names}!r}\nasn_paths={ {k:found[k] for k in owners}!r}\n'
s+=original[original.index('needed='):original.index('recipes=')]
paths={
  'head':inputs/'owned-record-call-and-failures-1943/head-native.eval',
  'head-input':inputs/'owned-record-call-and-failures-1943/witness-body.txt',
  'supervisor':inputs/'fixed-list-delayed-call-copies-1952/supervisor-native.eval',
  'udp':move/'udp-native/native-udp.eval',
  'reltool':stage/'reltool-trial/observe-reltool.eval',
  'inbound':inputs/'owned-record-action-arms-1954/native.eval',
  'action':inputs/'owned-record-action-arms-1954/action-native.eval',
  'ssh-stub':inputs/'owned-record-action-arms-1954/stub/ssh_connection.beam',
  'parser':binary/'accept-return-frames-1942/native-parser-scope.eval',
  'asn':asn/'record-encoder-residuals/native-retained-record.eval',
}
s+=f'recipes={ {k:v.as_posix() for k,v in paths.items()}!r}\n'
s+=r'''
recipes={key:Path(path) for key,path in recipes.items()}
(root/'head-sample.txt').write_bytes(recipes['head-input'].read_bytes())
direct=[
    ('mod_head','head',12,[root/'head-sample.txt']),
    ('httpd_instance_sup','supervisor',10,[]),
    ('inet_udp','udp',2,[]),
    ('inet6_udp','udp',2,[]),
    ('local_udp','udp',1,[]),
    ('reltool_server','reltool',3,[]),
]
for module,key,count,extra in direct:
    relative=release_paths[module]
    for label,directory in [('Reference',refs),('Candidate',generated)]:
        out=root/f'{module}-{label}.terms'
        evaluate(recipes[key],[directory/relative,*extra,out],root,f'{module}-{label}')
    outcome=sha(root/f'{module}-Candidate.terms')
    assert outcome==sha(root/f'{module}-Reference.terms'),module
    witnesses[module]=dict(Beam=sha(generated/relative),Observations=count,Outcome=outcome)
ssh=['ssh_tcpip_forward_client','ssh_tcpip_forward_srv']
for case in ['inbound','action']:
    extra=[] if case=='inbound' else [recipes['ssh-stub']]
    for label,directory in [('Reference',refs),('Candidate',generated)]:
        targets=[directory/release_paths[module] for module in ssh]
        evaluate(recipes[case],[*targets,*extra,root/f'ssh-{case}-{label}.terms'],root,f'ssh-{case}-{label}')
    outcome=sha(root/f'ssh-{case}-Candidate.terms')
    assert outcome==sha(root/f'ssh-{case}-Reference.terms'),case
    for module in ssh:
        entry=witnesses.setdefault(module,dict(Beam=sha(generated/release_paths[module]),Observations=0,Outcomes={}))
        entry['Observations']+=9
        entry['Outcomes'][case]=outcome
regex=release_paths['xmerl_xsd_re_parse']
evaluate(recipes['parser'],[refs/regex,prior_image(regex),generated/regex,root/'regex.terms'],root,'regex')
assert len((root/'regex.terms').read_text().splitlines())==5
witnesses['xmerl_xsd_re_parse']=dict(Beam=sha(generated/regex),Observations=5,Outcome=sha(root/'regex.terms'))
asn_before=root/'asn-parent-images'
asn_before.mkdir()
for relative in asn_paths.values():
    target=asn_before/relative
    target.parent.mkdir(parents=True,exist_ok=True)
    shutil.copyfile(prior_image(relative),target)
evaluate(recipes['asn'],[refs,asn_before,generated],root,'asn-retained-pairs')
log=root/'asn-retained-pairs.stdout.log'
rows=re.findall(r"^'([^']+)'/.*? (\d+) cases, baseline (\d+), candidate (\d+)\s*$",
                log.read_text(encoding='utf-8'),re.MULTILINE)
assert sum(int(row[1]) for row in rows)==63,rows
assert all(row[1]==row[3] for row in rows),rows
for module,count,_,_ in rows:
    entry=witnesses.setdefault(module,dict(Beam=sha(generated/asn_paths[module]),Observations=0,Outcome=sha(log)))
    entry['Observations']+=int(count)
assert sum(row['Observations'] for row in witnesses.values())==134,witnesses
'''
s+=original[original.index("freeze=read(until('source-freeze.json'))"):]
compile(s,str(stage/'verify-linked-runtime.py'),'exec')
(stage/'verify-linked-runtime.py').write_text(s,encoding='utf-8',newline='\n')
shell=(parent/'run-linked-runtime.ps1').read_text().replace(parent.as_posix(),stage.as_posix())
(stage/'run-linked-runtime.ps1').write_text(shell,encoding='utf-8',newline='\n')

