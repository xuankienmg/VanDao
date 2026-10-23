// Điền nút tải và ghi chú phát hành từ release.json (workflow deploy-site sinh ra lúc deploy).
// Thiếu hoặc lỗi thì hỏi thẳng GitHub API; vẫn lỗi thì nút giữ link /releases/latest có sẵn trong HTML.
(() => {
  const REPO = 'xuankienmg/nguoi-luoi-tu-tien';
  const isGameZip = a => /^VanDao_v\d+\.\d+\.\d+\.zip$/i.test(a.name) && a.state === 'uploaded';

  async function fromSite() {
    const r = await fetch('release.json', { cache: 'no-cache' });
    if (!r.ok) throw new Error('release.json ' + r.status);
    const j = await r.json();
    if (!j.zip_url) throw new Error('release.json thiếu zip_url');
    return j;
  }

  async function fromGitHub() {
    const r = await fetch(`https://api.github.com/repos/${REPO}/releases?per_page=10`, { headers: { Accept: 'application/vnd.github+json' } });
    if (!r.ok) throw new Error('GitHub API ' + r.status);
    for (const rel of await r.json()) {
      if (rel.draft || rel.prerelease) continue;
      const zip = (rel.assets || []).find(isGameZip);
      if (!zip) continue;
      return {
        tag: rel.tag_name, version: rel.tag_name.replace(/^v/i, ''), published_at: rel.published_at,
        zip_url: zip.browser_download_url, size: zip.size, sha256: (zip.digest || '').replace(/^sha256:/, ''),
        body: rel.body || ''
      };
    }
    throw new Error('Chưa có bản phát hành kèm ZIP');
  }

  // Cormorant Garamond chỉ có số kiểu cổ (0 trông như o), nên số phiên bản dùng font thân bài
  const num = t => Object.assign(document.createElement('span'), { className: 'num', textContent: t });
  const mb = n => Math.round(n / 1048576) + ' MB';
  const L = window.siteLang || { lang: 'vi', t: k => k, onChange(){} };
  const date = s => new Date(s).toLocaleDateString(L.lang === 'en' ? 'en-GB' : 'vi-VN', { day: 'numeric', month: L.lang === 'en' ? 'short' : 'numeric', year: 'numeric' });
  const esc = s => s.replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));

  // Markdown tối giản cho ghi chú GitHub: ##/### tiêu đề, gạch đầu dòng, **đậm**, [link](https://…).
  // Trang không dẫn sang GitHub: link GitHub chỉ giữ chữ, URL GitHub trơn thì bỏ.
  function inline(s) {
    return esc(s)
      .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
      .replace(/\[([^\]]+)\]\((https:\/\/[^)\s]+)\)/g, (_, text, url) => /^https:\/\/([^/]+\.)?github\.com\//.test(url) ? text : `<a href="${url}">${text}</a>`)
      .replace(/(^|\s)https:\/\/github\.com\/[^\s<]+/g, '$1');
  }
  function renderNotes(md) {
    const out = []; let list = null, group = null;
    const open = () => { if (!group) { group = []; } };
    const flushList = () => { if (list) { group.push('<ul>' + list.join('') + '</ul>'); list = null; } };
    const flushGroup = () => { flushList(); if (group && group.length) out.push('<div>' + group.join('') + '</div>'); group = null; };
    for (const raw of md.replace(/\r/g, '').split('\n')) {
      const line = raw.trim();
      if (!line || /^##\s/.test(line) && !/^###/.test(line)) continue; // bỏ "## Có gì mới": trang đã có tiêu đề
      let m;
      if ((m = line.match(/^#{3,}\s+(.*)/))) { flushGroup(); open(); group.push('<h4>' + inline(m[1]) + '</h4>'); }
      else if ((m = line.match(/^[-*]\s+(.*)/))) { open(); (list ||= []).push('<li>' + inline(m[1]) + '</li>'); }
      else { open(); flushList(); group.push('<p>' + inline(line) + '</p>'); }
    }
    flushGroup();
    return out.join('');
  }

  function apply(rel) {
    const dl = document.getElementById('dl');
    dl.href = rel.zip_url;
    const main = dl.querySelector('.seal-main');
    main.textContent = L.t('download') + ' ';
    main.append(num('v' + rel.version));
    document.getElementById('dlMeta').textContent = (rel.size ? mb(rel.size) + ', ' : '') + 'Windows 64-bit';
    if (rel.published_at) document.getElementById('dlNote').textContent = L.t('freeReleased', date(rel.published_at));
    document.getElementById('relVersion').replaceChildren(num('v' + rel.version));
    const meta = document.getElementById('relMeta');
    meta.textContent = rel.published_at ? L.t('released', date(rel.published_at)) : '';
    if (rel.body) document.getElementById('relNotes').innerHTML = renderNotes(rel.body);
  }

  // đổi ngôn ngữ: lang.js trả chữ gốc của nút tải / ghi chú, nên điền lại thông tin bản phát hành
  let current = null;
  L.onChange(() => { if (current) apply(current); });
  fromSite().catch(fromGitHub).then(rel => { current = rel; apply(rel); }).catch(err => console.warn('Không lấy được bản phát hành:', err.message));
})();
