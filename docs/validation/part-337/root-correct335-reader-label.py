from pathlib import Path
import json,hashlib
r=Path.cwd();d=r/'artifacts/part335-remote-new-review';p=d/'review-ordinary.py';old=p.read_bytes()
(d/'review-ordinary-initial-stale-status-label.py').write_bytes(old)
s=old.decode().replace('PASS_PUBLISHED334_ORIGINAL6114_FD8_PREVIOUS6085_ORDERED_AND_FORTY_THREE_NEW_METADATA_LABEL_ORIGINAL_AND_SDK_CASES','PASS_PUBLISHED335_ORIGINAL6114_FD8_PREVIOUS6085_ORDERED_AND_TWENTY_NINE_STOP_SYSTEM_ORIGINAL_AND_SDK_CASES').replace('historical-differences.json','historical-differences2.json').replace('independent-guard.json','independent-guard2.json')
# Keep the prior334 input guard filename unchanged.
s=s.replace('part334-remote-new-review/ordinary-semantic-independent-guard2.json','part334-remote-new-review/ordinary-semantic-independent-guard.json')
p.write_bytes(s.encode())
(d/'reader-label-correction.json').write_text(json.dumps(dict(Reason='Descriptive Status retained334 metadata wording while actual335 assertions correctly checked6114/29stopcases. Preserve oldsource/reports, correctlabelonly andnewguardfilenames.',PriorReaderSha256=hashlib.sha256(old).hexdigest().upper(),OriginalOrSdkRerun=False),indent=2)+'\n',encoding='utf-8')
