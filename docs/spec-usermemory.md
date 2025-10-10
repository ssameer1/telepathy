Here’s a clean, ready-to-drop **Markdown specification document** for your project — defining the MVP user memory system for personalization in your .NET MAUI app.
It captures architecture, storage schema, operational rules, and implementation guidance in developer-friendly language.

---

# 🧠 User Memory System (MVP Spec)

## Overview

This module provides **local, privacy-first personalization** for the application.
It stores user behavior, preferences, and contextual insights on-device, and periodically distills them into a concise “snapshot” used for adaptive UI or AI-driven features.

---

## Goals

* Enable **personalization** without cloud data sharing.
* Maintain **transparent, user-controllable memory**.
* Provide a **compact perspective** on user behavior for AI or UI adaptation.
* Use **SQLite** for portability, speed, and durability.

---

## Architecture

### Tiered Storage Pattern

| Tier           | Table      | Purpose                                                                     | Size / Frequency               |
| -------------- | ---------- | --------------------------------------------------------------------------- | ------------------------------ |
| **Raw Events** | `Events`   | Immutable stream of user actions, interactions, or feedback.                | High volume, pruned regularly. |
| **Facts**      | `Facts`    | Derived preferences, affinities, and behaviors aggregated from events.      | Moderate size, persistent.     |
| **Profile**    | `Profile`  | Explicit user settings or declared preferences.                             | Small and stable.              |
| **Snapshot**   | `Snapshot` | The distilled representation (8–16 lines) used for personalization context. | Tiny, rebuilt periodically.    |

---

## Data Model

### 1. Profile

Stores explicit user preferences.

```sql
CREATE TABLE IF NOT EXISTS Profile (
  Key TEXT PRIMARY KEY,
  Value TEXT NOT NULL
);
```

Example entries:

| Key   | Value    |
| ----- | -------- |
| tone  | casual   |
| units | imperial |
| theme | dark     |

---

### 2. Events

Logs raw user actions and contextual signals.

```sql
CREATE TABLE IF NOT EXISTS Events (
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  UserId TEXT NOT NULL,
  Type TEXT NOT NULL,       -- e.g., "open:task", "action:complete"
  Topic TEXT,               -- e.g., "exercise", "email"
  MetaJson TEXT,            -- serialized object with details
  Weight REAL DEFAULT 1.0,  -- signal strength
  AtUtc TEXT NOT NULL       -- ISO timestamp
);
CREATE INDEX IF NOT EXISTS IX_Events_User_At ON Events(UserId, AtUtc DESC);
```

Example:

```json
{ "UserId": "u1", "Type": "action:complete", "Topic": "exercise", "Weight": 1.0 }
```

---

### 3. Facts

Aggregated, higher-level knowledge inferred from events or direct updates.

```sql
CREATE TABLE IF NOT EXISTS Facts (
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  UserId TEXT NOT NULL,
  Key TEXT NOT NULL,          -- e.g., "prefers.morning_exercise"
  Value TEXT NOT NULL,
  Score REAL NOT NULL DEFAULT 0.0,
  UpdatedUtc TEXT NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS UX_Facts_User_Key ON Facts(UserId, Key);
```

Example:

| Key                      | Value | Score |
| ------------------------ | ----- | ----- |
| prefers.morning_exercise | true  | 0.9   |
| affinity.topic:fitness   | high  | 0.8   |

---

### 4. Snapshot

A small, prompt-ready summary of the user’s current context.

```sql
CREATE TABLE IF NOT EXISTS Snapshot (
  UserId TEXT PRIMARY KEY,
  Version INTEGER NOT NULL,
  BuiltUtc TEXT NOT NULL,
  LinesJson TEXT NOT NULL     -- serialized string[]
);
```

Example snapshot lines:

```
Tone: casual
Units: imperial
prefers.morning_exercise=true (s=0.9)
affinity.topic:fitness=high (s=0.8)
recent.topics=exercise,family,productivity
```

---

## Operational Flow

### 1. Logging Events

Each user action (tap, complete, navigate, etc.) is logged via:

```csharp
await memory.LogEventAsync(new MemoryEvent(
  userId, "action:complete", "exercise",
  new { duration = 30 }, 1.0, DateTimeOffset.UtcNow));
```

High-frequency, cheap writes.
Older events are pruned automatically (target: ≤50k per user).

---

### 2. Rolling Up to Facts

Periodic job or event triggers infer persistent “facts” from behavior:

```csharp
await memory.AddOrBumpFactAsync(userId,
  "prefers.morning_exercise", "true", +0.5);
```

* Each fact is capped at a **Score range [0–10]**.
* Facts decay over time (implicit via snapshot rules).
* Only **300–600 facts per user** retained.

---

### 3. Snapshot Generation

Build a distilled memory snapshot every **10 minutes** or after **N major events**.

```csharp
var snap = await memory.BuildOrGetSnapshotAsync(userId, TimeSpan.FromMinutes(10));
var context = string.Join("\n", snap.Lines);
```

* **8–16 lines** max.
* Prioritizes:

  * Recent high-confidence facts (`Score ≥ 0.8`)
  * Diverse prefixes (`prefers`, `affinity`, `habit`, etc.)
  * 1–2 top recent topics (based on decayed event weights).

This is what you send as personalization context to your AI or decision logic.

---

## Decay & Pruning Rules

| Type         | Lifespan                         | Action                                           |
| ------------ | -------------------------------- | ------------------------------------------------ |
| **Events**   | 120 days                         | Delete beyond limit or count cap (50k per user). |
| **Facts**    | 30 days inactivity + Score < 0.2 | Drop fact.                                       |
| **Snapshot** | 10–15 minutes                    | Rebuild automatically.                           |

Weight decay function:

```
w = exp(-(now - event_time) / 7 days)
```

---

## Privacy & Transparency

### Principles

* All data stored **locally on device**.
* No cloud sync unless explicitly configured.
* Each user can:

  * View current facts & snapshot.
  * Opt-out or “Forget me”.
  * Lock or override any inferred fact.

### “My Data” Interface

Expose in settings:

* `Facts (key, value, score)`
* “Forget Me” → wipes `Events`, `Facts`, and `Snapshot`.
* “Lock Fact” → sets score=0 and marks `locked:true`.

---

## Recommended Defaults

| Table    | Target Size       | Prune Cadence                |
| -------- | ----------------- | ---------------------------- |
| Events   | ≤50,000 rows/user | Daily                        |
| Facts    | ≤600 rows/user    | Weekly                       |
| Snapshot | 1 per user        | Every 10 min or 8 key events |

---

## Design Learnings and Rationale

| Principle              | Why                                                                                            |
| ---------------------- | ---------------------------------------------------------------------------------------------- |
| **Recency > Volume**   | Recent behavior predicts current intent better than long-term averages.                        |
| **Diversity of facts** | Reduces bias from repetitive behavior; each prefix family contributes one representative fact. |
| **Compact snapshots**  | LLMs and heuristics perform better with concise, high-signal summaries.                        |
| **Confidence gating**  | Only high-score facts influence personalization to reduce noise.                               |
| **Privacy by design**  | Users maintain control; memory is local, inspectable, and ephemeral by default.                |

---

## Future Extensions

* Add **FTS5 search** for fast keyword lookup on `Events`.
* Add **vector column** for semantic embeddings (local ONNX).
* Implement background **aggregator service** to refresh snapshots automatically.
* Optionally **sync facts across devices** via encrypted export/import.

---

## Implementation Path

1. Add this spec and `SqliteUserMemoryStore.cs` to your shared project.
2. Register in `MauiProgram.cs`:

   ```csharp
   builder.Services.AddSingleton<IUserMemoryStore>(sp =>
       new SqliteUserMemoryStore(Path.Combine(FileSystem.AppDataDirectory, "memory.db")));
   ```
3. Instrument UI events and preference changes with `LogEventAsync` / `AddOrBumpFactAsync`.
4. Generate snapshots periodically or on app resume.
5. Display the “My Data” view for transparency.

---

### 📦 MVP Delivery

* **Phase 1:** Tables, inserts, pruning.
* **Phase 2:** Snapshot builder + confidence filter.
* **Phase 3:** “My Data” UI.
* **Phase 4:** Optional AI prompt integration.

---

Would you like me to append a **"Snapshot Builder Design Notes"** section next — showing logic flow diagrams and pseudocode for how facts get selected into the snapshot (ready for dev handoff)?
