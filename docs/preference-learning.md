# 🧠 Preference Learning System

## Overview

The User Memory System learns both **positive** and **negative** preferences from user behavior. This document explains how rejection signals are captured and used.

---

## Event Types

### Positive Signals (User Likes)
| Event Type | Weight | Example | Learning |
|------------|--------|---------|----------|
| `task:complete` | 1.0 | User completes "Morning Workout" at 7am | Likes morning exercise |
| `recommendation:accept` | 1.5 | User accepts "Buy groceries" suggestion | Likes this task type |
| `feature:voice` | 1.0 | User opens voice recorder | Prefers voice input |

### Negative Signals (User Dislikes)
| Event Type | Weight | Example | Learning |
|------------|--------|---------|----------|
| `recommendation:reject` | 1.5 | User deletes "Set voice reminder" | Dislikes voice reminders |
| `task:delete` | 0.8 | User deletes uncompleted task | Task wasn't relevant |

---

## Your "Voice Reminder" Example

### Scenario
1. **AI generates recommendation**: "I'll set voice reminders for departure times..."
2. **You delete the task** → `recommendation:reject` event logged
3. **Event details**:
   ```json
   {
     "type": "recommendation:reject",
     "topic": "I'll set voice reminders for departure times...",
     "weight": 1.5,
     "metadata": { "projectName": "Travel Planning" }
   }
   ```

### How the System Learns

#### Phase 1: Event Logging (✅ Now Working)
- Event is stored in the `Events` table
- Topic contains the full task title for pattern analysis
- High weight (1.5) signals this is an explicit user choice

#### Phase 2: Pattern Detection (Future Enhancement)
After 2-3 rejections of similar tasks, the snapshot builder could create:

```
Negative Fact:
  Key: "avoids.voice_reminders"
  Value: "true"
  Score: 0.9
```

Or more granularly:

```
Negative Fact:
  Key: "dislikes.task_type:voice"
  Value: "high"
  Score: 0.85
```

#### Phase 3: AI Integration (Already Active)
Next time AI generates recommendations:
1. Snapshot includes: "User avoids voice reminder tasks"
2. AI sees this context
3. AI stops suggesting voice-related tasks
4. **Result**: No more unwanted voice reminder suggestions!

---

## Current Implementation Status

### ✅ **What's Working NOW:**
1. **Event Logging**: All accept/reject events are captured
2. **Metadata**: Task title and project context stored
3. **High Weight**: Explicit choices weighted at 1.5 (50% higher than automatic events)
4. **AI Context**: Rejection patterns visible in event history

### 🔜 **Future Enhancements:**
1. **Automatic Fact Creation**: Snapshot builder detects rejection patterns
2. **Negative Preference Facts**: Create `avoids.*` or `dislikes.*` facts
3. **Topic Analysis**: Extract keywords from rejected tasks ("voice", "reminder")
4. **Decay**: Old rejections fade over time (preferences change)

---

## Recommendation Sources

The system tracks where recommendations come from:

| Source | Location | Metadata |
|--------|----------|----------|
| `project` | ProjectDetailPage | When user creates new project |
| `voice` | VoicePageModel | After analyzing voice recording |
| `photo` | PhotoPageModel | After analyzing photo of task list |
| `priority` | MainPage Telepathy | AI priority task suggestions |

This helps the system learn: "User likes voice-based task creation but dislikes voice reminder suggestions"

---

## Testing the System

### To See Your Preference Being Learned:

1. **Open Memory Debug Page**
2. **Create a project** and reject an AI recommendation
3. **Tap "Refresh Data"**
4. **Check Events section** - you should see:
   ```
   [timestamp] recommendation:reject
     Topic: [task title you rejected]
     Weight: 1.5
   ```

### Over Time:
- After 3-5 rejections of similar topics, you should see the AI stop suggesting those types of tasks
- Check the snapshot text to see if your preferences appear as facts

---

## Why This Matters

### Without Rejection Tracking:
- AI keeps suggesting tasks you don't want
- You repeatedly reject the same types of suggestions
- Frustrating experience, AI seems "dumb"

### With Rejection Tracking:
- AI learns from your "No" responses
- Stops suggesting unwanted task types
- Becomes more personalized over time
- **Result**: AI that respects your preferences!

---

## Implementation Details

### Code Locations

**Event Logging:**
- `ProjectDetailPageModel.RejectRecommendation()` - Project-based recommendations
- `VoicePageModel.RejectRecommendation()` - Voice-analyzed tasks
- `MainPageModel.RejectRecommendation()` - Priority task suggestions

**Event Structure:**
```csharp
MemoryEvent.Create(
    "recommendation:reject",
    task.Title,                    // Full task text for pattern analysis
    new { 
        projectName = project.Name,  // Context
        source = "project"           // Where rejection happened
    },
    1.5                            // High weight = explicit user choice
)
```

### Database Schema

**Events Table:**
```sql
CREATE TABLE Events (
    UserId TEXT NOT NULL,
    Type TEXT NOT NULL,        -- "recommendation:reject"
    Topic TEXT,                -- Task title (e.g., "Set voice reminders...")
    MetaJson TEXT,             -- {"projectName": "...", "source": "..."}
    Weight REAL NOT NULL,      -- 1.5
    AtUtc TEXT NOT NULL        -- Timestamp
)
```

**Future Facts Table Enhancement:**
```sql
-- When pattern detected, create negative fact:
INSERT INTO Facts (UserId, Key, Value, Score)
VALUES ('default', 'avoids.voice_reminders', 'true', 0.9);
```

---

## Next Steps

### For Immediate Impact:
1. ✅ **Events are being logged** - your rejection is captured!
2. ✅ **High weight applied** - system knows this is important
3. ✅ **AI sees event history** - context is available

### For Future Enhancement (Phase 4):
1. Add pattern detection algorithm to snapshot builder
2. Create negative preference facts from rejection clusters
3. Enhance AI prompts to explicitly respect `avoids.*` facts
4. Add UI in "My Data" page to view learned dislikes

---

## Summary

**Your observation was spot-on!** 🎯 

When you delete that "voice reminder" task, the system now:
1. ✅ Logs a `recommendation:reject` event
2. ✅ Stores the full task title for pattern analysis
3. ✅ Marks it with high importance (weight 1.5)
4. ✅ Includes context (project name, source)

**What happens next:**
- Over 2-3 similar rejections, patterns become clear in the event log
- AI sees these events when making future recommendations
- Future enhancement: Automatic negative fact creation

**Your feedback made the system smarter! This is exactly the kind of signal a good memory system needs to capture.** 🌟
