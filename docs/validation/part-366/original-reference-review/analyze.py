import pathlib,hashlib,json,importlib.util,subprocess
R=pathlib.Path(__file__).resolve().parents[2]
OUT=pathlib.Path(__file__).resolve().parent
HEAD="9661925d5bedd6b2a216fefdeb0f7b3e80bc0c5d"
TOOL=R/"artifacts/proplists-protocol366-original1/tool"
DOWNLOAD=R/"artifacts/proplists-protocol366-original1/download/run-39066726283"
CAP=DOWNLOAD/"artifact-12114150364/capture"
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def require(v):
 if not v:raise ValueError("Passive evidence admission failed")
def serial(v):
 if isinstance(v,bytes):return {"BytesHex":v.hex().upper()}
 if isinstance(v,(tuple,list)):return [serial(x)for x in v]
 if isinstance(v,frozenset):return sorted([serial(x)for x in v],key=repr)
 return v
manifest_rows=json.loads((DOWNLOAD/"download-manifest.json").read_bytes())
for row in manifest_rows:
 f=DOWNLOAD/row["Path"];require(f.stat().st_size==row["Bytes"]and sha(f)==row["Sha256"])
committed=subprocess.run(["git","show",HEAD+":tools/Erlang.ProplistFamilyObservations/source-inputs.corrected.json"],cwd=R,check=True,stdout=subprocess.PIPE).stdout
inputs=json.loads((CAP/"inputs.json").read_bytes());require(hashlib.sha256(committed).hexdigest().upper()==inputs["SourceInputsSha256"])
plan=json.loads(committed)
for row in plan["ToolInputs"]:
 f=TOOL/row["Path"];require(f.stat().st_size==row["Bytes"]and sha(f)==row["Sha256"])
parser=TOOL/"passive_etf_family.py";spec=importlib.util.spec_from_file_location("trusted_local_reference_decoder",parser);decoder=importlib.util.module_from_spec(spec);spec.loader.exec_module(decoder)
def decode(p):return decoder.decode_exact(p.read_bytes())
folder=CAP/"reference_layout";returned=decode(folder/"returned.etf");stacks=decode(folder/"stack-observations.etf")
require(returned[0]==stacks[0]=="list");rows=returned[1];require([x[1][0][1]for x in rows]==["word_counts","map_aliases","roundtrip_aliases"])
require([x[1][0]for x in stacks[1]]==[("atom","default")]+[("integer",x)for x in [0,1,2,8]])
require(all(group[1][1]==returned for group in stacks[1]))
def values(row):require(row[1][1][1][0]==("atom","ok"));return row[1][1][1][1][1]
counts=[]
for item in values(rows[0]):
 n,equal,wire,h=item[1];ref=decoder.decode_exact(wire[1]);require(ref[0]=="reference"and len(ref[3])==n[1]and ref[3]==(7,)+(0,)*(n[1]-1));require(equal==("atom","true"));counts.append({"StoredWordCount":n[1],"EqualsCount1":True,"OriginalHash":h[1],"ReferenceWireHex":wire[1].hex().upper()})
require(len({x["OriginalHash"]for x in counts})==5)
lookup=[{"StoredWordCount":x[1][0][1],"Observed":x[1][1][1]}for x in values(rows[1])];require([x["Observed"]for x in lookup]==["present"]+["missing"]*4)
roundtrip=[{"StoredWordCount":x[1][0][1],"EqualsCount1":x[1][1][1],"HashEqualsCount1":x[1][2][1]}for x in values(rows[2])];require([x["EqualsCount1"]for x in roundtrip]==["true"]*5 and[x["HashEqualsCount1"]for x in roundtrip]==["true"]+["false"]*4)
processes=[]
for group in ["reference_layout","map_hashes"]:
 for stage in ["compile","observe"]:
  f=CAP/group/(stage+".process.json");a=json.loads(f.read_bytes());require(a["ExitCode"]==0 and a["ParentReaped"]and a["StreamsClosed"]and a["CleanupFailure"]is None and not a["TerminationRequested"]);processes.append({"Group":group,"Stage":stage,"Sha256":sha(f),"ExitCode":a["ExitCode"],"ElapsedSeconds":a["ElapsedSeconds"],"NormalCleanup":True})
full={"Returned":serial(returned),"StackProfiles":serial(stacks),"Metadata":serial(decode(folder/"metadata.etf"))};(OUT/"decoded-reference.json").write_text(json.dumps(full,indent=2)+"\n",encoding="utf-8")
report={"Status":"PASS_PASSIVE_REFERENCE_OBSERVATION_ADMISSION_NOT_WHOLE_WORKFLOW_PASS","Run":39066726283,"Head":HEAD,"PinnedOtp":inputs["PinnedOtpCommit"],"DownloadManifestRowsRehashed":len(manifest_rows),"CorrectedCommittedManifestSha256":inputs["SourceInputsSha256"],"LocalToolInputsRehashed":len(plan["ToolInputs"]),"DecoderSha256":sha(parser),"ReferenceDiagnosticRows":3,"FullProfileRows":15,"Profiles":["default",0,1,2,8],"AllProfilesExactlyEqualReturned":True,"WordCounts":counts,"HamtLookup":lookup,"Roundtrip":roundtrip,"ProcessReceipts":processes,"RawHashes":{n:sha(folder/n)for n in ["returned.etf","stack-observations.etf","metadata.etf"]},"Implications":["Actual original equality ignores trailing-zero external reference words while its internal hash preserves stored count.","HAMT map containing count1 reference is not found by equal count2..5 external reference aliases in this actual original process.","Count-sensitive managed protocol agrees with source and this observed original diagnostic; do not silently normalize counts.","Hash numbers include process-owned node identity and are not literal cross-runtime oracle values.","Ordinary managed MapTerm dictionary alias semantics and complete from_map compatibility still require explicit classification; this observation does not resolve them."],"WholeWorkflowStatusPreserved":"FAIL_ORIGINAL_CAPTURE_GROUPS_RETAINED: map_hashes passive decoder rejected extended identity-bearing map key; both native workers normal exit0.","MapHashesScope":"No repaired map decoder executed here; Root owns that admission repair and independent map analysis.","ExecutionScope":"Only own passive Python, trusted local passive decoder and read-only local Git show; no downloaded code/native/SDK/network/Main execution or edits."}
(OUT/"report.json").write_text(json.dumps(report,indent=2)+"\n",encoding="utf-8")
print("PASS_REFERENCE_ROWS3_PROFILES15_MANIFEST"+str(len(manifest_rows)))
