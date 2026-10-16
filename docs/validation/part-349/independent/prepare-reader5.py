from pathlib import Path
import ast, hashlib, json

root=Path(__file__).resolve().parents[2]
old=root/'artifacts/proc-lib-real-peer-reader-draft4'
new=root/'artifacts/proc-lib-real-peer-reader-draft5'
new.mkdir(exist_ok=False)
sha=lambda b:hashlib.sha256(b).hexdigest().upper()
text=(old/'read.py').read_text(encoding='utf-8')
before="   need(info[A('env')]==L(state[A('captured')]) and info[A('pid')]==parent,'origin complete environment and creator PID')"
after="""   need(info[A('env')]==L(state[A('captured')]),'origin complete single-map environment')
   metadata_pid=info[A('pid')]
   need(isinstance(metadata_pid,tuple) and len(metadata_pid)==6 and metadata_pid[0]=='pid' and metadata_pid[1]==origin_node and metadata_pid[4]==parent[4],'origin fun metadata init PID node and creation')"""
assert text.count(before)==1
text=text.replace(before,after)
before_limit="Limits=['Installed binary/fullmodule"
after_limit="Limits=['Pinned fun_info(pid) returns the init-process PID, not the creator; exact init PID was not separately captured. Its origin node and creation are checked and the complete raw PID is retained.','Installed binary/fullmodule"
assert text.count(before_limit)==1
text=text.replace(before_limit,after_limit)
ast.parse(text)
(new/'read.py').write_text(text,encoding='utf-8',newline='\n')
(new/'source-crosswalk.json').write_bytes((old/'source-crosswalk.json').read_bytes())
readme=(old/'README.md').read_text(encoding='utf-8')
before_history='Revision2 addresses the preserved independent source findings: exact artifact workflow_run.id and expected artifact name; exact extracted file set equal to API-digest ZIP file members; origin fun_info must be a proper unique atom-key/two-field proplist and contain module/type/arity/env/creator PID/name. The original captured environment is exactly the single captured map and creator PID equals its parent. No native observations or SDK execution were performed to establish these source-derived relations.'
after_history='Historical revision2 added exact workflow/artifact/extraction admission and unique fun_info fields. Its extra assertion that fun_info(pid) equals the capture parent was incorrect. Revision4 inherited that assertion; its actual346 refusal remains immutable. Revision5 supersedes only that assertion: pinned erl_bif_info.c returns erts_init_process_id for local functions, initialized by erl_first_process_otp in erl_init.c. The exact single-map captured environment remains required. The metadata PID must belong to the origin node and creation; exact init identity is not separately captured and is an explicit evidence limit.'
assert readme.count(before_history)==1
readme=readme.replace(before_history,after_history)
readme+='\nRevision5 is a source-reviewed correction to the reader, not to native observations or SDK behavior. Pinned OTP commit ad05823719d77c8faee87348ea39513d4e2f99c5: erts/emulator/beam/erl_bif_info.c local fun_info(pid) and erl_init.c initialization. All eight outcome, normal cleanup, source/provenance, digest/extraction and complete environment gates remain unchanged. Dynamic PID values are not normalized or replaced. Reader4 refusal: artifacts/part346-real-peer-independent/reader4-result.json.\n'
(new/'README.md').write_text(readme,encoding='utf-8',newline='\n')
controls={'Status':'SOURCE_ONLY_AST_PASS','Reader4SourceSha256':sha((old/'read.py').read_bytes()),'ExactEnvironmentRetained':True,'OnlyContractCorrection':'fun_info(pid) is init PID, not creator PID','ExactInitIdentityIndependentlyCaptured':False,'Network':False,'Sdk':False,'CaptureExecuted':False}
(new/'source-controls.json').write_text(json.dumps(controls,indent=2)+'\n',encoding='utf-8')
rows=[{'Path':p.name,'Bytes':p.stat().st_size,'Sha256':sha(p.read_bytes())} for p in sorted(new.iterdir())]
freeze={'Status':'PASSIVE_REAL_PEER_READER_REVISION5_SOURCE_ONLY','PreviousSourceFreezeSha256':sha((old/'source-freeze.json').read_bytes()),'OriginalSourceManifestSha256':'C1B3C8BADDCC519B48E557628522E42E5EB31C2CF4055DC343CCBD436EEECD16','Files':rows,'ReaderExecuted':False,'SdkExecuted':False,'NetworkUsed':False,'MainEdited':False}
(new/'source-freeze.json').write_text(json.dumps(freeze,indent=2)+'\n',encoding='utf-8')
print(sha((new/'source-freeze.json').read_bytes()))
