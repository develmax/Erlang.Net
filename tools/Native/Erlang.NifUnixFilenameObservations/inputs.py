"""Reuse published source identity, installed runtime approval and owned-process checks."""
from pathlib import Path
import hashlib,json,importlib.util,subprocess,os
PIN='ad05823719d77c8faee87348ea39513d4e2f99c5'
H=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
def load(path,name):
 spec=importlib.util.spec_from_file_location(name,path);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m);return m
def verify_source(args,root,tool):
 assert subprocess.check_output(['git','-C',str(root),'rev-parse','HEAD'],text=True,timeout=20).strip()==args.expected_head
 assert not os.environ.get('GITHUB_SHA') or os.environ['GITHUB_SHA']==args.expected_head
 p=tool/'manifest.json';assert H(p)==args.source_manifest_sha256.upper();manifest=json.loads(p.read_bytes());assert manifest['PinnedOtpCommit']==PIN
 for row in manifest['Files']+manifest['Dependencies']+[manifest['Workflow']]:
  q=root/row['Path'];assert q.resolve().is_relative_to(root) and q.stat().st_size==row['Bytes'] and H(q)==row['Sha256']
 return manifest
def verify(args,root,tool):
 manifest=verify_source(args,root,tool)
 identity=load(root/'tools/Runtime/Erlang.SchedulerAffinityOriginalObservations/source_inputs.py','raw_identity')
 common=load(root/'tools/Native/Erlang.NifEntryOriginalObservations/common.py','raw_original_common')
 owned=load(root/'tools/Distribution/Erlang.GuestConnectedSendObservations/owned_process.py','raw_owned')
 runtime,build=common.approve_runtime(args,manifest,identity)
 return identity,common,owned,runtime,build
