from pathlib import Path
import hashlib,json,subprocess
a=Path('artifacts');head='e1d8a999faac113d98ebf2d4471f80ef30daf84a';lp=a/'pack308-native-38763668519-independent-envelope-and-sdk-guard.json';linux=json.loads(lp.read_bytes());key='docs/validation/part-308/root-artifacts/root-part308-main-native-guard.json';raw=subprocess.check_output(['git','show',head+':'+key]);windows=json.loads(raw)
assert linux['SdkSixArgumentPassed'] and windows['ActualGeneratedCSharpRoutePassed'] and linux['GeneratedCSharpSha256']==windows['GeneratedCSharpSha256']
assert windows['PublicLoadNifRegistered'] and not windows['DefaultNativeLoadInstructionHandlerInstalled'] and not windows['GeneralNifAbiComplete']
r={'Status':'ACTUAL_WINDOWS_AND_LINUX_NATIVE_TOOL_GENERATED_CSHARP_SHA_EXACT','Head':head,'GeneratedCSharpSha256':linux['GeneratedCSharpSha256'],'LinuxIndependentGuardSha256':hashlib.sha256(lp.read_bytes()).hexdigest().upper(),'WindowsPublishedGuardPath':key,'WindowsPublishedGuardSha256':hashlib.sha256(raw).hexdigest().upper(),'Original13SdkCompared':False,'GeneralNifAbiComplete':False,'NativeExecutedByVerifier':False}
p=a/'pack308-native-cross-platform-generated-source-guard.json';assert not p.exists();p.write_text(json.dumps(r,indent=2)+'\n',encoding='utf-8');print(r['Status'])
