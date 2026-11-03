// node tools/Validation/check-api-documentation.cjs [source-docs] [output-docs]
const fs = require('node:fs'), path = require('node:path'), crypto = require('node:crypto');
const docs = path.resolve(process.argv[2] || 'docs'), output = path.resolve(process.argv[3] || docs);
const read = file => fs.readFileSync(file, 'utf8');
const sha = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
const ledgerBytes = fs.readFileSync(path.join(docs, 'mfa-compatibility.json'));
const ledger = JSON.parse(ledgerBytes);
const files = JSON.parse(read(path.join(output, 'api/generated-files.json')));
const escape = value => value.replaceAll('\r', ' ').replaceAll('\n', ' ').replaceAll('|', '\\|').replaceAll('<', '&lt;').replaceAll('>', '&gt;');
const failures = [], rows = new Map(), cache = new Map(), evidenceFiles = new Set();
const requireCheck = (condition, message) => { if (!condition) failures.push(message); };
const display = status => status === 'Verified compatible' ? '100% compatible' : status === 'Needs verification' ? 'Needs verification for 100% compatibility' : status;
function link(from, url) {
    if (/^(?:[a-z][\w+.-]*:|\/\/)/i.test(url)) return null;
    const index = url.indexOf('#'), name = index < 0 ? url : url.slice(0, index);
    return {file:name ? path.resolve(path.dirname(from), decodeURIComponent(name)) : from, anchor:index < 0 ? '' : decodeURIComponent(url.slice(index + 1))};
}
function anchors(file) {
    if (cache.has(file)) return cache.get(file);
    const text = read(file), values = new Set(), counts = {};
    for (const match of text.matchAll(/^#{1,6} (.+)\r?$/gm)) {
        const base = match[1].trim().toLowerCase().replace(/[^\p{L}\p{N}\p{M}_\-\s]/gu, '').replace(/ /g, '-');
        const count = counts[base] || 0; counts[base] = count + 1;
        values.add(base + (count ? '-' + count : ''));
    }
    for (const match of text.matchAll(/<a id="([^"]+)">/g)) values.add(match[1]);
    cache.set(file, values);
    return values;
}
let localLinks = 0, maximumBytes = 0;
for (const relative of files) {
    const file = path.join(output, relative), text = read(file);
    maximumBytes = Math.max(maximumBytes, Buffer.byteLength(text));
    requireCheck(Buffer.byteLength(text) <= 24 * 1024, 'Oversized page: ' + relative);
    for (const match of text.matchAll(/!?\[[^\]\n]*\]\(([^\s)]+)\)/g)) {
        const target = link(file, match[1]); if (!target) continue; localLinks++;
        requireCheck(fs.existsSync(target.file), 'Missing target: ' + relative + ' -> ' + match[1]);
        if (target.anchor && fs.existsSync(target.file)) requireCheck(anchors(target.file).has(target.anchor), 'Missing anchor: ' + relative + ' -> ' + match[1]);
    }
    for (const row of text.split('\n').filter(line => line.startsWith('| `'))) {
        const cells = row.split(/(?<!\\)\|/).slice(1,-1).map(cell => cell.trim());
        const key = cells[0].replace(/^`|`$/g, '');
        requireCheck(!rows.has(key), 'Duplicate command: ' + key);
        rows.set(key, {cells,file});
    }
}
function evidence(start) {
    const result = [], visited = new Set(); let file = start;
    while (file) {
        requireCheck(!visited.has(file), 'Evidence navigation cycle: ' + file);
        if (visited.has(file)) break;
        visited.add(file);
        const text = read(file);
        for (const match of text.matchAll(/^- \[([^\]]+)\]\(([^)]+)\) `([A-Fa-f0-9]{64})`: (.*)$/gm)) {
            const target = link(file, match[2]);
            result.push({Kind:match[1], Path:target.file, Sha256:match[3], Scope:match[4]});
        }
        const next = text.match(/\[Next\]\(([^)]+)\)/);
        file = next ? link(file, next[1]).file : null;
    }
    return result;
}
for (const entry of ledger.Entries) {
    const key = entry.Module + ':' + entry.Function + '/' + entry.Arity, row = rows.get(key);
    requireCheck(!!row, 'Missing command: ' + key); if (!row) continue;
    requireCheck(row.cells[1] === escape(display(entry.Status)) && row.cells[2] === escape(entry.Scope) && row.cells[4] === escape(entry.Limits.join('; ')), 'Changed contract/status/limits: ' + key);
    const reference = row.cells[3].match(/^\[Full evidence\]\(([^)]+)\)$/);
    const actual = reference ? evidence(link(row.file, reference[1]).file) : [];
    requireCheck(actual.length === entry.Evidence.length, 'Evidence count differs: ' + key);
    for (let i = 0; i < entry.Evidence.length; i++) {
        const expected = entry.Evidence[i], found = actual[i], source = path.resolve(docs, expected.Path);
        requireCheck(found && found.Kind === escape(expected.Kind) && found.Path === source && found.Sha256 === expected.Sha256 && found.Scope === escape(expected.Scope), 'Evidence changed or reordered: ' + key + ':' + i);
        const identity = source + ':' + expected.Sha256.toUpperCase();
        if (!evidenceFiles.has(identity)) {
            evidenceFiles.add(identity);
            requireCheck(fs.existsSync(source) && sha(fs.readFileSync(source)).toUpperCase() === expected.Sha256.toUpperCase(), 'Evidence bytes differ: ' + expected.Path);
        }
    }
}
requireCheck(rows.size === ledger.Entries.length, 'Extra commands in generated output');
requireCheck(sha(fs.readFileSync(path.join(docs,'mfa-compatibility.json'))) === sha(ledgerBytes), 'Ledger changed during verification');
const report = {commands:rows.size, modules:new Set(ledger.Entries.map(e=>e.Module)).size, pages:files.length, localLinks, maximumBytes, evidenceFiles:evidenceFiles.size, ledgerSha256:sha(ledgerBytes), failures};
fs.mkdirSync('artifacts/api-doc-split', {recursive:true});
fs.writeFileSync('artifacts/api-doc-split/verification.json', JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify(report,null,2));
process.exitCode = failures.length ? 1 : 0;
