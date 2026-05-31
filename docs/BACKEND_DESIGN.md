# Backend Design

## Status

This is a design document only. The prototype remains local-only, with no production networking, accounts, cloud save, guilds, PvP, purchases, ads, or backend dependency.

## Goals

- Preserve local gameplay responsiveness while adding optional online durability later.
- Keep timers, resources, and competitive surfaces server-authoritative when online features arrive.
- Avoid real-time PvP for the first online version.
- Avoid monetization until the local game is stable and the product direction is explicit.

## Future Services

- Account service: device login first, then Apple sign-in if needed.
- Cloud save service: stores player base, heroes, resources, timers, and unlocked levels.
- Timer service: validates HQ upgrades and future timed actions against server time.
- Leaderboard service: stores score snapshots from completed local minigame runs.
- Alliance service: later guild membership, chat metadata, and shared contribution state.
- PvP simulation service: asynchronous defense/attack snapshots, not real-time combat.
- Purchase validation service: only if monetization is later added, with App Store receipt validation.
- Anti-cheat service: validates suspicious deltas in resources, timers, hero progression, and scores.

## Data Ownership

The client can remain authoritative for local-only prototype saves. Once online mode exists, the server should own:

- Player identity.
- Resource balances.
- Upgrade start and completion timestamps.
- Hero ownership, level, and XP.
- Minigame unlock state.
- Leaderboard submissions.
- Alliance membership and contributions.

The client should own:

- Rendering and moment-to-moment local input.
- Cached read models for offline display.
- Unsynced local run attempts until the server accepts or rejects the result.

## Minimal API Draft

```text
POST /login/device
GET  /player/save
PUT  /player/save/migrate-local
POST /base/hq/upgrade/start
POST /base/hq/upgrade/claim
POST /heroes/equip
POST /heroes/level-up
POST /runs/complete
GET  /leaderboards/{season}
```

## Save Migration

1. Keep the current local JSON save as the offline source.
2. When accounts are introduced, upload a normalized local save once.
3. Server validates ranges and clamps impossible values.
4. Server returns a cloud save revision.
5. Client stores the revision locally and sends it with future mutations.

## Anti-Cheat Rules

- Reject negative resources, duplicate hero ids, unknown hero ids, and out-of-range hero levels.
- Validate timer completion against server time.
- Cap run rewards by known level definition and player state.
- Rate-limit repeated reward claims and level-up requests.
- Store audit events for suspicious resource, timer, or leaderboard deltas.

## Deployment Shape

Start with one small backend service plus a managed database. Avoid microservices until traffic or ownership boundaries require them. Use basic observability from day one: structured logs, request ids, latency metrics, error rates, and audit-event sampling.

## Not In Scope Yet

- Real-time PvP.
- Production guild chat.
- Ads.
- Real-money purchases.
- Paid gacha or paid loot boxes.
- Complex live operations tooling.
