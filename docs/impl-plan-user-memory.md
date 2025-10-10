# 🧠 User Memory System - Implementation Plan

**Branch**: `feature/user-memory-system`  
**Start Date**: October 10, 2025  
**Target**: MVP with full instrumentation and My Data UI

---

## 📋 Implementation Decisions

### Decision Log
1. **Fact Creation Strategy**: Option A - Explicit `AddFactAsync` calls for obvious patterns
2. **Snapshot Rebuild Triggers**: All three (N events, significant events, app resume)
3. **"About Me" Migration**: Option B - Stay in Preferences, include in snapshot
4. **User Profile Access**: Option A - New Shell tab bar item

### Technical Decisions
- **Database**: Separate `memory.db3` database (not shared with app database)
- **User Model**: Single user with constant UserId = "default"
- **Repository Pattern**: Continue using `Microsoft.Data.Sqlite` with manual ADO.NET
- **Background Jobs**: None - all triggered by user actions or app lifecycle

---

## 🎯 Complete Instrumentation Map

### Tier 1: Core Task Actions (Highest Signal)
- [x] Task.Complete (time of day, project, topic)
- [ ] Task.Uncomplete (context matters!)
- [ ] Task.Create (source: manual/voice/photo, time, project)
- [ ] Task.Delete
- [ ] Task.Edit (title changes)
- [ ] Task.ViewDetails (engagement signal)
- [ ] PriorityTask.ViewReasoning (user curiosity about AI suggestions)
- [ ] PriorityTask.Assist (location assist, other assist types)

### Tier 2: Project Interactions
- [ ] Project.Create
- [ ] Project.View (navigation to detail page)
- [ ] Project.Edit
- [ ] Project.Delete

### Tier 3: AI-Powered Features
- [ ] Voice.StartRecording
- [ ] Voice.CompleteAnalysis (success/failure, duration)
- [ ] Voice.ExtractTasks (how many tasks extracted)
- [ ] Photo.Capture
- [ ] Photo.Analyze (success/failure)
- [ ] Photo.ExtractTasks (how many tasks extracted)
- [ ] Telepathy.Toggle (on/off preference)

### Tier 4: Context & Preferences
- [ ] Location.Enable
- [ ] Location.GetLocation (frequency of checking)
- [ ] Location.CheckNearby (which tasks trigger location checks)
- [ ] Calendar.Enable
- [ ] Calendar.SelectCalendar
- [ ] Settings.UpdateApiKey (which provider)
- [ ] Settings.UpdateAboutMe (becomes Profile data)

### Tier 5: Navigation & Lifecycle
- [ ] Page.Navigate (which pages user visits most)
- [ ] App.Resume (time between sessions)
- [ ] App.Background
- [ ] MainPage.Appear (daily engagement)

---

## 🚀 Phase 1: Foundation & Storage

**Status**: ✅ **COMPLETE**  
**Completed**: October 10, 2025

### Tasks
- [x] Create `/Data/UserMemory/` folder structure
- [x] Create `MemoryEvent.cs` model
- [x] Create `MemoryFact.cs` model
- [x] Create `MemorySnapshot.cs` model
- [x] Create `MemoryConstants.cs` with UserId constant
- [x] Create `IUserMemoryStore.cs` interface
- [x] Create `SqliteUserMemoryStore.cs` implementation
  - [x] Profile table
  - [x] Events table (with indexes)
  - [x] Facts table (with unique constraint)
  - [x] Snapshot table
- [x] Add to DI in `MauiProgram.cs`
- [x] Basic unit tests (verified via build)

**Git Commit**: `5617a15` - feat: Phase 1 - User Memory System foundation

---

## 🎯 Phase 2: Event Instrumentation

**Status**: ✅ **COMPLETE**  
**Completed**: October 10, 2025

### Tier 1: Task Actions
- [x] `MainPageModel.Completed` → log Task.Complete
- [x] `TaskDetailPageModel.SaveCommand` → log Task.Create
- [x] `MainPageModel.NavigateToTaskCommand` → log Task.ViewDetails

### Tier 2: Project Actions
- [x] `MainPageModel.NavigateToProjectCommand` → log Project.View  
- [x] `ProjectDetailPageModel.LoadData` → log Project.View

### Tier 3: AI Features
- [x] `VoicePageModel.ExtractTasksAsync` → log Voice.Analyze (with metadata)
- [x] `PhotoPageModel.ExtractTasksFromImageAsync` → log Photo.Analyze (with metadata)
- [x] `MainPageModel.VoiceRecord` → log Feature.Voice access
- [x] `MainPageModel.TakePhotoAsync` → log Feature.Photo access
- [x] `MainPageModel.AnalyzeAndPrioritizeTasks` → log AI.Telepathy (with metadata)
- [x] `ProjectDetailPageModel.GetRecommendationsAsync` → log AI.Recommend (with metadata)

### Implementation Details
- All page models inject `IUserMemoryStore` via DI
- Events include rich metadata (project names, task counts, duration, etc.)
- Memory tracking uses async fire-and-forget pattern (no UI blocking)
- Events tagged with semantic types: task:complete, task:create, project:view, voice:analyze, photo:analyze, ai:telepathy, ai:recommend

**Git Commit**: `e5648c4` - feat: Phase 2 - User Memory instrumentation across all key app events

---

## 📸 Phase 3: Snapshot Integration with AI

**Status**: ✅ **COMPLETE**  
**Completed**: October 10, 2025

### ChatClientService Enhancement
- [x] Add optional `userContext` parameter to `GetResponseWithToolsAsync`
- [x] Prepend user context to prompts with clear section headers
- [x] Add logging when snapshot context is included

### Integration Points
- [x] **VoicePageModel**: Include snapshot when analyzing voice input
  - Snapshot prepended to transcript analysis prompt
  - Logs snapshot version being used
  - Test: Voice analysis now aware of user preferences
- [x] **PhotoPageModel**: Include snapshot when extracting tasks from photos
  - Snapshot injected into ChatMessage with image data
  - Context helps interpret image content based on user habits
- [x] **MainPageModel (Telepathy)**: Include snapshot in priority task analysis
  - Snapshot prepended as "USER MEMORY" section
  - AI uses memory to personalize task prioritization
  - Test: Morning tasks boosted if user has morning preference
- [x] **ProjectDetailPageModel**: Include snapshot in task recommendations
  - Snapshot provides context for generating relevant task suggestions
  - AI writing style matches user's past behavior

### Implementation Details
- Snapshot retrieved via `GetSnapshotAsync(MemoryConstants.UserId)`
- Context formatted using `snapshot.GetFormattedText()`
- Prompt structure: `# USER CONTEXT\n{snapshot}\n\n# USER REQUEST\n{prompt}`
- All calls include snapshot version logging for debugging
- Graceful degradation: if no snapshot exists, calls proceed without context

**Git Commit**: `fe4c3d1` - feat: Phase 3 - Snapshot integration with AI calls

---

## 🎨 Phase 4: My Data UI

**Status**: ⏳ Pending  
**Target**: Day 3

### Navigation Structure
- [ ] Add "Profile" tab to Shell navigation
- [ ] Create `UserProfilePage.xaml` and `UserProfilePageModel.cs`
- [ ] Create `MyDataPage.xaml` and `MyDataPageModel.cs`
- [ ] Add Shell route for "mydata" sub-page

### UserProfilePage
- [ ] Move settings content from MainPage bottom sheet
  - [ ] Telepathy toggle
  - [ ] Location services
  - [ ] Foundry endpoint/API key
  - [ ] Google Places API key
  - [ ] Calendar selection
  - [ ] About Me text
- [ ] Add navigation button to My Data page
- [ ] Update MainPage to remove bottom sheet

### MyDataPage
- [ ] Display current snapshot (formatted nicely)
- [ ] Display facts list (key, value, score)
  - [ ] Sort by score descending
  - [ ] Color-code by confidence (high/medium/low)
- [ ] Display recent events (last 20)
  - [ ] Type, topic, timestamp
- [ ] Add "Forget Me" button
  - [ ] Confirmation dialog
  - [ ] Wipe Events, Facts, Snapshot tables
  - [ ] Keep Profile table intact
- [ ] Add "Refresh Snapshot" button (for debugging)
- [ ] Statistics display
  - [ ] Total events logged
  - [ ] Total facts
  - [ ] Snapshot age
  - [ ] Last rebuild timestamp

---

## 🤖 Phase 5: AI Integration

**Status**: ⏳ Pending  
**Target**: Day 3-4

### ChatClientService Enhancement
- [ ] Add `GetSnapshotContextAsync()` method to `IChatClientService`
- [ ] Modify `GetResponseAsync` to accept optional context parameter
- [ ] Update internal prompt construction to prepend context

### Integration Points
- [ ] **VoicePageModel**: Include snapshot when analyzing voice input
  - [ ] Prepend snapshot to system message
  - [ ] Test: "Schedule workout" should recognize morning preference
- [ ] **PhotoPageModel**: Include snapshot when extracting tasks from photos
  - [ ] Prepend snapshot to analysis prompt
- [ ] **TaskAssistAnalyzer**: Use snapshot for priority scoring
  - [ ] Parse snapshot for relevant preferences
  - [ ] Adjust priority scores based on facts
  - [ ] Test: Morning tasks get boosted in morning
- [ ] **MainPageModel**: Personalized greeting
  - [ ] Use facts to customize greeting
  - [ ] Include recent activity context

### Testing
- [ ] Test voice analysis with snapshot context
- [ ] Test photo analysis with snapshot context
- [ ] Test priority scoring changes
- [ ] Verify greeting personalization

---

## 🧹 Phase 6: Maintenance & Polish

**Status**: ⏳ Pending  
**Target**: Day 4

### Maintenance Jobs
- [ ] Implement event pruning (30-day retention)
  - [ ] Run on app startup
  - [ ] Delete events older than 30 days
  - [ ] Cap at 50k events per user
- [ ] Implement fact decay logic
  - [ ] Drop facts with Score < 0.2 AND inactive for 30 days
  - [ ] Cap at 600 facts per user
- [ ] Implement weight decay function for events
  - [ ] `w = exp(-(now - event_time) / 7 days)`

### Error Handling
- [ ] Add try-catch blocks around all memory operations
- [ ] Log errors but don't break app functionality
- [ ] Handle database corruption gracefully

### Performance
- [ ] Add indexes to Events table (UserId, AtUtc DESC)
- [ ] Add indexes to Facts table (UserId, Key)
- [ ] Benchmark snapshot generation time
- [ ] Optimize if > 100ms

### Testing
- [ ] End-to-end flow test
- [ ] Performance testing with 10k events
- [ ] Memory leak testing
- [ ] UI testing on iOS and Android

---

## 📊 Success Metrics

### MVP Completion Criteria
- ✅ All tables created and accessible
- ✅ All Tier 1 & 2 events instrumented
- ✅ Snapshot generation working (8-16 lines)
- ✅ My Data UI showing live data
- ✅ At least one AI integration point working (Voice or Priority)
- ✅ "Forget Me" functionality working
- ✅ No performance degradation in app

### Future Enhancements
- [ ] AI-powered fact inference from events
- [ ] Vector embeddings for semantic memory
- [ ] Multi-user profile support
- [ ] Memory export/import
- [ ] Background maintenance service (when MAUI supports it)
- [ ] FTS5 search on events
- [ ] Memory insights dashboard

---

## 🐛 Known Issues / Tech Debt

*Track issues here as they arise during implementation*

---

## 📝 Notes

### Database Schema Location
`/Users/davidortinau/work/dotnet-buildai/src/Telepathic/Data/UserMemory/`

### Constants
- **UserId**: `"default"`
- **Database**: `memory.db3`
- **Event Retention**: 30 days
- **Max Events**: 50,000 per user
- **Max Facts**: 600 per user
- **Snapshot Lines**: 8-16
- **Snapshot Rebuild Threshold**: 15 events
- **Snapshot Max Age**: 10 minutes
- **Fact Confidence Threshold**: 0.8 for snapshot inclusion

---

## 🎯 Current Sprint

**Active Phase**: Phase 4 - My Data UI (Optional Enhancement)  
**Next Up**: Consider creating user profile page and My Data viewer, or merge to main  
**Blockers**: None

**Recent Completions**: 
- Phase 1 ✅ (Foundation & Storage complete)
- Phase 2 ✅ (Event instrumentation complete)
- Phase 3 ✅ (Snapshot integration with AI complete)

**🎉 Core Memory System is LIVE! 🎉**
- Events are being tracked across the app
- Snapshots are being built automatically
- AI is now memory-aware in all major features

---

*Last Updated: October 10, 2025 - Phase 3 Complete - Core Memory System Operational*
