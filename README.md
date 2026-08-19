# Doofus Adventure 🎮

A charming, fast-paced 3D arcade platformer built in **Unity 6 (6000.5.7f1)**.

Guide **Doofus**, a lovable snowman-like adventurer, across disappearing 9x9 green metallic platforms called **Pulpits**. The goal is simple yet challenging: survive and step across at least **50 Pulpits** without falling into the void!

---

## 🌟 Game Overview

- **Dynamic Platform Lifecycles**: Each Pulpit has an internal countdown timer. As time ticks down, the platform shifts color from green to warning yellow and pulsing red before shattering into pieces!
- **Adjacent Platform Spawning**: Only up to 2 Pulpits exist at any given time. A new pulpit spawns adjacent to the active one based on the configured spawn interval.
- **Configurable Gameplay**: Player speed, spawn rates, and platform lifespans are dynamically loaded from a configuration file (`game_data.json`).
- **Character Personality**: Procedural physics-based character movement with natural body lean, bouncy head movement, and expressive eyes that react dynamically to the remaining platform timer.
- **Character Customization Lobby**: Customize Doofus with custom colors for body, head, and eyes before diving into the run.
- **Rewind Time Mechanic**: Accidentally fell off the edge? A slow-motion rewind ability gives you another chance to reverse back in time and save your run (limited uses per game).
- **Juicy Polish**: Visual particle bursts on movement and landings, dynamic camera follow with screen shake, escalating audio feedback with musical note progressions, and ambient lighting that evolves as you reach score milestones.

---

## 🕹️ Controls

| Action | Key / Input |
|---|---|
| **Move** | `W`, `A`, `S`, `D` or `Arrow Keys` |
| **Start / Confirm** | `Space` or `Enter` |
| **Quick Restart** | `R` |

---

## 🏗️ Architecture & Technical Design

The project is built with clean, modular, and decoupled C# systems adhering to modern Unity best practices:

- **Decoupled Event Bus (`GameEvents.cs`)**: Systems communicate via static C# events, keeping gameplay, audio, UI, and VFX independently testable and maintainable.
- **Robust Configuration Loader (`GameConfig.cs`)**: Handles JSON parsing with comprehensive fallback defaults to ensure zero crashes even if files are missing or malformed.
- **State Machine Management (`GameManager.cs`)**: Clear state transitions (`StartScreen` ➔ `Lobby` ➔ `Playing` ➔ `Rewinding` ➔ `GameOver`).
- **Procedural Animations (`DoofusAnimator.cs`)**: Math and physics-driven wobble, squashes, and leans without heavy animation clip overhead.
- **Snapshot-Based Rewind (`RewindManager.cs`)**: Lightweight circular buffer recording game states for smooth reverse playback.

---

## 🗺️ Roadmap & Milestones

- [ ] **Level 1: Core Mechanics & Configuration**
  - Character movement with normalized input and physics collision.
  - JSON configuration loader with full validation.
  - Dynamic adjacent pulpit spawning (max 2 active platforms).
  - Platform countdown timer, diegetic warning colors, and fall triggers.

- [ ] **Level 2: Scoring & Progression**
  - Score updates on visiting new platforms.
  - Milestone fanfare and celebrations at 10, 25, and 50 pulpits.
  - Dynamic ambient color evolution based on score tier.

- [ ] **Level 3: UI & Menus**
  - Title / Start Screen with animated prompts.
  - Customization Lobby for player colors.
  - In-game HUD with live score, timer indicator, and rewind counter.
  - Game Over screen with animated score tally, best score persistence, and instant retry.

- [ ] **Level 4: Game Juice & Polish**
  - Rewind Time mechanic with slow-motion and reverse playback.
  - Pre-fractured platform shatter physics and particle effects.
  - Sound effects, musical pitch scaling, and soundtrack loop.
  - Cinemachine camera follow and screen shake feedback.

---

## 🛠️ Requirements & Setup

1. **Unity Version**: Unity 6 (`6000.5.7f1`) or newer.
2. **Render Pipeline**: Universal / Built-in 3D.
3. **Packages**: TextMeshPro, Cinemachine, Input System.

---

*Stay on your toes and reach 50 Pulpits!*
