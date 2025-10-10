# 🧪 Phase 1 Verification Guide

**Feature Branch**: `feature/user-memory-system`  
**Status**: Ready for Testing  
**Date**: October 10, 2025

---

## 🎯 What We're Testing

Phase 1 implemented the complete User Memory System foundation:
- Database schema (Profile, Events, Facts, Snapshot tables)
- Core operations (logging events, creating facts, building snapshots)
- Memory management (pruning, forgetting, statistics)

This guide shows you how to verify everything works before we instrument the real app.

---

## 🚀 How to Run the Test

### 1. **Build & Run the App**
```bash
cd /Users/davidortinau/work/dotnet-buildai/src
dotnet build Telepathic.sln -f net10.0-maccatalyst -c Debug
# Then run from VS Code or command line
```

### 2. **Navigate to Memory Debug Page**
- Open the app
- Tap the **hamburger menu** (☰) to open the flyout
- Scroll down and tap **"🧠 Memory Debug"**

---

## ✅ Verification Checklist

### Test 1: Create Test Data
1. Tap **"Create Test Data"** button
2. Wait for status message: "Test data created successfully!"
3. **Expected Result**: 
   - 5 test events logged (task:complete, task:create, project:view, voice:analyze)
   - 3 test facts created (morning_tasks, topic affinity, voice usage)
   - Snapshot automatically built

### Test 2: View the Data
1. Tap **"Refresh Data"** button
2. **Verify Statistics Section**:
   - Total Events: 5
   - Total Facts: 3
   - Snapshot Version: 1
   - Last Event: (shows recent timestamp)

3. **Verify Current Snapshot**:
   - Should show **8-16 lines** of formatted memory
   - Example lines:
     ```
     prefers.morning_tasks=true (s=0.8)
     affinity.topic:exercise=high (s=1.2)
     habit.uses_voice=frequently (s=0.9)
     recent.topics=exercise,email,shopping
     ```

4. **Verify Facts Section**:
   - Lists all 3 facts with scores
   - Sorted by score (highest first)
   - Shows update timestamps

5. **Verify Recent Events**:
   - Shows last 10 events (you have 5)
   - Each event shows: timestamp, type, topic, weight
   - Events in reverse chronological order (newest first)

### Test 3: Rebuild Snapshot
1. Tap **"Rebuild Snapshot"** button
2. Wait for confirmation
3. Tap **"Refresh Data"**
4. **Expected Result**:
   - Snapshot Version increments to 2
   - Snapshot content remains similar (high-confidence facts prioritized)

### Test 4: Create More Test Data
1. Tap **"Create Test Data"** again
2. Tap **"Refresh Data"**
3. **Expected Result**:
   - Total Events: 10
   - Total Facts: still 3 (same keys, scores increased)
   - Snapshot Version: may increment if threshold reached

### Test 5: Forget Me
1. Tap **"Forget Me"** button
2. Confirm the dialog
3. **Expected Result**:
   - All sections reset to "No ... yet"
   - Statistics show 0 for everything
   - Database cleared (except Profile table)

4. Tap **"Refresh Data"** to confirm everything is gone

---

## 🐛 What to Look For

### ✅ Success Indicators
- [ ] Database tables created without errors
- [ ] Events log successfully with timestamps
- [ ] Facts created/updated with correct scores
- [ ] Snapshot generates with 8-16 lines
- [ ] Snapshot includes high-confidence facts (score ≥ 0.8)
- [ ] Recent topics appear in snapshot
- [ ] Statistics are accurate
- [ ] "Forget Me" successfully wipes data
- [ ] No app crashes or exceptions

### ❌ Potential Issues to Report
- App crashes when tapping buttons
- Data not persisting between app restarts
- Snapshot not generating or empty
- Facts not incrementing scores
- Events not appearing in Recent Events list
- Timestamps in wrong timezone
- "Forget Me" doesn't clear all data

---

## 📱 Database Location

The memory database is stored at:
```
{FileSystem.AppDataDirectory}/memory.db3
```

On MacCatalyst, this is typically:
```
~/Library/Containers/{BundleId}/Data/Library/Application Support/memory.db3
```

You can inspect it with any SQLite viewer if needed.

---

## 🔍 Advanced Testing (Optional)

### Test Background Behavior
1. Create test data
2. **Close the app** completely
3. **Reopen the app**
4. Navigate to Memory Debug
5. Tap "Refresh Data"
6. **Expected**: All data persists across app restarts

### Test Snapshot Aging
1. Create test data (builds snapshot)
2. Wait 11+ minutes
3. Create one more event
4. **Expected**: Snapshot should rebuild automatically (version increments)

### Test Event Threshold
1. Create test data 3 times (15 events total)
2. **Expected**: Snapshot rebuilds automatically when event count hits threshold (15)

---

## 📊 What's Next?

Once Phase 1 verification passes:
- ✅ **Phase 2**: Instrument real app actions (task completion, project views, etc.)
- ⏳ **Phase 3**: Integrate snapshots with AI calls
- ⏳ **Phase 4**: Build "My Data" UI for users
- ⏳ **Phase 5**: AI-powered personalization

---

## 🆘 Troubleshooting

### "No snapshot yet" even after creating test data
- Check console logs for errors during snapshot build
- Verify facts were created (check Facts section)
- Try manually tapping "Rebuild Snapshot"

### Events not showing in Recent Events
- Check if database file exists in AppDataDirectory
- Verify no SQLite errors in logs
- Try "Forget Me" then recreate test data

### App crashes on startup
- Check if `IUserMemoryStore` is registered in DI
- Verify all constructor dependencies are available
- Check console for initialization errors

---

**Last Updated**: October 10, 2025  
**Tested By**: _[Your name here after testing]_  
**Result**: _[PASS / FAIL]_  
**Notes**: _[Any observations]_
