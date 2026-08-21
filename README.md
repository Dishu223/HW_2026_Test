# ⛄ DOOFUS ADVENTURE — 50 Shades of Green

[![Unity 6](https://img.shields.io/badge/Unity-6000.5.7f1-black?logo=unity&logoColor=white)](https://unity.com/)
[![Render Pipeline](https://img.shields.io/badge/URP-Universal%20Render%20Pipeline-blue)](https://unity.com/srp/universal-render-pipeline)
[![Architecture](https://img.shields.io/badge/Architecture-Event--Driven%20%2F%20SOLID-emerald)](https://github.com/Dishu223/HW_2026_Test)
[![GitHub Repository](https://img.shields.io/badge/GitHub-Dishu223%2FHW__2026__Test-181717?logo=github)](https://github.com/Dishu223/HW_2026_Test)

> **A high-juice 3D arcade platformer built in Unity 6 (URP) featuring dynamic adjacent platform spawning, procedural snowman animations, a 360° Character Customizer, and a full *Prince of Persia: Warrior Within*-inspired Time-Rewind System.**

---

## 📸 Visual Showcase & Previews

| In-Game Action & Platform Lifecycles | 360° Circular HSV Character Studio |
| :---: | :---: |
| <img src="./screenshots%20and%20videos/Screenshot%202026-08-21%20203822.png" width="100%" alt="In-Game Action & Timers" /> | <img src="./screenshots%20and%20videos/Screenshot%202026-08-21%20203804.png" width="100%" alt="360 HSV Character Studio" /> |
| *Active pulpit countdowns, Character dashing, score tracker & rewind charges* | *Live 360° HSV color wheel & 3D character customization* |

| Start Screen & Lobby UI | Game Over & Time Rewind Overlay |
| :---: | :---: |
| <img src="./screenshots%20and%20videos/Screenshot%202026-08-21%20091530.png" width="100%" alt="Start Screen & Lobby" /> | <img src="./screenshots%20and%20videos/Screenshot%202026-08-21%20202141.png" width="100%" alt="Game Over & Stats" /> |
| *Arcade lobby with custom character preview* | *Unity Editor view* |

### 🎥 Full Gameplay & Rewind Video Demonstration

<div align="center">

> 🎬 **Watch the full gameplay and rewind system in action**:

<a href="https://www.youtube.com/watch?v=hgogVKnqOVE">
  <img src="https://img.youtube.com/vi/hgogVKnqOVE/0.jpg" alt="Full Gameplay & Rewind Video Demonstration" width="600">
</a>

</div>

## 📖 Table of Contents
- [🎮 Game Overview](#-game-overview)
- [⏳ Spotlight: Prince of Persia Time-Rewind System](#-spotlight-prince-of-persia-time-rewind-system)
- [🕹️ Controls & Keybindings](#️-controls--keybindings)
- [✨ Core Features & Mechanics](#-core-features--mechanics)
- [🏗️ Architecture & Engineering](#️-architecture--engineering)
- [⚙️ JSON Configuration](#️-json-configuration)
- [🏆 Evaluation Matrix & Features](#-evaluation-matrix--features)
- [🛠️ Build & Run Instructions](#️-build--run-instructions)
- [🔗 Repository Link](#-repository-link)

---

## 🎮 Game Overview

Take control of **Doofus**, a lovable procedural snowman, as he runs across an endlessly shifting abyss of dissolving platforms called **Pulpits**:

- ⏱️ **5-Second Platform Lifespan**: Every pulpit counts down from green to yellow to red before shattering into the void.
- 🎲 **Procedural Adjacent Spawning**: Platforms spawn in real-time right beside your current platform with forward-biased random walks.
- 🏁 **The 50-Pulpit Challenge**: Traverse 50 pulpits without falling to achieve victory!
- ⏳ **Rewind Charges**: Fatal fall? Reverse time and try the jump again!

---

## ⏳ Spotlight: Prince of Persia Time-Rewind System

### 🗡️ The Childhood Inspiration
Growing up, one of my absolute favorite games was **_Prince of Persia: Warrior Within_**. That iconic mechanic—where slipping into an abyss or mistiming a deadly jump wasn't an immediate game over, but an opportunity to unleash the **Sands of Time** and reverse reality—left a lasting impression on me.

For **Doofus Adventure**, I recreated that magical experience from scratch in Unity 6!

```mermaid
flowchart LR
    A[Normal Gameplay] -->|Fatal Fall / Trigger| B[0.12x Slow-Mo Fall]
    B -->|Time Reverse| C[3.2x Dynamic Rewind Curve]
    C -->|World Sync| D[3D Shard Reassembly & Path Restore]
    D -->|Pause on Platform| E[WASD Tactical Resume]
    E -->|Input Detected| A
```

---

### 🧠 How It Works Under the Hood (Simple & Intuitive)

The rewind system is engineered across five synchronized layers to make time reversal feel seamless, responsive, and cinematic:

#### 1. 📜 Continuous History Ring Buffer (The Sandglass)
During normal gameplay, [`RewindManager.cs`](file:///d:/Antigravity%20Projects/50%20Shades%20of%20Green/DoofusAdventure/Assets/_Game/Scripts/Core/RewindManager.cs) records Doofus’s exact **position, rotation, and timestamp** on every physics step (`FixedUpdate`) into a high-performance `LinkedList<PlayerSnapshot>`. It stores up to 3.5 seconds of historical snapshots while automatically pruning older frames to keep memory overhead near zero.

#### 2. 🎬 Phase 1: Dramatic Slow-Motion Fall ($0.12\times$)
The moment a fatal fall is detected, time slows down to **$0.12\times$ speed** for 0.50 seconds in real-time. This mirrors the heart-stopping cinematic suspense of *Warrior Within*, giving you that punchy moment of tension before the time reversal takes over.

#### 3. ⏪ Phase 2: Dynamic Non-Linear Rewind Curve ($3.2\times$)
Rather than playing history back at a flat speed, the reversal follows an acceleration curve:
- **Ease-In ($0.35\times \to 3.2\times$)**: Starts smoothly as Doofus begins rising from the abyss.
- **High-Speed Rush ($3.2\times$)**: Sprints backwards through the falling trajectory.
- **Ease-Out ($3.2\times \to 1.0\times$)**: Decelerates smoothly as Doofus lands back safely onto solid ground.

#### 4. 🧩 Phase 3: Mid-Air Shard Reassembly & Deterministic Path Memory
Time doesn't just reverse for Doofus—the entire world rewinds in unison:
- **3D Fracture Reassembly** ([`PlatformShatterFX.cs`](file:///d:/Antigravity%20Projects/50%20Shades%20of%20Green/DoofusAdventure/Assets/_Game/Scripts/Platform/PlatformShatterFX.cs)): Platforms that shattered into 9–13 physics debris pieces rewind their trajectories and organically snap back into place.
- **Deterministic Platform Layout** ([`PulpitManager.cs`](file:///d:/Antigravity%20Projects/50%20Shades%20of%20Green/DoofusAdventure/Assets/_Game/Scripts/Platform/PulpitManager.cs)): Platform coordinates are cached in a sequence history so that after rewinding, the exact same platform layout and timings are maintained without random desynchronization.

#### 5. 📺 Phase 4: Retro Glitch VFX, Tape-Warp Audio & WASD Resume
- **Authentic Retro Glitch** ([`RewindScreenUI.cs`](file:///d:/Antigravity%20Projects/50%20Shades%20of%20Green/DoofusAdventure/Assets/_Game/Scripts/UI/RewindScreenUI.cs)): Rolling CRT scanlines, RGB chromatic aberration, and VHS tracking noise play during the rush, instantly clearing when landing.
- **Pitch-Warped Audio** ([`SoundManager.cs`](file:///d:/Antigravity%20Projects/50%20Shades%20of%20Green/DoofusAdventure/Assets/_Game/Scripts/Core/SoundManager.cs)): Background music dynamically slows down and a tape-rewind whoosh plays in reverse.
- **Tactical Pause**: Once safely on the platform, the game pauses (`Time.timeScale = 0`) displaying `>> PRESS WASD OR SPACE TO RESUME <<`, giving the player full control over when to step back into action!

---

## 🕹️ Controls & Keybindings

| Action | Primary Key | Secondary Key | Description |
| :--- | :--- | :--- | :--- |
| **Move** | `W` `A` `S` `D` | `Arrow Keys` | Move Doofus with smooth acceleration & procedural body lean |
| **Heroic Dash** | `Left Shift` | `Right Shift` | $3.2\times$ burst dash with exaggerated head lag ($0.65\text{m}$) & spring recovery |
| **Start / Resume** | `Space` | `Enter` / Click | Launch game from lobby or resume from rewind touchdown |
| **Quick Retry** | `R` | `Space` / Click | Instantly restart a fresh run from the Game Over screen |
| **Color Studio** | `Mouse Drag` | — | Interactive 360° Circular HSV Color Wheel in the Lobby |

---

## ✨ Core Features & Mechanics

### ⛄ Procedural Character Animation
- **Dynamic Lean**: Leans $22^\circ$ during normal runs and up to $38^\circ$ during high-speed dashes.
- **Head Lag & Spring Recovery**: Snowman head trails realistically behind the base when dashing and bounces back via spring physics.
- **Animated Expressions**: Wind-speed eye widening during sprints, 3-phase blinking, and sudden stopping brake whip.

### 🎨 360° Character Studio (HSV Color Picker)
- Live 360° circular hue-saturation color wheel generated via procedural math ($H = \text{atan2}(y, x), S = r/R$).
- Real-time 3D turntable preview for customizing Doofus’s **Body**, **Head**, and **Eyes**.
- Persistent custom skins saved automatically across sessions via `PlayerPrefs`.

### 💥 Procedural 3D Platform Shatter FX
- Organic platform fracture system creating 9 to 13 varied stone/tile shards per platform.
- Radial wave collapse where the center sinks and corner shards tumble outward into the abyss.
- Seamless reverse reassembly during time rewinds.

### 🎵 High-Juice Audio & Retro VFX Suite
- Alternating left/right footstep boops (muted during dash bursts).
- Procedural 44.1 kHz synthesizer audio fallbacks for all SFX.
- Full-screen VHS scanlines, VCR tracking noise bar, and URP Chromatic Aberration.

---

## 🏗️ Architecture & Engineering

The codebase is built on **SOLID principles** and an **Event-Driven Decoupled Architecture**:

```mermaid
graph TD
    Input[Input System / Player] --> DoofusCtrl[DoofusController.cs]
    DoofusCtrl --> Events[GameEvents.cs]
    Events --> ScoreMgr[ScoreManager.cs]
    Events --> SoundMgr[SoundManager.cs]
    Events --> VFXMgr[VFXManager.cs]
    Events --> PulpitMgr[PulpitManager.cs]
    Events --> RewindMgr[RewindManager.cs]
    Events --> HUD[HUDController.cs]
    Events --> GameOver[GameOverUI.cs]
    Events --> RewindUI[RewindScreenUI.cs]
    GameConfig[GameConfig.cs] -->|Loads JSON| PulpitMgr
    GameConfig -->|Loads JSON| DoofusCtrl
```

### Module Breakdown:
- **`GameEvents.cs`**: Static event hub eliminating hard coupling between UI, Audio, VFX, and Gameplay systems.
- **`RewindManager.cs`**: Sands of Time rewind engine handling snapshot caching, slow-mo dilation, dynamic playback curves, and resume pauses.
- **`PulpitManager.cs`**: Platform lifecycle engine ensuring a maximum of 2 active platforms, anti-backtracking memory, and deterministic spatial path caching.
- **`DoofusController.cs` & `DoofusAnimator.cs`**: Physics locomotion, shift dashes, and procedural snowman animations.
- **`PlatformShatterFX.cs`**: 3D physics fragmentation and mid-air reassembly.
- **`SoundManager.cs`**: Multi-track music player with tape-pitch warping and procedural SFX generation.
- **`CustomizationManager.cs` & `ColorWheelPicker.cs`**: 360° HSV texture generator and turntable controller.

---

## ⚙️ JSON Configuration

Core balance parameters are loaded dynamically from `StreamingAssets/game_data.json` at startup:

```json
{
  "player_data": {
    "speed": 3
  },
  "pulpit_data": {
    "min_pulpit_destroy_time": 5,
    "max_pulpit_destroy_time": 5,
    "pulpit_spawn_time": 2.5
  }
}
```

- If `game_data.json` is missing or contains invalid syntax, [`GameConfig.cs`](file:///d:/Antigravity%20Projects/50%20Shades%20of%20Green/DoofusAdventure/Assets/_Game/Scripts/Core/GameConfig.cs) automatically catches the error and applies safe fallback defaults without crashing.

---

## 🏆 Evaluation Matrix & Features

| Requirement / Feature | Implementation Details | Status |
| :--- | :--- | :---: |
| **Level 1: Core Mechanics** | Dynamic pulpit spawning, 5s countdown lifecycle, color shift (Green $\to$ Yellow $\to$ Red), max 2 active platforms, player physics movement, JSON config loader. | ✅ **100% Complete** |
| **Level 2: Animation & Polish** | Procedural snowman lean & head lag, stopping brake whip, wide panic eyes, isometric camera follow with screen shake, high score persistence. | ✅ **100% Complete** |
| **Level 3: Game Flow & UI** | Start Screen Studio, Chunky Arcade Marquee HUD (`SCORE : X`, `BEST : Y`), Rewind Charges Battery, Game Over screen with score ticker, 50-pulpit challenge banner, instant retry. | ✅ **100% Complete** |
| **Extra: Prince of Persia Rewind** | Deterministic WorldTime buffer, $0.12\times$ slow-mo dilation, dynamic $3.2\times$ time-reversal curve, mid-air platform shard reassembly, interactive WASD resume. | ✅ **100% Complete** |
| **Extra: 3D Shatter FX** | Procedural 3D organic shards crumbling into the abyss with reverse-physics reassembly. | ✅ **100% Complete** |
| **Extra: Character Studio** | 360° Circular HSV Color Wheel for Body, Head, and Eyes with live 3D preview and PlayerPrefs persistence. | ✅ **100% Complete** |
| **Extra: Heroic Shift Dash** | $3.2\times$ burst on Shift with exaggerated $0.65\text{m}$ head drag backward and spring recovery. | ✅ **100% Complete** |
| **Extra: Audio & VFX Suite** | Alternating boop footsteps, BGM pitch warping, outward tile placement dust, VHS RGB glitch overlay. | ✅ **100% Complete** |

---

## 🛠️ Build & Run Instructions

### In Unity Editor:
1. Open the project in **Unity 6 (6000.5.7f1)** with Universal Render Pipeline (URP).
2. Open scene: `Assets/Scenes/SampleScene.unity`.
3. Press **Play** ▶!

### Standalone Windows Build:
1. In Unity, go to **File ➔ Build Profiles** (or **Build Settings**).
2. Ensure `Assets/Scenes/SampleScene.unity` is listed in **Scenes In Build**.
3. Select Target Platform: **Windows, Mac, Linux** ➔ Architecture: **x86_64**.
4. Click **Build and Run** ➔ Choose destination folder (e.g., `Builds/DoofusAdventure.exe`).

---

## 🔗 Repository Link

- **GitHub Repository**: [https://github.com/Dishu223/HW_2026_Test](https://github.com/Dishu223/HW_2026_Test)

---

*Crafted with my love for gaming and game development!.*
