-- Vấn Đạo · Kiểm soát phiên bản game (bắt cập nhật, chặn bản cũ). Chạy trong Supabase → SQL Editor, chạy lại an toàn.
--
-- Phát hành bản mới: Table Editor → app_config (1 dòng):
--   latest_version : bản mới nhất → game hiện thông báo "Đã có bản mới" (vẫn cho chơi)
--   min_version    : bản thấp nhất được chơi → bản thấp hơn bị chặn hẳn + Chợ / Bảng xếp hạng từ chối
--   download_url   : link tải (Google Drive / OneDrive)
--   message        : lời nhắn hiện kèm (tùy chọn)

create table if not exists public.app_config (
  id             int  primary key default 1 check (id = 1),
  min_version    text not null default '0.0.0',
  latest_version text not null default '0.0.0',
  download_url   text not null default '',
  message        text not null default ''
);
insert into public.app_config (id) values (1) on conflict (id) do nothing;

alter table public.app_config enable row level security;
drop policy if exists app_config_read on public.app_config;
create policy app_config_read on public.app_config for select using (true);
revoke all on public.app_config from anon, authenticated;
grant select on public.app_config to anon, authenticated;

-- "0.6.0" / "0.6" / "v0.6.0-beta" → {0,6,0}
create or replace function public.version_num(v text)
returns int[] language sql immutable as $$
  select array(select coalesce(nullif(regexp_replace(split_part(split_part(coalesce(v, ''), '-', 1), '.', i), '\D', '', 'g'), ''), '0')::int
               from generate_series(1, 3) i)
$$;

-- Gọi ở đầu mọi hàm ghi (submit_score, market_auth): game gửi phiên bản qua header X-Game-Version.
-- Bản cũ không gửi header (hoặc thấp hơn min_version) → bị từ chối.
create or replace function public.require_version()
returns void language plpgsql stable security definer set search_path = public as $$
declare
  h json := nullif(current_setting('request.headers', true), '')::json;
  v text := h ->> 'x-game-version';
  m text;
begin
  select min_version into m from public.app_config where id = 1;
  if m is null then return; end if;
  if v is null or public.version_num(v) < public.version_num(m) then
    raise exception 'Phiên bản game đã cũ (cần bản % trở lên). Hãy tải bản mới.', m;
  end if;
end $$;

revoke all on function public.require_version() from public, anon, authenticated;
grant execute on function public.version_num(text) to anon, authenticated;
