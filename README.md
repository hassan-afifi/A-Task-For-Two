# A Task For Two

<p align="center">
  <img width="650" height="400" alt="LogoTex" src="https://github.com/user-attachments/assets/c24fdfc8-3713-4785-af7e-7d261dea33e1" />
  [![Download for Windows](https://img.shields.io/badge/Download-Windows-blue?style=for-the-badge&logo=windows)](../../releases/latest)
</p>

![Unity](https://img.shields.io/badge/Unity-6000.3_LTS-black?logo=unity)
![C#](https://img.shields.io/badge/C%23-.NET-purple?logo=csharp)
![License](https://img.shields.io/badge/License-CC_BY--NC--ND_4.0-blue)

A Task For Two is an online cooperative multiplayer puzzle game developed in **Unity** as my Bachelor's thesis project.

The project implements a polished multiplayer gameplay vertical slice in which two players communicate and cooperate to solve an asymmetric puzzle in a synchronized online environment. It focuses on multiplayer networking, software architecture, real-time synchronization, automated testing, and the delivery of a polished player experience.


---

## Overview

This project demonstrates the design and implementation of a complete online cooperative gameplay vertical slice.

Players create or join a multiplayer session using Unity Relay and Unity Lobby, solve an asymmetric cooperative puzzle by exchanging information in real time, and complete a shared objective. Alongside the gameplay, the project includes a modular architecture, persistent settings, automated testing, and robust multiplayer session handling.

---

## Features

- Online cooperative multiplayer gameplay
- Unity Relay & Lobby integration
- Host and join-by-code sessions
- Real-time synchronized gameplay
- Asymmetric cooperative puzzle
- Shared victory flow
- Host disconnect recovery
- Extensive video, audio, and HUD settings
- Persistent player preferences
- Automated Edit Mode and Play Mode testing

---

## Technologies

| Category | Technologies |
|-----------|--------------|
| Language | C# |
| Engine | Unity 6 (6000.3 LTS) |
| Multiplayer | Unity Netcode for GameObjects |
| Networking Services | Unity Relay, Unity Lobby |
| UI | Unity UI Toolkit, TextMeshPro |
| Version Control | Git, Git LFS |
| Development | Visual Studio Code |

---

## Software Engineering

This project applies modern software engineering practices, including:

- Modular object-oriented architecture
- Multiplayer state synchronization
- Event-driven programming
- Automated Edit Mode and Play Mode testing
- Version control with Git
- Separation of gameplay, networking, and UI systems
- Persistent data management
- Clean project organization

---

# Screenshots

## Main Menu

<img width="1920" height="1080" alt="MainMenu" src="https://github.com/user-attachments/assets/89cc7d0b-e21c-419f-a8a1-e009fc7ae73a" />

---

## Multiplayer Lobby

Players can create private multiplayer sessions and invite another player using a generated lobby code.

<img width="1920" height="1080" alt="SecondPlayer" src="https://github.com/user-attachments/assets/6e081f21-4883-49f8-b302-eb41ebab3b1a" />

---

## Gameplay

Players explore the environment together while communicating to solve a synchronized cooperative puzzle.

<img width="1920" height="1080" alt="GameView" src="https://github.com/user-attachments/assets/e5c43758-5d25-4f31-b70c-f718097e9b12" />

---

## Cooperative Puzzle

Each player sees only part of the puzzle. Communication is required because each player controls the value needed to complete their partner's equation.

<img width="1920" height="1080" alt="PuzzleA" src="https://github.com/user-attachments/assets/5db71e22-8346-4700-ab4e-8cf0fb9c52a8" />

---

## Audio Settings

Customize separate volume levels for music, gameplay sound effects, and menu audio.

<img width="1920" height="1080" alt="AudioOptionsMenu" src="https://github.com/user-attachments/assets/a06a40ed-a394-49d7-b030-ec1f77ca64fd" />

---

## HUD Settings

Customize crosshair appearance, FPS and ping counters, and the in-game system clock.

<img width="1920" height="1080" alt="HudOptionsMenu" src="https://github.com/user-attachments/assets/37b4f8cd-f779-4f7f-95d1-a68c66ad0787" />

---

## Pause Menu

Pause gameplay at any time while maintaining multiplayer session state.

<img width="1920" height="1080" alt="PauseMenu" src="https://github.com/user-attachments/assets/c2ab45b5-7e7c-42bd-b2f7-495e790ab135" />

---

## Victory Screen

Complete the cooperative puzzle to finish the level.

<img width="1920" height="1080" alt="EndScreen" src="https://github.com/user-attachments/assets/8b5e724f-b77d-4533-825e-659806995003" />

---

## Getting Started

### Requirements

- Unity 6 (6000.3 LTS)
- Git
- Git LFS

### Clone the repository

```bash
git clone https://github.com/hassan-afifi/A_Task_For_Two.git
```

Enable Git LFS

```bash
git lfs install
git lfs pull
```

Open the project using **Unity 6 (6000.3 LTS)**.

---

# Download

**Windows Build**

Download the latest playable version from the Releases page.

https://github.com/hassan-afifi/A_Task_For_Two/releases/latest

---

## Repository Structure

```
Assets/
├── Animations/
├── Audio/
├── Materials/
├── Models/
├── Prefabs/
├── Scenes/
├── Scripts/
├── Settings/
├── Tests/
└── Textures/

Packages/
ProjectSettings/
```

---

## License

This project is licensed under the **Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License**.

https://creativecommons.org/licenses/by-nc-nd/4.0/
