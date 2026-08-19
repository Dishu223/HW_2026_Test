# ? Doofus Adventure

A vibrant, fast-paced 3D arcade platformer built in **Unity 6**. Guide **Doofus**—a quirky, animated snowman—across floating green platforms called **Pulpits** before they crumble beneath his feet!

---

## ?? Story & Objective

Doofus loves exploring floating platforms, but there is a catch: **Pulpits don't last long and disappear within seconds!**

Your mission is simple:
- Guide Doofus across at least **50 Pulpits** to complete the challenge.
- Stay sharp: Pulpits are constantly ticking down and disintegrating.
- Fall into the void, and your adventure ends (unless you have a **Time Rewind** ready!).

---

## ?? Key Features

- **Living Character Animation**: Doofus leans, wobbles, and reacts dynamically to platform danger with changing eye expressions.
- **Dynamic Platform System**: Platform speed, lifetime, and spawn rates are driven by a configurable data file (`game_data.json`).
- **Shatter Effects**: When a pulpit timer reaches zero, it breaks into physical fragments and collapses.
- **Time Rewind Mechanic**: If Doofus slips off an edge, activate a slow-motion time rewind to pull yourself back onto safety!
- **Lobby & Customization**: Personalize Doofus with custom colors for his body, head, and eyes before hopping into the run.
- **Audio & Haptic Feedback**: Procedural sound effects, dynamic music, and celebratory milestones at 10, 25, and 50 points.

---

## ??? Controls

| Action | Key / Input |
| :--- | :--- |
| **Move** | `W`, `A`, `S`, `D` or `Arrow Keys` |
| **Start Game / Confirm** | `Space` or `Enter` |
| **Restart Run** | `R` |

---

## ??? Tech Stack & Architecture

- **Engine**: Unity 6 (`6000.5.7f1`)
- **Language**: C# (.NET / Mono)
- **Input**: Unity Input System
- **Camera**: Cinemachine with dynamic damping and screen shake
- **Architecture**: Decoupled, event-driven design (`GameEvents` bus) for clean, modular, and maintainable gameplay logic.

---

## ?? Development Roadmap & Milestones

- [ ] **Level 1**: Core Character Movement & Dynamic Platform Spawning from JSON Data
- [ ] **Level 2**: Step-by-Step Scoring System, Procedural Wobble & Visual Polish
- [ ] **Level 3**: Start Screen, Lobby Customization, Game Over, and Rewind System
- [ ] **Level 4**: Audio Integration, Particle Effects, Ambience & Final Polish

---

## ?? How to Run the Project

1. Clone this repository:
   ```bash
   git clone https://github.com/Dishu223/HW_2026_Test.git
   ```
2. Open **Unity Hub** and click **Add project from disk**.
3. Select this folder and open using **Unity 6 (6000.5.7f1)**.
4. Open the main scene located in `Assets/_Game/Scenes/GameScene.unity` and press **Play**!
