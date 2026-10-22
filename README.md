# MH2 Quest Checklist

A desktop checklist for the quests of **Monster Hunter 2 (dos)** — the PlayStation 2 MH2, released **only in Japan** (SLPM-66280 / SLPM-74245).

![.NET](https://img.shields.io/badge/.NET-9.0-blue)
![Platform](https://img.shields.io/badge/platform-Windows-lightgrey)
![Save access](https://img.shields.io/badge/save-readonly-green)

---

## Why this tool exists

MH2 is the only game in the series where the quest list **rotates**:

- Quests change based on the **season** the game is currently in (温暖期 warm / 寒冷期 cold / 繁殖期 breeding);
- Many quests only become available **under conditions** (accumulated HRP, previous quests completed, etc.);
- The game **never shows the full list** anywhere — you only see what is available at that moment.

The result is easy to imagine: with dozens of quests per season, players completely lose track of **what they have already completed** and **what is still missing** to reach 100%.

This tool solves that on two levels:

1. **Complete list, always visible** — every quest (Village, Town and Events) with season, fee, reward, HRP, time, locale and goals, in English and/or Japanese;
2. **Save import** — load a `.psu` save file and the tool decodes it, reads each quest's completion flag and automatically marks **what that character has already completed**. Whatever remains unmarked is exactly what is left to do.

Preview:
<img width="1151" height="719" alt="image" src="https://github.com/user-attachments/assets/4b94bc23-c90d-4dcb-a74f-0b75f4b6444e" />

## How it works

### For the user

1. Export your save as `.psu`. On PC, use **[mymc-alpha](https://www.ps2-home.com/forum/viewtopic.php?t=449)** to open the memory card image and extract the `BISLPM-66280MH2` save (on a real console, uLaunchELF works as well).
2. Place the `.psu` file in the `savedata/` folder next to the executable.
3. Click **Load Save**. The tool lists the files it finds and, if there is more than one, opens a picker.
4. The save is **decoded in memory** and the card's 3 slots (play00/01/02) appear in the slot dropdown, identified by the **character name**.
5. Selecting a slot marks all quests that character has completed (**Village + Town**), with overall and per-category progress shown at the top.
6. You can also mark/unmark quests **manually** — clicking anywhere on the row or on the checkbox.

> **Events are tracked manually.** Event quests never mark their completion in the save file, so the tool cannot detect them — mark them by hand in the Events group.

State (checks, language, theme, last save/slot, window position) is saved automatically to `data/config.json`.

> The tool is **read-only**: it never writes to the `.psu` and does not modify the original save in any way.

### Features

| Feature | Description |
|---|---|
| Groups | Village (Offline), Town (Online/Practice), Events (manual tracking) |
| Categories | Basics/Free/Urgent/Defense (Village) · practice/free_low/free_high/exams/defense/tournament (Town) |
| Columns | ✔ · ID (hex) · ★ · name · season (colored) · fee · reward · HRP · time · locale · requirements · goals |
| Sorting | Click any column header (asc/desc, per tab) |
| Search | Field at the top, comma-separated terms, partial match on name, season, locale, requirements and goals (EN and JP) |
| Language | **English / 日本語** toggle for the whole UI and quest names |
| Theme | **Dark / Light / System** (dark title bar included) |

## Dependencies (user)

- **Windows 10/11** (x64);
- **.NET 9 Desktop Runtime** — [download](https://dotnet.microsoft.com/download/dotnet/9.0) (the "Run desktop apps" option);
- A `.psu` save from the **Japanese Monster Hunter 2** (`BISLPM-66280MH2` folder) — disc versions SLPM-66280/74245. Official foreign releases of the game do not exist, but the layout was mapped for this region.

There are no other dependencies: the quest database (`data/quests.json`) ships with the package.

## For devs

### Building

Requirements:

- **.NET 9 SDK** (any 9.0.x) — the WPF workload comes included with the SDK on Windows;
- Optional: **Visual Studio 2022** (17.12+) with the "*.NET desktop development*" workload.

Commands (from the project root):

```powershell
# development build
dotnet build

# run from source
dotnet run --project MH2QuestChecklist.csproj

# framework-dependent release (requires the Desktop Runtime on the target machine)
dotnet publish -c Release

# self-contained release (portable output folder, no runtime installation required)
dotnet publish -c Release -r win-x64 --self-contained true
```

The build automatically copies `data/quests.json` to the output and creates an empty `savedata/` folder at the destination.

### Project structure

```
MH2QuestChecklist.csproj    WPF project (.NET 9, no external NuGet packages)
App.xaml(.cs)               lifecycle + theming
MainWindow.xaml(.cs)        main layout, column sorting, row toggle
Models/                     QuestData (quests.json schema) and AppState
ViewModels/                 ShellViewModel, QuestGroup, CategoryTab, QuestItem,
                            SavePickerWindow (lightweight MVVM)
Services/                   Mh2SaveCodec (.psu container + decoder),
                            VillageQuestMap / TownQuestMap (bit → quest),
                            DataService (quests.json + config.json)
Themes/                     theme dictionaries (Dark/Light)
data/quests.json            quest database (village/Town/events, EN+JP)
savedata/                   working folder for the user's .psu files
```

---

## Special thanks

This tool only exists because the community discovered (and published) much of the knowledge required. Many thanks to:

- **[MH-Oldschool Wiki](https://wiki.mholdschool.com/mh2/quests)** — the English MH2 quest database that fed `data/quests.json`, and the hub of the classic MH community;
- **[MH2Kaiseki (MH2解析)](https://mh2kaiseki.web.fc2.com/index.html)** — the Japanese MH2 analysis site, our reference for quest data and Japanese terminology;
- **[MH-Oldschool/MHSaveRepair](https://github.com/MH-Oldschool/MHSaveRepair)** (2Tie, 2023) — documentation of the `176 mod 0xFF53` LCG cipher in MH1/MHG saves, the starting point that confirmed MH2's save algorithm;
- **[MH-Oldschool/mhpatcher](https://github.com/MH-Oldschool/mhpatcher)** (2026) — implementation (`src/dnas/savecodec.c`) of the same codec for MH2's `patch.bin`, with the algorithm reverse engineered from the original binaries by those authors;
- **[crowded-street/3s-decomp](https://github.com/crowded-street/3s-decomp)** — the Street Fighter III 3rd Strike decompilation, which let us identify `PL2ENC` as a CRI middleware audio component and **rule it out** as the save cipher (dead end eliminated);

*Monster Hunter is a trademark of Capcom. This is an unofficial, fan-made project and is not affiliated with Capcom.*
