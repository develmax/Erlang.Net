from pathlib import Path

OUT = Path(__file__).resolve().parent
ROOT = OUT.parents[1]
prior = ROOT/'artifacts/queue-all368-source-review1/review.py'
source = prior.read_text()
source = source.replace("artifacts/queue-all368-original-proposal1/tool/profile.json", "artifacts/queue-original368-original1/tool/profile.json")
source = source.replace("'PinnedExportsAndActualRegistrationSetExact38':True,", "'PinnedExportsAndActualRegistrationSetExact38':True,\n 'ActiveSourceInputsCorrected':{'Path':'artifacts/queue-original368-original1/tool/source-inputs.corrected.json','Sha256':sha(ROOT/'artifacts/queue-original368-original1/tool/source-inputs.corrected.json')},\n 'FrozenOriginalProposal2':{'Path':'artifacts/queue-all368-original-proposal2/source-freeze.json','Sha256':sha(ROOT/'artifacts/queue-all368-original-proposal2/source-freeze.json')},")
source = source.replace('This artifact preserves all617', 'This successor binds the active profile2 and source-inputs.corrected.json plus frozen original proposal2. The first source review remains unchanged. This artifact preserves all617')
(OUT/'review.py').write_text(source,encoding='utf-8')
