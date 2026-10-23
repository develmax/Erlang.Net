from pathlib import Path
import hashlib,json,shutil
old=Path('artifacts/library447-probe1');new=Path('artifacts/library447-probe2');assert not new.exists();new.mkdir()
for f in old.iterdir():
 if f.is_file():shutil.copyfile(f,new/f.name)
f=new/'beam_ddll_identity447.erl';s=f.read_text();s=s.replace('waiting() -> [{_,Count}]=erl_ddll:info(echo_drv,awaiting_unload),Count.','waiting() -> waiting(erl_ddll:info(echo_drv,awaiting_unload),0).\nwaiting([],N)->N;waiting([{_,Count}|Rest],N)->waiting(Rest,N+Count).')
s=s.replace("receive {'DOWN',Mon,process,Kernel,normal}->identity() after 5000->error(kernel_timeout) end.","receive {'DOWN',Mon,process,Kernel,normal}->inactive(5000) after 5000->error(kernel_timeout) end.\ninactive(0)->error(node_reset_timeout);\ninactive(N)->case is_alive() of false->identity();true->receive after 1->inactive(N-1) end end.")
f.write_text(s)
f=new/'ddll_tcp447.erl';s=f.read_text();s=s.replace('stop -> Worker!stop,ok=gen_tcp:send(S,term_to_binary(stopped)),gen_tcp:close(S),halt();',"stop -> M=monitor(process,Worker),Worker!stop,\n                    receive {'DOWN',M,process,Worker,normal}->ok after 5000->error(worker_stop_timeout) end,\n                    ok=gen_tcp:send(S,term_to_binary(stopped)),gen_tcp:close(S),halt();");f.write_text(s)
f=new/'Program.cs';s=f.read_text();s=s.replace('if(!compare)return;','if(!compare)return;\n  ValidateOutcome(name,expected,depth);ValidateOutcome(name,actual,depth);')
s=s.replace('Save("genuine-peer-reference",foreign,foreign,false);','Save("genuine-peer-reference",foreign,foreign,false);\n DriverParentHarnessAccess423.Require(foreign is ReferenceTerm foreignRef&&foreignRef.Node=="ddll447peer@loopback");')
s=s.replace('await Step("foreign-reference-full-stack","demon",foreign);','await Step("foreign-reference-full-stack","demon",foreign);\n Save("peer-monitor-still-queued",peer.Call("waiting"),Term.I(1));')
s=s.replace('await Step("foreign-caller-preserves-queue","waiting");','await Step("queue-one-foreign-caller-preserves","waiting");')
for source,addition in [
 ('Save("returned-reference-cancels-owner",Original("demon_binary",ob),await Managed("demon_binary",mb));','await Step("queue-zero-after-return","waiting");'),
 ('await Step("pre-admission-wire-after-admission","demon_wire",Term.A("inactive"));','await Step("queue-one-after-admission-reject","waiting");'),
 ('await Step("retained-internal-after-admission","demon_saved",Term.A("inactive"));','await Step("queue-zero-after-internal-admission","waiting");'),
 ('await Step("active-wire-local-roundtrip","demon_wire",Term.A("active"));','await Step("queue-zero-after-active-roundtrip","waiting");'),
 ('await Step("prior-epoch-wire-after-reset","demon_wire",Term.A("restart"));','await Step("queue-one-after-reset-reject","waiting");'),
 ('await Step("retained-internal-after-reset","demon_saved",Term.A("restart"));','await Step("queue-zero-after-internal-reset","waiting");')]:
 assert s.count(source)==1;s=s.replace(source,source+'\n '+addition)
validation='''static void ValidateOutcome(string name,Term value,int depth)
{
 if(name.StartsWith("queue-one-"))DriverParentHarnessAccess423.Require(value.Equals(Term.I(1)));
 if(name.StartsWith("queue-zero-"))DriverParentHarnessAccess423.Require(value.Equals(Term.I(0)));
 if(name is "foreign-reference-full-stack" or "peer-rejects-primary-reference" or "pre-admission-wire-after-admission" or "prior-epoch-wire-after-reset" or "prior-epoch-wire-after-next-admission")
 {
  DriverParentHarnessAccess423.Require(value is TupleTerm{Items.Count:4});var failure=(TupleTerm)value;
  DriverParentHarnessAccess423.Require(failure.Items[0].Equals(Term.A("exception"))&&failure.Items[1].Equals(Term.A("error"))&&failure.Items[2].Equals(Term.A("badarg")));
  var frames=Cons.Items(failure.Items[3]).ToArray();DriverParentHarnessAccess423.Require(frames.Length==Math.Min(depth,2));
  if(frames.Length>0)DriverParentHarnessAccess423.Require(frames[0] is TupleTerm frame&&frame.Items[0].Equals(Term.A("erl_ddll"))&&frame.Items[1].Equals(Term.A("demonitor"))&&Cons.Items(frame.Items[2]).Single() is ReferenceTerm);
 }
 if(name is "returned-reference-cancels-owner" or "repeat-completed-reference" or "retained-internal-after-admission" or "active-wire-local-roundtrip" or "retained-internal-after-reset" or "retained-internal-after-next-admission")DriverParentHarnessAccess423.Require(value.Equals(Term.Tuple(Term.A("value"),Term.A("true"))));
 if(name is "kernel-death-node-reset" or "final-kernel-reset")DriverParentHarnessAccess423.Require(value.Equals(Term.Tuple(Term.A("nonode@nohost"),Term.I(0),Term.A("false"))));
}
'''
s=s.replace('static bool Equal(',validation+'static bool Equal(')
# One namespace-level type per file, including the standalone evidence harness.
head,peer=s.split('sealed class Peer447:IDisposable',1);f.write_text(head);(new/'Peer447.cs').write_text('using System.Buffers.Binary;\nusing System.Net.Sockets;\nusing Erlang;\nsealed class Peer447:IDisposable'+peer)
f=new/'run-root.py';s=f.read_text();s=s.replace("forced=False\n    try:process.wait(timeout=10)\n    except subprocess.TimeoutExpired:forced=True;process.terminate();process.wait(timeout=10)\n    for log in logs:log.close()", "forced=False;cleanup_failure=None\n    try:\n     try:process.wait(timeout=10)\n     except subprocess.TimeoutExpired:\n      forced=True;process.terminate()\n      try:process.wait(timeout=10)\n      except subprocess.TimeoutExpired:\n       killed=subprocess.run(['taskkill','/PID',str(process.pid),'/T','/F'],capture_output=True,timeout=15);(group/(role+'.kill.stdout.log')).write_bytes(killed.stdout);(group/(role+'.kill.stderr.log')).write_bytes(killed.stderr);process.wait(timeout=10)\n    except Exception as error:cleanup_failure=repr(error);group_errors.append(role+' cleanup: '+cleanup_failure)\n    finally:\n     for log in logs:log.close()")
f.write_text(s)
rows=[{'Path':p.name,'Sha256':hashlib.sha256(p.read_bytes()).hexdigest().upper()} for p in sorted(new.iterdir()) if p.is_file()]
(new/'reviewed-successor.json').write_text(json.dumps({'Previous':'artifacts/library447-probe1','Reason':'Independent pre-acceptance review: bounded inactive wait, join worker before shutdown ACK, always attempt both original cleanups, explicit nonvacuous queue and error-shape controls, one namespace type per file','Files':rows},indent=2)+'\n')
