"""Root-executed metadata proposal only. Author did not execute or edit Main.
Run after actual Part291 full validation, before readiness generation/final archive.
"""
from pathlib import Path
import argparse, hashlib, json

p = argparse.ArgumentParser()
p.add_argument('--full-exit', required=True)
p.add_argument('--include-otp-ports', action='store_true')
args = p.parse_args()
r=Path.cwd().resolve(); a=r/'artifacts'; read=lambda q:json.loads(q.read_bytes())
assert read(Path(args.full_exit))['ExitCode']==0
t=read(a/'tests.json'); assert (t['Passed'],t['Failed'])==(6599,0)
assert b'Validation passed' in (a/'root-part291-full-validation.log').read_bytes()
ng=read(a/'nif-proof290-independent-guard.json')
assert ng['Head']=='f2e861afc3f1ff2bdfade705ecb058e7cef433d6' and len(ng['OriginalCases'])==7
assert all(x['Passed'] for x in ng['OriginalCases'].values()) and len(ng['ProcessControls']['Passed'])==2
q=r/'docs/module-readiness-scope.json'; before=q.read_bytes(); scope=json.loads(before)
components={x['Id']:x for x in scope['Components']}
assert all(x in components for x in ['Compiler','Runtime','NifFixtureHost'])
assert not any('Part291:' in x for c in components.values() for x in c.get('Implemented',[])), 'Refuse duplicate update'
scope['ComparisonTestReport']='validation/part-291/tests.json'
notes={
 'Compiler':'Part291: source -nifs control supports absent versus present-empty immutable declarations, repeated union and local/private-function validation, typed diagnostics and generated metadata. Original constructor signatures retained. Main6599/full; eight new compiled contracts and two diagnostics await original execution. Warning pipeline, broader lint parity and BEAM NIF declarations remain unfinished.',
 'Runtime':'Part291: immutable source NIF declaration metadata reaches ModuleCodeDescriptor without changing its public constructor signature; installed module_info(nifs) remains empty and fallback code runs. Main6599/full, default502 and erlang344/351 presence unchanged. Actual load_nif, native overrides, code replacement and purge lifetime remain unfinished.',
 'NifFixtureHost':'Part291: independently verified published290 workflow yields seven typed original NIF observations and two Linux capture controls; Windows trusted fixture ABI/ownership checks pass separately. These are tool/original-observer gates, not additional Main tests or general SDK load_nif parity; native unload counter was not compared.'
}
for key,note in notes.items(): components[key].setdefault('Implemented',[]).append(note)
nif=components['NifFixtureHost']; old='Original seven NIF observations and Linux collector controls pending; arbitrary NIF ABI, native threads, dirty jobs, on_load, source/BEAM replacement and registry/purge integration unfinished.'
assert nif['Remaining'].count(old)==1
nif['Remaining']=[('Arbitrary NIF ABI, native threads, dirty jobs, on_load, source/BEAM replacement, registry/purge integration and original native unload comparison remain unfinished.' if x==old else x) for x in nif['Remaining']]
assert nif['Status']=='Partially compatible' and nif['TestPrefixes']==[]
ports=r/'docs/OTP-PORTS.md'; ports_before=ports.read_bytes()
ports_note='\n## Part 291: source NIF declarations and original fixture proof\n\nPinned erl_lint declaration validation and beam_ssa nifs control semantics guide the source compiler: preserve absent versus present-empty declarations, deduplicate repeated controls and validate actual local user functions, including private ones. CLR metadata owns read-only copies through ModuleDefinition and ModuleCodeDescriptor while retaining original constructor signatures. This is a source-guided adaptation, not native dispatch installation or BEAM NIF decoding. The published290 manual workflow independently verifies seven typed original observations and two Linux collector controls; trusted Windows fixture assertions remain a separate ABI check. Generic load_nif, warning completeness, full callback coverage, on_load/atomic image replacement and purge lifetime remain unfinished; no original unload-counter comparison is claimed.\n'
if args.include_otp_ports: assert b'## Part 291:' not in ports_before
comparison=r/'docs/validation/part-291/tests.json'
report_bytes=(a/'tests.json').read_bytes()
if comparison.exists(): assert comparison.read_bytes()==report_bytes
else: assert not comparison.parent.exists(), 'Refuse an unrelated incomplete archive'
ledger={'Status':'ROOT_EXECUTED_PART291_READINESS_METADATA_UPDATE','BeforeScopeSha256':hashlib.sha256(before).hexdigest().upper(),'ComponentIds':list(notes),'ComparisonTestReport':'validation/part-291/tests.json','OtpPortsUpdated':args.include_otp_ports,'ReadinessGeneratedByThisScript':False}
ledger['ComparisonPreseed']={'Path':'docs/validation/part-291/tests.json','Bytes':len(report_bytes),'Sha256':hashlib.sha256(report_bytes).hexdigest().upper(),'Passed':6599,'Failed':0}
ledger['PreviousReadinessOutputs']=[{'Path':'docs/'+name,'Sha256':hashlib.sha256((r/'docs'/name).read_bytes()).hexdigest().upper()} for name in ['module-readiness.json','MODULE_READINESS.md']]
comparison.parent.mkdir(parents=True,exist_ok=True)
comparison.write_bytes(report_bytes)
q.write_text(json.dumps(scope,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
if args.include_otp_ports: ports.write_bytes(ports_before+ports_note.encode())
ledger['AfterScopeSha256']=hashlib.sha256(q.read_bytes()).hexdigest().upper()
(a/'root-part291-readiness-metadata-update.json').write_text(json.dumps(ledger,indent=2)+'\n',encoding='utf-8')
print(json.dumps(ledger))
