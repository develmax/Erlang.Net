from pathlib import Path
import json
r=Path(__file__).resolve().parent;f=json.loads((r/'source-freeze.json').read_bytes())
for name,method in [('exit-two','ExitTwoPublishes'),('exit-three','ExitThreePublishes')]:
 x=json.loads((r/(name+'-actual-red.json')).read_bytes());assert x['Status']=='FAIL' and len(x['Cases'])==1
 case=x['Cases'][0];assert case['Name']=='DistributionGuestExitPublicRouteHostChecks:'+method and case['Status']=='FAIL';error=case['Exception']
 assert f['ExpectedExceptionType']+': '+f['ExpectedExceptionMessage'] in error and ':line '+str(f['ExpectedMissingPacketAssertionLine']) in error
 assert all(token not in error for token in ['TimeoutException','TypeLoadException','MissingMethodException','MissingMemberException','NullReferenceException'])
 print(name+': meaningful actual missing-packet RED retained')
