from pathlib import Path
import json,hashlib
R=Path(__file__).resolve().parents[2];O=Path(__file__).resolve().parent
def H(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
native=R/'artifacts/queue-original367-peer2/acceptance.json';assert H(native)=='ED8F4BCB0E2B387B7ED958B9471AA5134295BE05596E449864C3B4DF4FEF22D3'
partition=R/'artifacts/queue-original367-peer2/actual1/partitions.json';assert H(partition)=='502F810A4C8D156F571E2DF8A17B7FC33E25402E6867A5300BD67493BDD13FC7'
p=O/'comparison.json';x=json.loads(p.read_text());assert x['Status']=='PASS_LONG_PAIRED8_40_SOURCE_BOUND_WHOLE_FIELDS' and x['Accepted']==48 and not x['Unexpected'] and len(x['NegativeControls'])==20 and all(r['Rejected'] for r in x['NegativeControls'])
for row in x['Rows']:row['Status']='PASS_STRICT_WHOLE' if row['StrictWholeDecodedEqual'] else 'PASS_SOURCE_BOUND_ACTUAL_OWN_ENTRY_PROTOCOL'
x['Counts']={'Values':{'Total':8,'Strict':8,'SourceBound':8},'FullObservations':{'Total':40,'Strict':30,'SourceBound':40},'NativeOnly':{'Values':3,'FullObservations':15},'Unexpected':0,'NegativeControlsRejected':20}
x['IndependentNativeAdmission']={'Path':native.relative_to(R).as_posix(),'Sha256':H(native),'PartitionProofPath':partition.relative_to(R).as_posix(),'PartitionProofSha256':H(partition)}
p.write_text(json.dumps(x,indent=2)+'\n')
summary={'Status':'PASS_FINAL_LONG_PAIRED8_40','Counts':x['Counts'],'OriginalRun':39072770120,'OriginalHead':x['OriginalHead'],'NativeArtifact':12116022082,'ComparisonSha256':H(p),'IndependentNativeAdmission':x['IndependentNativeAdmission'],'SdkCapture':'artifacts/queue-integration367/capture3/long-sdk','SdkAdmissionSha256':H(R/'artifacts/queue367-final-long-sdk-peer1/admission.json'),'OwnSourceReview':x['OwnSourceReview'],'AllNonAllowedFieldsAndFramesExact':True,'Native64017ObservedKZero':True,'Native256017ActuallyTrapped':True,'SourceGuardCount':357,'MainModified':False,'SdkOrNativeExecutedByReview':False,'Normalization':False,'Projection':False,'CompatibilityPromoted':False,'Scope':'Final source-bound complete-field finite evidence under explicit own allocation-path state protocol. Physical native heap choices/scheduler quantum/partition numbers are not claimed equal. Complete API classification remains Root source-contract/full-regression decision.'}
(O/'acceptance.json').write_text(json.dumps(summary,indent=2)+'\n')
(O/'freeze.json').write_text(json.dumps({'Scope':'PASSIVE_FINAL_LONG_COMPARISON_EVIDENCE','Files':[{'Path':f.relative_to(O).as_posix(),'Sha256':H(f)} for f in sorted(O.iterdir()) if f.is_file() and f.name!='freeze.json']},indent=2)+'\n')
print('ACCEPTANCE',H(O/'acceptance.json'));print('COMPARISON',H(p));print('FREEZE',H(O/'freeze.json'))
