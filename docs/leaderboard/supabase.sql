-- Vấn Đạo · Bảng xếp hạng online trên Supabase.
-- Chạy một lần trong Supabase → SQL Editor. Chạy lại an toàn (create or replace / if not exists).
--
-- Bảo mật:
--   * Bảng gốc không cho anon đọc/ghi trực tiếp. Client chỉ đọc qua view leaderboard_public
--     (không có cột secret_hash) và ghi qua hàm submit_score / remove_score.
--   * Mỗi nhân vật có một "secret" ngẫu nhiên lưu trong DB máy người chơi; chỉ ai giữ secret mới
--     ghi đè được dòng của nhân vật đó. Server chỉ lưu SHA-256 của secret.

create table if not exists public.leaderboard (
  player_id   uuid primary key,
  secret_hash text        not null,
  name        text        not null,
  realm       int         not null default 0,
  stage       int         not null default 0,
  level       int         not null default 1,
  qi          double precision not null default 0,
  realm_name  text        not null default '',
  stage_name  text        not null default '',
  power       bigint      not null default 0,
  version     text        not null default '',
  updated_at  timestamptz not null default now()
);

-- trang bị đang mặc (ô → mã vật phẩm) và chỉ số chiến đấu, để xem khi bấm vào một người trên bảng
alter table public.leaderboard add column if not exists equipment jsonb not null default '{}'::jsonb;
alter table public.leaderboard add column if not exists stats     jsonb not null default '{}'::jsonb;
-- nhân vật từng mở bằng bản game cũ (SaveGuard): vẫn xếp hạng nhưng gắn dấu * đỏ và không vào top 10. Đã gắn thì không tự gỡ.
alter table public.leaderboard add column if not exists flagged   boolean not null default false;

create index if not exists leaderboard_realm_idx on public.leaderboard (realm desc, stage desc, qi desc);
create index if not exists leaderboard_power_idx on public.leaderboard (power desc);

alter table public.leaderboard enable row level security;
revoke all on public.leaderboard from anon, authenticated;

-- View công khai: không lộ secret_hash, kèm hạng theo từng tiêu chí
-- Hạng: 10 vị trí đầu chỉ dành cho nhân vật không bị gắn cờ; nhân vật gắn cờ (flagged) xếp từ hạng 11 trở đi theo đúng thứ tự.
-- (cột mới phải nằm cuối danh sách để "create or replace view" chạy được trên view cũ)
create or replace view public.leaderboard_public as
select player_id, name, realm, stage, level, qi, realm_name, stage_name, power, updated_at,
       row_number() over (order by (case when not flagged and clean_realm <= 10 then 0 else 1 end), realm desc, stage desc, qi desc) as realm_rank,
       row_number() over (order by (case when not flagged and clean_power <= 10 then 0 else 1 end), power desc)                     as power_rank,
       equipment, stats, flagged
from (
  select l.*,
         row_number() over (partition by l.flagged order by l.realm desc, l.stage desc, l.qi desc) as clean_realm,
         row_number() over (partition by l.flagged order by l.power desc)                          as clean_power
  from public.leaderboard l
) t;

grant select on public.leaderboard_public to anon, authenticated;

-- bản cũ của hàm (không có trang bị / không có cờ): bỏ để PostgREST không phải chọn giữa nhiều bản
drop function if exists public.submit_score(uuid, text, text, int, int, int, double precision, text, text, bigint, text);
drop function if exists public.submit_score(uuid, text, text, int, int, int, double precision, text, text, bigint, text, jsonb, jsonb);

create or replace function public.submit_score(
  p_id uuid, p_secret text, p_name text,
  p_realm int, p_stage int, p_level int, p_qi double precision,
  p_realm_name text, p_stage_name text, p_power bigint, p_version text,
  p_equipment jsonb default '{}'::jsonb, p_stats jsonb default '{}'::jsonb, p_flagged boolean default false)
returns boolean
language plpgsql security definer set search_path = public as $$
declare
  h text;
  n int;
begin
  perform public.require_version();      -- docs/version/supabase.sql: chặn bản game cũ
  if p_secret is null or length(p_secret) < 24 then raise exception 'invalid secret'; end if;
  if p_realm not between 0 and 50 or p_stage not between 0 and 50 or p_power not between 0 and 1000000000000 then
    raise exception 'invalid values';
  end if;
  if jsonb_typeof(coalesce(p_equipment, '{}')) <> 'object' or pg_column_size(p_equipment) > 4000
     or jsonb_typeof(coalesce(p_stats, '{}')) <> 'object' or pg_column_size(p_stats) > 1000 then
    raise exception 'invalid equipment';
  end if;
  h := encode(sha256(convert_to(p_secret, 'UTF8')), 'hex');
  insert into public.leaderboard as l
    (player_id, secret_hash, name, realm, stage, level, qi, realm_name, stage_name, power, version, equipment, stats, flagged, updated_at)
  values
    (p_id, h, left(btrim(coalesce(p_name, '')), 24), p_realm, p_stage, p_level, greatest(p_qi, 0),
     left(p_realm_name, 32), left(p_stage_name, 32), p_power, left(coalesce(p_version, ''), 16),
     coalesce(p_equipment, '{}'), coalesce(p_stats, '{}'), coalesce(p_flagged, false), now())
  on conflict (player_id) do update set
    name = excluded.name, realm = excluded.realm, stage = excluded.stage, level = excluded.level, qi = excluded.qi,
    realm_name = excluded.realm_name, stage_name = excluded.stage_name, power = excluded.power,
    version = excluded.version, equipment = excluded.equipment, stats = excluded.stats,
    flagged = l.flagged or excluded.flagged,      -- đã gắn cờ thì giữ (bản game không tự gỡ được)
    updated_at = now()
  where l.secret_hash = h;          -- sai secret: không ghi đè dòng của người khác
  get diagnostics n = row_count;
  return n > 0;
end $$;

create or replace function public.remove_score(p_id uuid, p_secret text)
returns boolean
language plpgsql security definer set search_path = public as $$
declare n int;
begin
  delete from public.leaderboard
  where player_id = p_id and secret_hash = encode(sha256(convert_to(coalesce(p_secret, ''), 'UTF8')), 'hex');
  get diagnostics n = row_count;
  return n > 0;
end $$;

revoke all on function public.submit_score(uuid, text, text, int, int, int, double precision, text, text, bigint, text, jsonb, jsonb, boolean) from public;
revoke all on function public.remove_score(uuid, text) from public;
grant execute on function public.submit_score(uuid, text, text, int, int, int, double precision, text, text, bigint, text, jsonb, jsonb, boolean) to anon, authenticated;
grant execute on function public.remove_score(uuid, text) to anon, authenticated;
