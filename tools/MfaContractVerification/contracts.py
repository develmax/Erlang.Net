from pathlib import Path
import hashlib, json, re
from contract_constants import PIN, VERIFIED

def digest(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest().upper()
def read(path): return json.loads(Path(path).read_bytes())
def write(path, data):
    with Path(path).open('x', encoding='utf-8') as stream:
        json.dump(data, stream, indent=2); stream.write('\n')
def resolve(repository, name):
    path = (repository / name).resolve()
    if not path.is_relative_to(repository.resolve()): raise ValueError('Input escapes repository: ' + str(name))
    return path

def admit(repository, row):
    if set(row) != {'Path', 'Bytes', 'Sha256'}: raise ValueError('Bound input requires Path, Bytes, Sha256.')
    path = resolve(repository, row['Path'])
    if path.stat().st_size != row['Bytes'] or digest(path) != row['Sha256']: raise ValueError('Input hash/size mismatch: ' + row['Path'])
    return path

def valid_mfa(key):
    if not isinstance(key, str): return False
    head, separator, arity = key.rpartition('/')
    module, colon, function = head.partition(':')
    return bool(separator and colon and module and function and arity.isascii() and arity.isdigit())

def load_catalog(repository, path):
    catalog = read(path)
    if catalog.get('SchemaVersion') != 1 or catalog.get('PinnedOtpCommit') != PIN: raise ValueError('Unsupported schema or OTP pin.')
    inventory = read(admit(repository, catalog['RegisteredInventory']))
    ledger = read(admit(repository, catalog['CompatibilityLedger']))
    if ledger['Commit'] != PIN: raise ValueError('Ledger pin mismatch.')
    registered = {f"{x['Module']}:{x['Function']}/{x['Arity']}" for x in inventory}
    statuses = {f"{x['Module']}:{x['Function']}/{x['Arity']}": x['Status'] for x in ledger['Entries']}
    if len(registered) != len(inventory) or set(statuses) != registered: raise ValueError('Registry/ledger identities do not match.')
    families = {}; mfas = {}; fixtures = {}
    for row in catalog['Descriptors']:
        descriptor = read(admit(repository, row))
        if descriptor.get('SchemaVersion') != 1 or descriptor.get('PinnedOtpCommit') != PIN: raise ValueError('Descriptor schema/pin mismatch.')
        family = descriptor['Id']
        if family in families: raise ValueError('Duplicate family: ' + family)
        families[family] = []
        for mfa in descriptor['Mfas']:
            key = mfa['Id']
            if not valid_mfa(key) or key in mfas: raise ValueError('Invalid/duplicate MFA: ' + key)
            if key not in registered and mfa.get('Registration') != 'candidate': raise ValueError('Unknown registered MFA: ' + key)
            if type(mfa['CoverageCompleteReviewed']) is not bool: raise ValueError('Coverage review must be explicit boolean.')
            mfas[key] = dict(mfa, Family=family); families[family].append(key)
        for fixture in descriptor['Fixtures']:
            key = fixture['Id']
            if not re.fullmatch(r'[A-Za-z0-9_-]+', key) or key in fixtures: raise ValueError('Invalid/duplicate fixture id: ' + key)
            if not fixture['Mfas'] or not set(fixture['Mfas']) <= set(families[family]): raise ValueError('Fixture MFA binding absent/unknown.')
            if fixture['CompilerPolicy'] != 'default': raise ValueError('Unsupported compiler policy, no silent default.')
            if type(fixture['TrapExits']) is not bool or not 1 <= fixture['DeadlineSeconds'] <= 60: raise ValueError('Invalid caller/deadline.')
            fixtures[key] = dict(fixture, Family=family)
    for key in mfas:
        if not any(key in f['Mfas'] for f in fixtures.values()): raise ValueError('Descriptor has no fixture: ' + key)
    return catalog, registered, statuses, families, mfas, fixtures

def select(registered, statuses, families, mfas, fixtures, explicit, family, queue, all_registered):
    if sum(bool(x) for x in [explicit, family, queue, all_registered]) != 1: raise ValueError('Select exactly one of --mfa, --family, --queue pending, --all.')
    if explicit:
        selected = list(dict.fromkeys(explicit))
        unknown = set(selected) - registered - set(mfas)
        if unknown: raise ValueError('Unknown MFA selection: ' + ','.join(sorted(unknown)))
    elif family:
        if family not in families: raise ValueError('Unknown family: ' + family)
        selected = families[family][:]
    elif queue: selected = sorted(x for x in registered if statuses[x] != VERIFIED)
    else: selected = sorted(registered)
    if not selected: raise ValueError('Empty selection is refused.')
    chosen = [f for f in fixtures.values() if set(f['Mfas']) & set(selected)]
    return selected, chosen
