from pathlib import Path
import shutil,json
old=Path('artifacts/library448-probe1');new=Path('artifacts/library448-probe2');assert not new.exists();new.mkdir()
for f in old.iterdir():
 if f.is_file():shutil.copyfile(f,new/f.name)
p=new/'run-root.py';s=p.read_text();s=s.replace("installed=Path(ad['InstalledOtpRoot']);execution=", "installed=Path(ad['InstalledOtpRoot']);epmd_candidates=list(installed.glob('erts-*/bin/epmd.exe'));assert len(epmd_candidates)==1;epmd_executable=epmd_candidates[0];execution=")
s=s.replace("str(installed/'bin/epmd.exe')","str(epmd_executable)")
s=s.replace("forced=True;epmd.terminate();epmd.wait(timeout=10)","forced=True\n    try:epmd.terminate();epmd.wait(timeout=10)\n    except Exception as cleanup_error:failures.append('EPMD forced settle: '+repr(cleanup_error))")
p.write_text(s)
(new/'reviewed-successor.json').write_text(json.dumps({'Previous':'artifacts/library448-probe1','Reason':'Preserve preparation refusal from wrong epmd path. Source-reviewed successor uses admitted exact erts epmd, role-independent old-generation snapshot plus same-current-handler inert input and positive real input, actual peer-retained reference, distribution-delivered peer reference, observed node absence and zero output queues, independent EPMD cleanup aggregation. Root applies reviewed source deltas before any successor execution; every resulting source bound in runner.'},indent=2)+'\n')
