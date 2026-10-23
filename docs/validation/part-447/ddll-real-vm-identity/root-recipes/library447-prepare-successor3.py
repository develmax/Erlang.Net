from pathlib import Path
import hashlib,json,shutil
old=Path('artifacts/library447-probe2');new=Path('artifacts/library447-probe3');assert not new.exists();new.mkdir()
for f in old.iterdir():
 if f.is_file():shutil.copyfile(f,new/f.name)
f=new/'beam_ddll_identity447.erl';s=f.read_text().replace('save_ref/1,wire/1','retain_wire/1,save_ref/1,wire/1');s=s.replace('save_wire(Key) ->','retain_wire(Key) -> put({external447,Key},decode(get({wire447,Key}))),true.\nsave_wire(Key) ->');f.write_text(s)
f=new/'Program.cs';s=f.read_text();needle='await Step("final-kernel-reset","stop_node");';assert s.count(needle)==1
s=s.replace(needle,needle+'''
 await Step("retain-real-prior-external-epoch","retain_wire",Term.A("restart"));
 await Step("same-name-epoch-collision","start_node",Term.A("ddll447local@loopback"));
 await Step("prior-epoch-wire-after-same-name","demon_wire",Term.A("restart"));
 await Step("retained-internal-after-same-name","demon_saved",Term.A("restart"));
 await Step("queue-zero-after-same-name","waiting");
 await Step("same-name-new-ref","save_ref",Term.A("new_epoch"));
 await Wires("new_epoch");await Step("same-name-new-wire","save_wire",Term.A("new_epoch"));
 await Step("same-name-new-roundtrip","demon_wire",Term.A("new_epoch"));
 await Step("queue-zero-after-new-epoch","waiting");
 await Step("last-kernel-reset","stop_node");''')
s=s.replace('or "prior-epoch-wire-after-next-admission")','or "prior-epoch-wire-after-next-admission" or "prior-epoch-wire-after-same-name")')
s=s.replace('or "retained-internal-after-next-admission")','or "retained-internal-after-next-admission" or "retained-internal-after-same-name" or "same-name-new-roundtrip")')
s=s.replace('or "final-kernel-reset")','or "final-kernel-reset" or "last-kernel-reset")')
s=s.replace('if(name.StartsWith("queue-one-"))','if(name=="same-name-epoch-collision")DriverParentHarnessAccess423.Require(value.Equals(Term.Tuple(Term.A("ddll447local@loopback"),Term.I(5),Term.A("true"))));\n if(name.StartsWith("queue-one-"))')
f.write_text(s)
rows=[{'Path':p.name,'Sha256':hashlib.sha256(p.read_bytes()).hexdigest().upper()} for p in sorted(new.iterdir()) if p.is_file() and p.name!='reviewed-successor.json']
(new/'reviewed-successor.json').write_text(json.dumps({'Previous':'artifacts/library447-probe2','Reason':'Add actual retained external epoch and same-name re-admission, asserting creation4 is skipped to5 and fresh reference roundtrip remains valid. No handshake/reconnect transport claim. Probe2 is pre-execution only.','Files':rows},indent=2)+'\n')
