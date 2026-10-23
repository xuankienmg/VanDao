// Gom mọi chuỗi tiếng Việt hiển thị trong game (wwwroot JS/HTML + C# phía server; menu native ở Desktop/ tự song ngữ qua WindowSettings.L) thành các "đoạn" để dịch.
// Mỗi đoạn là một dòng chữ người chơi thấy; phần nội suy ${...} / {...} thành {0}, {1}…
// Dùng: node tools/i18n_extract.cjs            → in số đoạn còn thiếu bản dịch, ghi tools/i18n-missing.json
//       node tools/i18n_extract.cjs --all      → ghi toàn bộ đoạn (kể cả đã dịch) vào tools/i18n-missing.json
//       thêm --launcher                        → làm với Launcher (NguoiLuoiTuTienLauncher/, từ điển wwwroot/i18n-en.js của nó)
const fs = require('fs'), path = require('path'), cp = require('child_process');
const ROOT = path.resolve(__dirname, '..');
const VI = /[àáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹđ]/i;
// giữ nguyên ở mọi ngôn ngữ: tên game, nhãn đã song ngữ, tên ngôn ngữ trong ô chọn
const KEEP = new Set(['Người Lười Tu Tiên', 'Người Lười Tu Tiên Launcher', '☯ NGƯỜI LƯỜI TU TIÊN', 'NGƯỜI LƯỜI', 'TU TIÊN',
  'Tiếng Việt', 'Ngôn ngữ · Language', 'Ngôn ngữ / Language', 'Choose the Người Lười Tu Tiên game folder']);
const SKIP = [/(^|\/)i18n[^/]*\.js$/,/fx-data\.js$/, /^wwwroot\/fx-preview\//];

// ---------- chuẩn hoá đoạn (giống hệt i18n.js lúc chạy) ----------
const norm = s => s.replace(/\s+/g, ' ').trim();
const ENT = { amp: '&', lt: '<', gt: '>', quot: '"', apos: "'", nbsp: ' ', middot: '·', hellip: '…', times: '×', rarr: '→', larr: '←', bull: '•', ndash: '–', mdash: '—' };
const decode = s => s.replace(/&#(\d+);/g, (_, n) => String.fromCharCode(+n)).replace(/&#x([0-9a-f]+);/gi, (_, n) => String.fromCharCode(parseInt(n, 16)))
  .replace(/&([a-z]+);/gi, (m, n) => ENT[n.toLowerCase()] ?? m);
const ATTRS = /\b(?:data-tip|title|aria-label|placeholder|alt|data-label|data-confirm)\s*=\s*(?:"([^"]*)"?|'([^']*)'?)/gi;
const PH = i => `\u0001${i}\u0002`;

/** Chuỗi (đã thay nội suy bằng \u0001i\u0002) → các đoạn chữ hiển thị. */
function segments(str, html){
  const out = [];
  const push = t => {
    for (const line of decode(t).split(/\n|\\n/)){
      let s = norm(line);
      if (!s) continue;
      // đánh số lại placeholder trong từng đoạn
      let k = 0; const map = {};
      s = s.replace(/\u0001(\d+)\u0002/g, (_, i) => `{${map[i] ??= k++}}`);
      const lit = s.replace(/\{\d+\}/g, '');
      if (!VI.test(lit)) continue;
      out.push(s);
    }
  };
  if (html || /[<>]|\b(?:data-tip|title|aria-label|placeholder|alt)\s*=/i.test(str)){
    // giá trị thuộc tính hiển thị (kể cả thuộc tính bị cắt giữa hai chuỗi ghép) + chữ giữa các thẻ
    str.replace(ATTRS, (_, d, s) => { push(d ?? s ?? ''); return '\u0003'; })
      .replace(/<[^<>]*>|^[^<>]*>|<[^<>]*$/g, '\u0003')
      .split('\u0003').forEach(push);
  } else push(str);
  return out;
}

// ---------- quét JS (chuỗi, template literal lồng nhau, chuỗi ghép bằng +, bỏ comment/regex) ----------
function scanJs(src, emit){
  let i = 0, prev = '', ph = 0;
  const n = src.length, OPEN = { '}': '{', ')': '(', ']': '[' };
  const ws = () => { while (i < n && /\s/.test(src[i])) i++; };
  function readTemplate(){            // con trỏ ở sau dấu `, trả về nội dung
    let buf = '';
    while (i < n){
      const c = src[i];
      if (c === '\\'){ buf += src.slice(i, i + 2); i += 2; continue; }
      if (c === '`'){ i++; break; }
      if (c === '$' && src[i + 1] === '{'){ i += 2; readCode('}'); buf += PH(ph++); continue; }
      buf += c; i++;
    }
    return unescapeJs(buf);
  }
  function readQuoted(q){
    let buf = '';
    while (i < n && src[i] !== q && src[i] !== '\n'){ if (src[i] === '\\'){ buf += src.slice(i, i + 2); i += 2; } else buf += src[i++]; }
    i++; return unescapeJs(buf);
  }
  const readStr = () => { const q = src[i++]; return q === '`' ? readTemplate() : readQuoted(q); };
  /** Bỏ qua một toán hạng không phải chuỗi: tên, số, (…), lời gọi, truy cập thuộc tính. */
  function skipOperand(){
    ws();
    while ('!-+~'.includes(src[i]) && src[i]) { i++; ws(); }
    if (src[i] === '(' || src[i] === '['){ const c = src[i++]; readCode(c === '(' ? ')' : ']'); }
    else while (i < n && /[A-Za-z0-9_$.]/.test(src[i])) i++;
    for (;;){
      if (src[i] === '?' && src[i + 1] === '.') i += 2;
      if (src[i] === '.'){ i++; while (i < n && /[A-Za-z0-9_$]/.test(src[i])) i++; continue; }
      if (/[A-Za-z0-9_$]/.test(src[i] || '')){ while (i < n && /[A-Za-z0-9_$]/.test(src[i])) i++; continue; }
      if (src[i] === '(' || src[i] === '['){ const c = src[i++]; readCode(c === '(' ? ')' : ']'); continue; }
      break;
    }
  }
  /** Chuỗi và các toán hạng nối sau nó bằng + → một chuỗi có chỗ trống. */
  function readChain(){
    let buf = (prev === '+' ? PH(ph++) : '') + readStr();
    for (;;){
      const save = i; ws();
      if (src[i] !== '+' || src[i + 1] === '+' || src[i + 1] === '='){ i = save; break; }
      i++; ws();
      if (src[i] === '"' || src[i] === "'" || src[i] === '`') buf += readStr();
      else { skipOperand(); buf += PH(ph++); }
    }
    emit(buf); prev = 'x';
  }
  function readCode(close){           // đọc code tới dấu đóng tương ứng (}, ) hoặc ])
    let depth = 0;
    while (i < n){
      const c = src[i];
      if (c === '/' && src[i + 1] === '/'){ while (i < n && src[i] !== '\n') i++; continue; }
      if (c === '/' && src[i + 1] === '*'){ i = src.indexOf('*/', i + 2); i = i < 0 ? n : i + 2; continue; }
      if (c === '"' || c === "'" || c === '`'){ readChain(); continue; }
      if (c === '/' && /[(,=:[!&|?{};+\-*%<>~^]|^$|return$/.test(prev)){   // regex literal
        i++; let cls = false;
        while (i < n && src[i] !== '\n'){ const d = src[i]; if (d === '\\'){ i += 2; continue; } if (d === '[') cls = true; else if (d === ']') cls = false; else if (d === '/' && !cls) break; i++; }
        i++; while (/[a-z]/i.test(src[i] || '')) i++; prev = 'x'; continue;
      }
      if (close && c === OPEN[close]) depth++;
      if (close && c === close){ if (depth === 0){ i++; prev = 'x'; return; } depth--; }
      if (!/\s/.test(c)){
        if (/[A-Za-z0-9_$]/.test(c)){ let w = ''; while (i < n && /[A-Za-z0-9_$]/.test(src[i])) w += src[i++]; prev = w === 'return' || w === 'typeof' ? 'return' : 'x'; continue; }
        prev = c;
      }
      i++;
    }
  }
  readCode(null);
}
const unescapeJs = s => s.replace(/\\u\{([0-9a-f]+)\}|\\u([0-9a-f]{4})|\\x([0-9a-f]{2})|\\(.)/gi, (m, a, b, c, d) =>
  a || b || c ? String.fromCodePoint(parseInt(a || b || c, 16)) : d === 'n' ? '\n' : d === 't' ? ' ' : d);

// ---------- quét C# (chuỗi thường, $"..." nội suy lồng nhau, @"..." verbatim, chuỗi ghép bằng +) ----------
function scanCs(src, emit){
  let i = 0, ph = 0, prev = '';
  const n = src.length, OPEN = { '}': '{', ')': '(', ']': '[' };
  const ws = () => { while (i < n && /\s/.test(src[i])) i++; };
  const isStr = () => { let j = i; while (src[j] === '$' || src[j] === '@') j++; return src[j] === '"' && !(src[j + 1] === '"' && src[j + 2] === '"'); };
  function readStr(){                 // con trỏ ở $ / @ / ", trả về nội dung
    let interp = false, verb = false;
    while (src[i] === '$' || src[i] === '@'){ if (src[i] === '$') interp = true; else verb = true; i++; }
    i++; let buf = '';
    while (i < n){
      const d = src[i];
      if (verb && d === '"' && src[i + 1] === '"'){ buf += '"'; i += 2; continue; }
      if (!verb && d === '\\'){ const e = src[i + 1]; buf += e === 'n' ? '\n' : e === 't' ? ' ' : e === 'u' ? String.fromCharCode(parseInt(src.slice(i + 2, i + 6), 16)) : e; i += e === 'u' ? 6 : 2; continue; }
      if (d === '"'){ i++; break; }
      if (interp && d === '{'){
        if (src[i + 1] === '{'){ buf += '{'; i += 2; continue; }
        i++; readCode('}'); buf += PH(ph++); continue;
      }
      if (interp && d === '}' && src[i + 1] === '}'){ buf += '}'; i += 2; continue; }
      buf += d; i++;
    }
    return buf;
  }
  function skipOperand(){
    ws();
    while ('!-+~'.includes(src[i]) && src[i]){ i++; ws(); }
    if (src[i] === '('){ i++; readCode(')'); }
    for (;;){
      if (src[i] === '?' && src[i + 1] === '.') i += 2;
      if (/[A-Za-z0-9_.]/.test(src[i] || '')){ i++; continue; }
      if (src[i] === '(' || src[i] === '['){ const c = src[i++]; readCode(c === '(' ? ')' : ']'); continue; }
      break;
    }
  }
  function readChain(){
    let buf = (prev === '+' ? PH(ph++) : '') + readStr();
    for (;;){
      const save = i; ws();
      if (src[i] !== '+' || src[i + 1] === '+' || src[i + 1] === '='){ i = save; break; }
      i++; ws();
      if (isStr()) buf += readStr(); else { skipOperand(); buf += PH(ph++); }
    }
    emit(buf); prev = 'x';
  }
  function readCode(close){
    let depth = 0;
    while (i < n){
      const c = src[i];
      if (c === '/' && src[i + 1] === '/'){ while (i < n && src[i] !== '\n') i++; continue; }
      if (c === '/' && src[i + 1] === '*'){ i = src.indexOf('*/', i + 2); i = i < 0 ? n : i + 2; continue; }
      if (c === "'"){ i++; while (i < n && src[i] !== "'"){ if (src[i] === '\\') i++; i++; } i++; prev = 'x'; continue; }
      if (c === '"' && src[i + 1] === '"' && src[i + 2] === '"'){ const e = src.indexOf('"""', i + 3); i = e < 0 ? n : e + 3; continue; }   // raw string: không dùng cho chữ hiển thị
      if ((c === '"' || c === '$' || c === '@') && isStr()){ readChain(); continue; }
      if (close && c === OPEN[close]) depth++;
      if (close && c === close){ if (depth === 0){ i++; prev = 'x'; return; } depth--; }
      if (!/\s/.test(c)) prev = /[A-Za-z0-9_]/.test(c) ? 'x' : c;
      i++;
    }
  }
  readCode(null);
}

// ---------- chạy ----------
const LAUNCHER = process.argv.includes('--launcher');
const SOURCES = LAUNCHER ? 'NguoiLuoiTuTienLauncher' : 'wwwroot Game Program.cs';
const DICT = LAUNCHER ? 'NguoiLuoiTuTienLauncher/wwwroot/i18n-en.js' : 'wwwroot/js/i18n-en.js';

function collect(){
  const files = cp.execSync(`git ls-files ${SOURCES}`, { cwd: ROOT, encoding: 'utf8' }).split(/\r?\n/)
    .filter(f => /\.(js|html|cs)$/.test(f) && !SKIP.some(r => r.test(f)));
  const found = new Map();      // đoạn → file đầu tiên chứa nó
  for (const f of files){
    const src = fs.readFileSync(path.join(ROOT, f), 'utf8');
    const add = (s, html) => { if (VI.test(s)) for (const seg of segments(s, html)) if (!found.has(seg)) found.set(seg, f); };
    if (f.endsWith('.cs')) scanCs(src, s => add(s));
    else if (f.endsWith('.js')) scanJs(src, s => add(s));
    else {
      add(src.replace(/<script[\s\S]*?<\/script>|<style[\s\S]*?<\/style>/gi, ''), true);
      (src.match(/<script(?![^>]*src)[^>]*>([\s\S]*?)<\/script>/gi) || []).forEach(s => scanJs(s.replace(/^<script[^>]*>|<\/script>$/gi, ''), x => add(x)));
    }
  }
  return found;
}

function loadDict(){
  const file = path.join(ROOT, DICT);
  if (!fs.existsSync(file)) return {};
  const g = {}; new Function('window', fs.readFileSync(file, 'utf8'))(g);
  return g.I18N_EN || {};
}

if (require.main === module){
  const found = collect(), dict = loadDict(), all = process.argv.includes('--all');
  const miss = {};
  for (const [seg, f] of found) if (all || !(seg in dict) && !KEEP.has(seg)) (miss[f] ??= []).push(seg);
  const count = Object.values(miss).reduce((a, b) => a + b.length, 0);
  fs.writeFileSync(path.join(__dirname, 'i18n-missing.json'), JSON.stringify(miss, null, 1));
  console.log(`${found.size} đoạn, ${Object.keys(dict).length} bản dịch, ${all ? 'ghi tất cả' : 'thiếu'} ${count} → tools/i18n-missing.json`);
}
module.exports = { collect, segments, norm };
