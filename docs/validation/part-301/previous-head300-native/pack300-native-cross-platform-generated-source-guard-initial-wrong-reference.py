from pathlib import Path
import hashlib,json,subprocess
a=Path('artifacts');head='c2d2cc304d03201dd52592c77d0389062aea0268';lp=a/'pack300-native-38727353087-independent-envelope-and-sdk-guard.json';linux=json.loads(lp.read_bytes());key='docs/validation/part-300/root-artifacts/root-part299-main-native-guard.json';raw=subprocess.check_output(['git','show',head+':'+key]);windows=json.loads(raw)
assert linux['SdkSixArgumentPassed'] and windows['ActualGeneratedCSharpRoutePassed'] and linux['GeneratedCSharpSha256']==windows['GeneratedCSharpSha256']
r={'Status':'ACTUAL_WINDOWS_AND_LINUX_NATIVE_TOOL_GENERATED_CSHARP_SHA_EXACT','Head':head,'GeneratedCSharpSha256':linux['GeneratedCSharpSha256'],'LinuxIndependentGuardSha256':hashlib.sha256(lp.read_bytes()).hexdigest().upper(),'WindowsPublishedGuardPath':key,'WindowsPublishedGuardSha256':hashlib.sha256(raw).hexdigest().upper(),'Original13SdkCompared':False,'GeneralNifAbiComplete':False,'NativeExecutedByVerifier':False}
p=a/'pack300-native-cross-platform-generated-source-guard.json';assert not p.exists();p.write_text(json.dumps(r,indent=2)+'\n',encoding='utf-8');print(r['Status'])
