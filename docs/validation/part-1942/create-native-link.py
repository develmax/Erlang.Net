from pathlib import Path
import json, re, sys
parent, stage, inputs, asn, binary, roots = map(Path,sys.argv[1:])
original=(parent/'verify-linked-runtime.py').read_text()
prefix=original[:original.index("release_paths={")]
prefix=prefix.replace("p=Path(r'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/bounded-entry-and-generator-calls-1940')",f"p=Path({parent.as_posix()!r})")
prefix=prefix.replace(parent.as_posix(),stage.as_posix(),1) if False else prefix.replace(f"stage=Path('{parent.as_posix()}')",f"stage=Path({stage.as_posix()!r})")
paths = {Path(line).stem:line for line in (stage/'pilot-modules.txt').read_text().splitlines()}
release = ['mod_head','httpd_instance_sup','rand','erl_pp','inet_parse','ssh_tcpip_forward_client','ssh_tcpip_forward_srv']
owners = ['PKIX1Implicit-2009','PKIXAttributeCertificate-2009','PKIXCMP-2023','PKIXCRMF-2009','OTP-PKIX']
copy=original[original.index('needed='):original.index('archived=')]
copy=copy.replace("    matches=list((p/'full-uncached').glob",'''    full=p/'full-uncached'
    deadline=time.monotonic()+1800
    while not (full/'otp-corpus.json').is_file():
        if time.monotonic()>deadline:
            raise TimeoutError(full)
        time.sleep(3)
    matches=list(full.glob''')
recipe = dict(
    head=inputs/'owned-record-call-and-failures-1943/head-native.eval',
    sample=inputs/'owned-record-call-and-failures-1943/witness-body.txt',
    supervisor=inputs/'fixed-list-delayed-call-copies-1952/supervisor-native.eval',
    rand_pp=roots/'release-native/observe-rand-pp.eval',
    inet=binary/'parser-residual-1941/native-parser-scope.eval',
    ssh=inputs/'nested-ssh-record-dispatch-1953/native.eval',
    required=asn/'leading-required-record-residuals/native-leading-required.eval',
    literal=asn/'literal-table-optional-residuals/native-optional-literal-table.eval',
    alias=asn/'multi-tag-optional-residuals/native-optional-alias-join.eval',
)
body=r'''
archived = {key:Path(path) for key,path in recipes.items()}
(root/'head-sample.txt').write_bytes(archived['sample'].read_bytes())
direct = [
    ('mod_head','head',12,[root/'head-sample.txt']),
    ('httpd_instance_sup','supervisor',10,[]),
    ('rand','rand_pp',7,[]),
    ('erl_pp','rand_pp',5,[]),
    ('inet_parse','inet',3,[]),
]
for module,key,count,extra in direct:
    relative=release_paths[module]
    outcomes={}
    for label,image in [('Reference',refs/relative),('Candidate',generated/relative)]:
        out=root/f'{module}-{label}.terms'
        args=[image] if key=='inet' else [image,*extra,out]
        evaluate(archived[key],args,root,f'{module}-{label}')
        if key=='inet':
            out=root/f'{module}-{label}.stdout.log'
        outcomes[label]=sha(out)
    assert len(set(outcomes.values()))==1,module
    witnesses[module]=dict(Beam=sha(generated/relative),Observations=count,Outcome=outcomes['Candidate'])
ssh=['ssh_tcpip_forward_client','ssh_tcpip_forward_srv']
for label,directory in [('Reference',refs),('Candidate',generated)]:
    evaluate(archived['ssh'],[*(directory/release_paths[module] for module in ssh),root/f'ssh-{label}.terms'],root,f'ssh-{label}')
assert sha(root/'ssh-Reference.terms')==sha(root/'ssh-Candidate.terms')
for module in ssh:
    witnesses[module]=dict(Beam=sha(generated/release_paths[module]),Observations=9,Outcome=sha(root/'ssh-Candidate.terms'))
parent=root/'asn-parent-images'
parent.mkdir()
for relative in asn_paths.values():
    destination=parent/relative
    destination.parent.mkdir(parents=True,exist_ok=True)
    shutil.copyfile(prior_image(relative),destination)
for key,expected in [('required',50),('literal',66),('alias',36)]:
    label=f'asn-{key}'
    evaluate(archived[key],[refs,parent,generated],root,label)
    log=root/f'{label}.stdout.log'
    rows=re.findall(r"^'([^']+)'/.*? (\d+) cases, baseline (\d+), candidate (\d+)\s*$",
                    log.read_text(encoding='utf-8'),re.MULTILINE)
    assert sum(int(row[1]) for row in rows)==expected,(key,rows)
    assert all(row[1]==row[3] for row in rows),rows
    for stem,count,_,_ in rows:
        value=witnesses.setdefault(stem,dict(Beam=sha(generated/asn_paths[stem]),Observations=0,Outcomes={}))
        value['Observations']+=int(count)
        value['Outcomes'][key]=sha(log)
'''
suffix=original[original.index("freeze=read(until('source-freeze.json'))"):]
prefix=prefix.replace("def escript(recipe,beam,folder,expected,candidate):"+prefix.split("def escript(recipe,beam,folder,expected,candidate):")[1].split("pilot=read")[0],"") 
script=prefix+f'release_paths={ {k:paths[k] for k in release}!r}\nasn_paths={ {k:paths[k] for k in owners}!r}\n'+copy+f'recipes={ {k:v.as_posix() for k,v in recipe.items()}!r}\n'+body+suffix
compile(script,str(stage/'verify-linked-runtime.py'),'exec')
(stage/'verify-linked-runtime.py').write_text(script,encoding='utf-8',newline='\n')
for name in ['run-linked-runtime.ps1']:
    new=(parent/name).read_text().replace(parent.as_posix(),stage.as_posix())
    (stage/name).write_text(new,encoding='utf-8',newline='\n')

