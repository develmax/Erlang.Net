from pathlib import Path
import json,hashlib
R=Path.cwd();C=R/'artifacts/guard345-candidate';O=R/'artifacts/guard345-verified-source-independent'
sha=lambda b:hashlib.sha256(b).hexdigest().upper()
prior=(O/'current-verified-source-review.json').read_bytes();assert sha(prior)=='B699617F8CD39D9A3A6DF209D6A591EE9E24E252C19F9CBE9F4AD8AB8B8D4B76'
review=json.loads(prior);builder='src/Erlang.Compiler/SourceCallBindingBuilder.cs';old=R/'artifacts/guard345-formatted-source/source'/builder
before=old.read_text(encoding='utf-8');after=(C/builder).read_text(encoding='utf-8')
assert after==before.replace('    public void Import(string module, Term signatures, int offset)\n    {\n','    public void Import(string module, Term signatures, int offset)\n    {\n        ModuleNameValidation.Validate(module);\n')
assert sha((C/builder).read_bytes())=='D0BE717064BC6DA794886E51BE32E8ED0CF175E00AEB6BD3569BB084B7343251'
for row in review['VerifiedSourceFiles']:
 if row['Path']!=builder:assert sha((C/row['Path']).read_bytes())==row['Sha256']
original_freeze=json.loads((R/'artifacts/guard345-formatted-source/source-freeze.json').read_bytes())
for row in original_freeze['Files']:
 if row['Path']!=builder:assert sha((C/row['Path']).read_bytes())==row['Sha256']
deps=['ModuleNameValidation.cs','ModuleNameParameters.cs','ModuleNameDiagnostics.cs','CompilerDiagnosticCodes.cs','CompileException.cs']
for name in deps:assert (C/'src/Erlang.Compiler'/name).read_bytes()==(R/'src/Erlang.Compiler'/name).read_bytes()
paths={r['Path'] for r in review['VerifiedSourceFiles']}|{'src/Erlang.Compiler/'+name for name in deps}
bindings=[{'Path':p,'Sha256':sha((C/p).read_bytes())} for p in sorted(paths)]
actual=R/'artifacts/guard345-focused-result2/source-freeze.json';assert sha(actual.read_bytes())=='EC4EF45A7A7059F8A7843BC38F674AE8C9982F5E209DCEF799F64600641817EC'
focus=R/'artifacts/guard345-focused-result2/report.json';assert sha(focus.read_bytes())=='318AFECD2379BB53299F9BA3C337957181B076618B009507F688536CE46F1FF1'
focus_report=json.loads(focus.read_bytes());assert focus_report['Passed']==41 and focus_report['SourceFreezeSha256']==sha(actual.read_bytes())
supplement={'Status':'PASS_SOURCE_ONLY_GUARD345_IMPORT_MODULE_VALIDATION_DEPENDENCY_REFRESH','PriorSourceReviewSha256':sha(prior),'ActualFocusedSourceFreezeSha256':sha(actual.read_bytes()),'ActualFocusedReportSha256':sha(focus.read_bytes()),'ActualFocusedPassedReportedByProducer':41,'VerifiedSourceFiles':bindings,'SourceDeltaExactlyOneModuleNameValidationCall':True,'OtherTwelveCompilerSourcesExactBE314':True,'AllOtherPreviousTwentyFourBindingsExact':True,'NewDependenciesRawExactCurrentMain':deps,'SemanticScope':'Import module-name validation rejects invalid module atoms before dispatch. Valid supported module names continue unchanged; the five existing erlang handler, guard and term contracts are untouched. ModuleNameValidation uses Unicode rune enumeration, explicit Latin1/visibility/control limits and existing ERL010 diagnostics. This adds compiler admission validation and no new runtime branch to the five APIs.','ExistingFiveClassificationsAndAllOriginalEvidenceMustBePreserved':True,'CompatibilityCounts':{'Verified compatible':5,'Known differences':17,'Needs verification':565},'OriginalComparisonOrNewApiPromotionClaimed':False,'SdkExecutedByReviewer':False,'MainModified':False}
p=O/'current-verified-source-review2.json';p.write_text(json.dumps(supplement,indent=2)+'\n',encoding='utf-8')
proposal={'Status':'PROPOSED_METADATA_BINDING_REFRESH_ONLY','VerifiedSourceFiles':bindings,'AppendSourceReviewToFiveVerifiedOnly':{'Kind':'SourceReview','Path':'validation/part-345/current-verified-source-review2.json','Sha256':sha(p.read_bytes()),'Scope':'Narrow source-only import module-name validation supplement to prior B699 guard345 review. Preserve five verified contracts, prior source/original evidence and5/17/565 statuses.'},'LedgerNotModified':True}
q=O/'binding-update-proposal2.json';q.write_text(json.dumps(proposal,indent=2)+'\n',encoding='utf-8');print(json.dumps({'ReviewSha256':sha(p.read_bytes()),'Bindings':len(bindings),'ProposalSha256':sha(q.read_bytes()),'ActualFocusedReportSha256':sha(focus.read_bytes())}))
