# Telepathy 🚀✨
https://devblogs.microsoft.com/dotnet/using-ai-foundry-with-dotnet-maui/
Welcome, space adventurer! Telepathy is your futuristic to‑do companion that goes beyond simple lists—leveraging AI, voice, photos, and context to become your personal **task-o-matic** navigator.

[![AI infused mobile and desktop apps with .NET MAUI YouTube video](images/youtube_poster.png)](https://youtu.be/tFOFU7LDQlA?si=F1O4QajdvlZEr1UE)

Slides (PPT): [Microsoft Build 2025 - AI infused mobile & desktop app development with .NET MAUI](docs/2025%20Build%20-%20AI%20Infused%20MAUI.pptx)

---

## 🚀 Getting Started

1. **Install & Launch**  
   Clone the repo or grab Telepathy from your favorite store and fire it up on your device—mobile or desktop.

2. **Add Your OpenAI and/or Azure AI Foundry Key**  
   Under **Settings**, paste your OpenAI API key and/or AI Foundry endpoint and key. This unlocks Telepathy’s AI superpowers: smart task suggestions, context‑aware prioritization, and that legendary “voice‐analysis” mode.

3. **Connect Your Calendar**  
   Link Google, Outlook, or iCloud calendars so Telepathy can see your schedule. It will optimize your tasks around meetings, deadlines, and travel time.

4. **Enable Location & Notifications**  
   Allow location access to trigger reminders at the right place—home, office, or cosmic café. Enable notifications so you never lose track of a mission-critical chore.

5. **Activate “Telepathy Mode”**  
   Hit the **Telepathy** toggle to awaken AI‑powered organization. Watch as your plain to‑do list transforms into an optimized daily plan—sorted by context, priority, and your own habits.

---

## 🎤 Voice & Photo Powers

- **Voice Analysis**  
  Tap the microphone icon and speak your stream-of-consciousness. Telepathy will parse your ramblings into projects and neatly structured tasks—no typing required!

- **Photo Tasking**  
  Snap a photo of a whiteboard, sticky note, or receipt. Telepathy’s AI will recognize actionable items and add them directly to the right project.

---

## 🔧 Core Features

- **MVVM Architecture** powered by .NET MAUI and the .NET Community Toolkit  
- **Dynamic Themes & Styles** with `AppThemeResource` for light/dark mode  
- **Virtualized Lists** via `CollectionView` for large sets, and `BindableLayout` for compact lists  
- **AI‑Driven Task Prioritization** that learns from your behavior  
- **Seamless Calendar Integration** to auto‑schedule buffer times  
- **Geo‑Context Reminders** triggered by location  
- **Rich Voice & Photo Input** for zero‑effort task capture  

---

## 🛰️ Architecture Overview

```
/Pages            → UI in XAML & C#  
/PageModels       → ViewModels with RelayCommands  
/Services         → OpenAI, calendar, location, and audio services  
/Resources/Styles → Centralized Colors.xaml, Styles.xaml, AppStyles.xaml  
/Data             → Repositories & seed data  
/Utilities        → Helpers & converters  
```

We follow best practices:
- Use `<Border>` and `<Grid>` for modern layouts  
- Leverage `RelayCommand` for async commands in XAML  
- Keep styling in merged resource dictionaries  

---

![screenshots](images/telepathy-screens.png)

## 🌌 Join the Mission

Telepathy is a living galaxy—your feedback and contributions propel it forward.  
Create an issue, submit a PR, or just say hello in discussions. Together, we’ll build the ultimate mind‑reading to‑do companion!  

May your tasks be ever in your favor. ✨  

— The Telepathy Crew
