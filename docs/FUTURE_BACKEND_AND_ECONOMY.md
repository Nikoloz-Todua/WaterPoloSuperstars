# Offline profile and future backend

M12 runs without networking. `HubProgression` is the sole local transaction authority; `JsonProfileRepository` stores compact schema-1 snapshots in `Application.persistentDataPath/profile-v1.json`. `HubCatalog` is an editable Resources ScriptableObject, catalog version `m12.1`. Never copy its names, art, stat definitions or reward tables into per-user saves.

## Current records and boundaries

- Local profile UUID, display name/avatar, cumulative XP/level, trophies, integer coins/gems, cosmetic palette indices.
- Owned athletes: stable `wp_001` identifiers, level and shards. Seven lineup IDs: goalkeeper first, then six field slots; reserves are owned players absent from the lineup.
- Packs: stable instance ID, type/rarity, duration, UTC start, fixed outcome-table reference. State derives from these fields. Removal and reward grant occur in one transaction.
- Season ID/XP, stable claimed reward IDs, career counters, last 20 match receipts, active session and pending result.
- Monotonic save revision and retained transaction keys. The key journal is intentionally retained for offline deduplication; it is not a cryptographic ledger. A future backend must compact acknowledged history safely rather than dropping deduplication keys arbitrarily.
- `IProfileRepository`, `IEconomyService`, `IProgressionService`, `IPlayerCollectionRepository`, `IRewardService` separate presentation from storage/transactions. `ICloudSyncAdapter` and `IPurchaseProvider` are future boundaries only, with no connected implementation.

Transactions copy state, validate, persist, then publish. No UI writes balances. Repeated match/pack/season/achievement IDs cannot grant twice. Upgrade keys include expected player level. Shop confirmations retain one request ID. There are no paid random packs, ads, billing or premium entitlement grants.

Primary writes use a validated `.tmp` plus atomic `File.Replace` and `.bak` where supported. An invalid primary cannot overwrite a good backup. Loading tries primary, backup, then temporary snapshot; unreadable existing saves halt initialization with a retry option instead of creating empty progress. First-run creation happens only when none exists. Storage errors leave transactions unapplied. Important actions save immediately; no long-lived dirty economy buffer depends on application shutdown callbacks. Preferences remain in their existing separate PlayerPrefs record.

## Versioning and catalog evolution

Schema 1 is explicit. An unsupported schema is retained and refused; it is never silently downgraded. Introduce explicit sequential migrations (1 → 2, etc.) before changing fields; migrate a validated copy and retain the original backup until successful save. Version catalog changes separately. Stable IDs must not change when names/art change. Retain tombstone/alias definitions for removed content. Unknown collection records can remain in the compact save and are hidden from selection; an unknown starting lineup ID currently stops loading to prevent silent roster replacement. Production migration should repair this through an explicit, documented replacement policy.

Pack outcomes currently reference a table index. Preserve existing table ordering/meaning when tuning this catalog. Before changing rewards for acquired packs, migrate to a versioned reward-table ID plus stable outcome ID; never reroll already-earned packs.

## Local timer and economy limits

Pack countdown uses UTC device time; backward jumps clamp remaining time to its original duration, forward jumps may unlock early. This is transparent offline behavior, not tamper resistance. Packs default to 45 seconds; capacity defaults to four. All occupied slots refuse a new pack and the result explains the overflow; no existing pack is deleted. Every eligible completed Quick Match earns a pack if space exists. Training and abandoned sessions grant nothing. Season 01 has no date-based reset. Client JSON, UUIDs and transaction keys do not prevent cheating.

## Firebase migration (future milestone)

1. Initialize locally exactly as today. Optional sync must never gate offline entry. Show **Local only** now; future states are Pending sync, Synced, Conflict and Sync error. Never label M12 as synced.
2. Add Firebase Authentication and explicit local-profile linking to an authenticated UID. Keep the local UUID as provenance. Preview and confirm conflicts with an existing cloud profile; never silently merge unrelated accounts or sum their currency.
3. Cloud documents contain compact ownership, roster, cosmetics, progress and inventory records plus schema/catalog versions, revision and server acknowledgment cursor. Assets and static definitions ship with the game or a separately versioned content catalog.
4. Replace local sensitive economy acceptance with authenticated server transactions / callable functions. Use globally unique request IDs with account-scoped idempotency, checked preconditions and a server transaction ledger. Retry the same request after dropped responses. Never use naive last-write-wins or additive merging for balances, rewards or paid entitlements.
5. Maintain an offline action outbox with base revision, device/profile provenance and operation IDs. Distinguish provisional offline rewards from acknowledged transactions. The server validates eligible match/reward claims under an explicitly defined trust policy; offline clients cannot prove a match was honest. Reconcile rejected actions visibly without duplicating grants or spending the same balance twice.
6. Two-device conflicts: cosmetic/name selections may use field-level conflict policy; lineup changes require validation against merged authoritative ownership. Currency, inventory, season claims and purchases resolve through the ledger. Never merge two whole snapshots by timestamp. Preserve recovery snapshots during conflict/error states.
7. Real purchases require Play Billing / App Store integration, authenticated receipt/token verification, replay protection, durable entitlement records, refund/revocation handling and server-authoritative premium balances. Never trust a local premium flag or a client-submitted receipt result. Firestore security rules must deny direct sensitive balance/entitlement writes.

No Firebase packages, credentials, authentication requests, network sync, analytics or purchase SDKs were introduced in M12.
