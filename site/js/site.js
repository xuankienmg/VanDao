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
        body: rel.body || '', html_url: rel.html_url
      };
    }
    throw new Error('Chưa có bản phát hành kèm ZIP');
  }

  // Cormorant Garamond chỉ có số kiểu cổ (0 trông như o), nên số phiên bản dùng font thân bài
  const num = t => Object.assign(document.createElement('span'), { className: 'num', textContent: t });
  const mb = n => Math.round(n / 1048576) + ' MB';
  const date = s => new Date(s).toLocaleDateString('vi-VN', { day: 'numeric', month: 'numeric', year: 'numeric' });
  const esc = s => s.replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));

  // Markdown tối giản cho ghi chú GitHub: ##/### tiêu đề, gạch đầu dòng, **đậm**, [link](https://…)
  function inline(s) {
    return esc(s)
      .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
      .replace(/\[([^\]]+)\]\((https:\/\/[^)\s]+)\)/g, '<a href="$2">$1</a>')
      .replace(/(^|\s)(https:\/\/github\.com\/[^\s<]+)/g, '$1<a href="$2">$2</a>');
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
    main.textContent = 'Tải ';
    main.append(num('v' + rel.version));
    document.getElementById('dlMeta').textContent = (rel.size ? mb(rel.size) + ', ' : '') + 'Windows 64-bit';
    if (rel.published_at) document.getElementById('dlNote').textContent = `Miễn phí. Phát hành ngày ${date(rel.published_at)}. File ZIP gồm game và Launcher tự cập nhật.`;
    document.getElementById('relVersion').replaceChildren(num('v' + rel.version));
    const meta = document.getElementById('relMeta');
    meta.textContent = rel.published_at ? 'Phát hành ngày ' + date(rel.published_at) + '. ' : '';
    const a = document.createElement('a'); a.href = rel.html_url || `https://github.com/${REPO}/releases/tag/${rel.tag}`; a.textContent = 'Xem trên GitHub';
    meta.append(a);
    if (rel.body) document.getElementById('relNotes').innerHTML = renderNotes(rel.body);
    if (rel.sha256) {
      const h = document.getElementById('relHash');
      h.textContent = `SHA-256 của ${rel.zip_name || 'VanDao_' + rel.tag + '.zip'}: ${rel.sha256}`;
      h.hidden = false;
    }
  }

  fromSite().catch(fromGitHub).then(apply).catch(err => console.warn('Không lấy được bản phát hành:', err.message));
})();
