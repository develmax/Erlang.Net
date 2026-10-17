"""Freeze narrow source-only successor4; actual reader3 refusal retained."""
from pathlib import Path
import common as m

directory = Path(__file__).resolve().parent
repository = directory.parent.parent
previous = directory.parent / 'part353-ci-ordinary-reader-proposal3'
unchanged = ('common.py', 'review-ordinary.py', 'root-transport-with-log.py', 'root-discover-runs.py')
for name in unchanged:
    m.need((directory/name).read_bytes() == (previous/name).read_bytes(), 'undeclared operational source delta')
m.need(m.sha(directory/'publication-differences.json') == 'B1CBDAD8BBBE706E43F29D9EF1AFDBF5B4D531FB5A818C4290037C5D22F6E893', 'inventory raw copy')
m.need(m.sha(directory/'reader3-refusal-history.json') == '74F661CABA435798235F28E27E937C2FBBDC6B53DB5EF62A09D244682D47134B', 'actual refusal raw copy')
files = unchanged + ('review-ci.py', 'check-closure-admission.py', 'closure-source-controls.json',
                    'publication-differences.json', 'reader3-refusal-history.json', 'README.md', 'freeze-source.py')
bindings = ('artifacts/part353-ci-ordinary-reader-proposal3/source-freeze.json',
    'artifacts/part353-remote-readers-source-independent/source3-report.json',
    'artifacts/root-part353-closure-publication-differences.json',
    'artifacts/root-part353-remote-independent/ci-reader3-refusal-history.json',
    'artifacts/root-part353-final-stage/staged-guard.json',
    'artifacts/scheduler-affinity353-composition-plan2/composition-plan.json')
value = {'Status':'SOURCE_ONLY_READER4_CLOSED134_UNCHANGED_PARENT_TEXT_REPRESENTATION_REQUIRES_PEER',
    'Files':[{'Path':name,'Bytes':(directory/name).stat().st_size,'Sha256':m.sha(directory/name)} for name in files],
    'Bindings':[{'Path':path,'Bytes':(repository/path).stat().st_size,'Sha256':m.sha(repository/path)} for path in bindings],
    'ChangedOperationalSource':['review-ci.py'],'RawUnchangedOperationalSources':list(unchanged),
    'StableRows':3106,'ClosedDifferences':142,'UnchangedNonCSharpUtf8LfParentRawExact':134,'PriorClosedStagedDifferences':8,
    'ActualPriorReaderRefusalIsCiTestFailure':False,'FullRemoteReader4Executed':False,
    'SdkOrNativeExecuted':False,'NetworkUsed':False,'MainOrGitMutated':False}
m.fresh(directory/'source-freeze.json',value)
print(m.sha(directory/'source-freeze.json'))
