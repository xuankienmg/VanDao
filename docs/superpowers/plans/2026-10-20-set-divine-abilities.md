# Set divine abilities implementation plan

**Goal:** Activate exactly one highest-quality divine ability for ten valid items of the same named set.

**Architecture:** Pure equipment snapshot describes progress and active tier. A per-battle ability object owns survival and next-attack state, with deterministic independent rolls. Battle DTO exposes sampled per-turn events; client only renders confirmed events.

**Tech stack:** C#, .NET, existing combat simulator and vanilla JS canvas.

- [x] Test equipment eligibility: ten unique valid slots, same set, minimum quality wins, mixed sets and missing pieces disabled.
- [x] Implement snapshot tiers: divine survival once per battle; immortal 15% nullify; dao +10 percentage points extra hit, cap 40%; supreme +50% next attack after evade/nullify, no stacking.
- [x] Test state transitions, persistent state across waves, lethal damage after shield and no reflection on nullified hits. Integrate with server turn simulation. Preserve event alignment when sampling.
- [x] Display progress/description in inventory, active icon and confirmed turn events in combat. Suppress damage/hurt feedback on nullification.
- [x] Run server integration/regression, client renderer checks and desktop build. Review final diff.

Decisions: survival protects only through the current turn. A normal or active skill hit consumes the next-attack boost; auxiliary burn/lightning/reflection do not. At supreme, only the supreme ability is active, so charging normally comes from dodge. The nullification path is also supported if a future independent source supplies it. No persisted save migration required.

Verification: `dotnet run --no-restore --project tools/dodge_checks -p:NuGetAudit=false` (160 seeded divine battles plus prior regression checks); `node tools/dodge_checks/client.cjs`; actual renderer Playwright checks in `tools/dodge_checks/ui.cjs`; desktop build output `bin/divine-check`. Read-only independent review found one charge-badge timing bug; regression reproduced it, then fix passed. Existing WindowsBase assembly warning remains.
