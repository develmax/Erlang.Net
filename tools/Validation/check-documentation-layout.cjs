// Run from the repository root: node tools/Validation/check-documentation-layout.cjs
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const sha = value => crypto.createHash('sha256').update(value).digest('hex');
const read = file => fs.readFileSync(file, 'utf8');
const manifest = JSON.parse(read('docs/history/manifest.json'));
const marker = '<!-- preserved-content -->\n';
const links = /!?\[[^\]\n]*\]\(([^\s)]+)\)/g;
const moves = Object.fromEntries(manifest.licenses.map(item => [item.from, item.to]));
const failures = [];
const inheritedMissing = [];
let checkedLinks = 0;
function check(condition, message) {
    if (!condition) failures.push(message);
}
function resolve(file, url) {
    if (/^(?:[a-z][\w+.-]*:|\/\/|\/)/i.test(url)) return null;
    const index = url.search(/[?#]/);
    const name = index < 0 ? url : url.slice(0, index);
    const fragment = index < 0 ? '' : url.slice(index);
    return {
        file: name ? path.posix.normalize(path.posix.join(path.posix.dirname(file), decodeURI(name))) : file,
        fragment
    };
}
function slug(text) {
    return text.toLowerCase().replace(/<[^>]+>/g, '').replace(/[^\p{L}\p{N}\p{M}_\-\s]/gu, '').replace(/ /g, '-');
}
const anchorCache = new Map();
function hasAnchor(file, fragment) {
    if (!fragment.startsWith('#')) return true;
    if (!anchorCache.has(file)) {
        const text = read(file), anchors = new Set(), counts = {};
        for (const match of text.matchAll(/^#{1,6} (.+)\r?$/gm)) {
            const base = slug(match[1].trim()), count = counts[base] || 0;
            counts[base] = count + 1;
            anchors.add(base + (count ? '-' + count : ''));
        }
        for (const match of text.matchAll(/<a\s+(?:id|name)="([^"]+)"/g)) anchors.add(match[1]);
        anchorCache.set(file, anchors);
    }
    return anchorCache.get(file).has(decodeURI(fragment.slice(1)));
}
for (const source of manifest.sources) {
    let restored = '';
    for (const chunk of source.chunks) {
        const text = read(chunk.file), index = text.indexOf(marker);
        check(index >= 0, 'Missing preservation marker: ' + chunk.file);
        const payload = text.slice(index + marker.length);
        check(sha(payload) === chunk.payloadSha256, 'Archive payload changed: ' + chunk.file);
        const reverse = new Map(chunk.replacements);
        const raw = payload.replace(/(!?\[[^\]\n]*\]\()([^\s)]+)(\))/g,
            (all, start, url, end) => start + (reverse.get(url) || url) + end);
        check(sha(raw) === chunk.sha256, 'Cannot reconstruct original chunk: ' + chunk.file);
        restored += raw;
        check(Buffer.byteLength(text) <= 24 * 1024, 'Archive exceeds 24 KiB: ' + chunk.file);
        const originalLinks = [...raw.matchAll(links)];
        const archivedLinks = [...payload.matchAll(links)];
        check(originalLinks.length === archivedLinks.length, 'Link count changed: ' + chunk.file);
        for (let i = 0; i < archivedLinks.length; i++) {
            const after = resolve(chunk.file, archivedLinks[i][1]);
            if (!after) continue;
            checkedLinks++;
            const before = resolve(source.source, originalLinks[i][1]);
            if (!fs.existsSync(after.file)) {
                const originalTarget = before && (moves[before.file] || before.file);
                if (originalTarget && !fs.existsSync(originalTarget)) {
                    inheritedMissing.push({source: source.source, archive: chunk.file, target: archivedLinks[i][1]});
                } else failures.push('New missing archive target: ' + chunk.file + ' -> ' + archivedLinks[i][1]);
            } else if (after.fragment && after.file.endsWith('.md') && !hasAnchor(after.file, after.fragment)) {
                // Compare with the original snapshot when the target was also split.
                const oldSource = manifest.sources.find(item => item.source === before?.file);
                if (oldSource) failures.push('Unrouted historical anchor: ' + chunk.file + ' -> ' + archivedLinks[i][1]);
                else inheritedMissing.push({source: source.source, archive: chunk.file, target: archivedLinks[i][1], anchor:true});
            }
        }
    }
    check(Buffer.byteLength(restored) === source.bytes && sha(restored) === source.sha256,
        'Original snapshot reconstruction failed: ' + source.source);
}
for (const license of manifest.licenses) {
    const present = fs.existsSync(license.to);
    check((license.local && !present) || (present && sha(fs.readFileSync(license.to)) === license.sha256),
        'License bytes changed: ' + license.to);
    check(!fs.existsSync(license.from), 'License still present at old path: ' + license.from);
}
const planArchive = 'docs/validation/integration-20261103/source-plan-refresh';
for (const name of fs.readdirSync(planArchive)) {
    if (/^[A-F0-9]{64}\.json$/.test(name))
        check(sha(fs.readFileSync(path.posix.join(planArchive, name))) === name.slice(0, 64).toLowerCase(),
            'Archived source plan bytes changed: ' + name);
}
for (const archiveName of ['ordsets-owner-handoff', 'atomic-native-caller', 'native-flat-order-pending']) {
    const ownerArchive = 'docs/validation/integration-20261103/' + archiveName;
    const ownerManifest = JSON.parse(read(path.posix.join(ownerArchive, 'source-archive.json')));
    check(sha(fs.readFileSync(path.posix.join(ownerArchive, 'manifest.json')))
        === ownerManifest.OriginalManifestSha256.toLowerCase(), 'Owner manifest bytes changed: ' + archiveName);
    for (const member of ownerManifest.Files) {
        const packed = fs.readFileSync(path.posix.join(ownerArchive, member.Archive));
        const raw = require('node:zlib').gunzipSync(packed);
        check(sha(packed) === member.ArchiveSha256.toLowerCase()
            && raw.length === member.Bytes && sha(raw) === member.Sha256.toLowerCase(),
            'Owner source archive bytes changed: ' + member.Path);
    }
}
const flatAuditRoot = 'docs/validation/integration-20261103/native-flat-order-pending/';
const flatAudit = JSON.parse(read(flatAuditRoot + 'source-classification.json'));
check(sha(require('node:zlib').gunzipSync(fs.readFileSync(flatAuditRoot + 'pinned-ordsets.erl.gz')))
    === flatAudit.PinnedOrdsetsSourceSha256.toLowerCase(), 'Pinned ordsets source archive changed');
const patternRoot = 'docs/validation/integration-20261103/string-pattern-review/';
const patternManifest = JSON.parse(read(patternRoot + 'manifest.json'));
for (const member of patternManifest.Files) {
    const packed = fs.readFileSync(patternRoot + member.Archive);
    const raw = require('node:zlib').gunzipSync(packed);
    check(sha(packed) === member.ArchiveSha256.toLowerCase()
        && raw.length === member.Bytes && sha(raw) === member.Sha256.toLowerCase(),
        'Pattern-search archive changed: ' + member.Path);
}
const nativeAllRoot = 'docs/validation/integration-20261103/native-flat-order-all-apis/';
const nativeAllArchive = JSON.parse(read(nativeAllRoot + 'archive.json'));
check(sha(fs.readFileSync(nativeAllRoot + nativeAllArchive.Archive)) === nativeAllArchive.Sha256.toLowerCase()
    && sha(fs.readFileSync(nativeAllRoot + nativeAllArchive.Manifest)) === nativeAllArchive.ManifestSha256.toLowerCase(),
    'Supplemental native all-API archive changed');
const entryFiles = ['AGENTS.md', 'docs/PROGRESS.md', 'docs/NEXT_STEPS.md', 'docs/COMPATIBILITY.md', 'docs/DECISIONS.md', 'docs/decisions/index.md',
    ...fs.readdirSync('docs/decisions').filter(file => file.endsWith('.md') && file !== 'index.md').map(file => 'docs/decisions/' + file),
    'docs/validation/integration-20261103/README.md',
    'docs/validation/integration-20261103/string-pattern-review/README.md',
    'docs/validation/integration-20261103/native-flat-order-all-apis/README.md',
    'docs/validation/integration-20261103/atomic-native-caller/README.md',
    'docs/validation/integration-20261103/math-ci-41638368916/README.md',
    'docs/validation/integration-20261103/ordsets-owner-handoff/README.md',
    'docs/validation/integration-20261103/native-flat-order-pending/README.md',
    'docs/ORDSETS_COMPATIBILITY.md',
    'docs/validation/integration-20261103/math-ci-41631118602/README.md',
    'docs/changelog/0102.md', 'docs/changelog/0103.md',
    'docs/history/index.md', 'docs/documentation-layout.md', 'docs/license-audit.md', 'licenses/README.md',
    ...manifest.sources.map(item => path.posix.dirname(item.chunks[0].file) + '/index.md')];
for (const file of entryFiles) {
    for (const match of read(file).matchAll(links)) {
        const link = resolve(file, match[1]);
        if (!link) continue;
        checkedLinks++;
        check(fs.existsSync(link.file), 'Missing entry-page target: ' + file + ' -> ' + match[1]);
        if (fs.existsSync(link.file) && link.file.endsWith('.md')) {
            check(hasAnchor(link.file, link.fragment), 'Missing entry-page anchor: ' + file + ' -> ' + match[1]);
        }
    }
}
const report = {
    restoredSources: manifest.sources.map(item => ({file:item.source, bytes:item.bytes, volumes:item.chunks.length})),
    checkedLinks, inheritedMissing, failures
};
fs.mkdirSync('artifacts/documentation-reorganization', {recursive:true});
fs.writeFileSync('artifacts/documentation-reorganization/verification.json', JSON.stringify(report, null, 2) + '\n');
console.log(JSON.stringify({restoredSources:report.restoredSources, checkedLinks, inheritedMissing:inheritedMissing.length, failures}, null, 2));
process.exitCode = failures.length ? 1 : 0;
