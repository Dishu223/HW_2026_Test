# 🎮 Doofus Adventure

A vibrant, fast-paced 3D arcade platformer built in **Unity 6**. Guide **Doofus**—a quirky, animated snowman character—across disappearing floating platforms called **Pulpits** before they crumble beneath his feet!

---

## 📖 Story & Objective

Doofus loves exploring floating green platforms, but there's a catch: **Pulpits don't last long and disappear within seconds!**

- **Goal**: Guide Doofus across at least **50 Pulpits** to complete the challenge.
- **Danger**: Each Pulpit has an internal countdown. When time runs out, the platform shatters into pieces and collapses.
- **Falling**: Stepping off an edge or staying on a shattered platform causes Doofus to fall into the abyss.
- **Rewind**: If you slip, activate a **Time Rewind** to pull yourself back to safety!

---

## 🌟 Key Planned Features

- **Living Character Animation**: A 2-sphere snowman body that procedurally leans, bobs, and reacts with expressive eyes (happy, worried, panicked, falling).
- **Dynamic Platform System**: Pulpit lifetimes, spawn rates, and Doofus speed configured cleanly via JSON data (`game_data.json`).
- **Platform Shatter FX**: Physical fracturing and explosion physics when platforms expire.
- **Prince of Persia Style Rewind**: Slow-motion camera, screen blur, and reverse time playback on fatal falls (limited uses per run).
- **Lobby & Customization**: Customize the colors of Doofus's body, head, and eyes before starting a run.
- **Juicy Audio & Visuals**: Ascending pitch musical milestones, heartbeat warning cues, walk particles, confetti celebrations, and evolving ambient lighting.

---

## 🗺️ Development Roadmap

### 🏁 Level 1: Core Movement & Platform Spawning
- [ ] Read configuration values (`speed`, `destroy_time`, `spawn_time`) from JSON data
- [x] Physics-based character controller with WASD / Arrow key movement
- [x] Platform manager spawning adjacent platforms (max 2 active simultaneously)
- [x] Individual platform countdown timers and destruction
- [x] Fall detection and boundary handling

### 🎯 Level 2: Scoring & Progression
- [ ] Score tracking for every unique pulpit stepped on
- [ ] Visual HUD displaying live score and platform timer
- [ ] Milestone celebrations at 10, 25, and 50 pulpits

### 📺 Level 3: Game Flow & UI
- [ ] Start Screen with animated intro and controls guide
- [ ] Character Customization Lobby
- [ ] Game Over Screen with final score and instant restart
- [ ] Time Rewind sequence and HUD counters

### ✨ Extra Polish & Game Juice
- [ ] Procedural wobble, tilt, and eye reactions
- [ ] Platform shatter and particle effects
- [ ] Audio manager with sound effects and music
- [ ] Dynamic ambient color shifts

---

## 🎮 Controls

| Action | Key / Input |
|---|---|
| **Move Forward** | `W` or `Up Arrow` |
| **Move Backward** | `S` or `Down Arrow` |
| **Move Left** | `A` or `Left Arrow` |
| **Move Right** | `D` or `Right Arrow` |
| **Start / Confirm** | `Space` / `Enter` |
| **Restart Game** | `R` |

---

## 🛠️ Tech Stack
- **Engine**: Unity 6 (6000.5.7f1)
- **Language**: C# (.NET Standard)
- **Render Pipeline**: Built-in 3D
- **Input**: Unity Input System

