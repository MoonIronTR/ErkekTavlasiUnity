# Erkek Tavlasi Unity

Unity 6 project for a Turkish backgammon prototype. The current version keeps the 3D table view, 2D top-down view, shared C# game logic, legal move highlighting, dice physics, bearing-off flow, and a menu for human or AI practice modes in one Unity scene.

The project also includes an experimental Monte Carlo based AI/training path. That part is still under validation; the training flow exists, but AI strength and decision quality have not been fully tested yet.

## Opening The Project

1. Open Unity Hub.
2. Add this folder as a project.
3. Use Unity `6000.4.11f1` or a compatible Unity 6 editor.
4. Open `Assets/Scenes/BackgammonScene.unity`.
5. Press Play and choose either the 3D or 2D mode from the main menu.

## Running The Windows Build

The repository includes a Windows player build. Run:

```text
Builds/ErkekTavlasiUnity.exe
```

## Repository Contents

- `Assets/Scripts/Game`: pure C# backgammon state, move, dice, bar, hit, legal move, and bearing-off logic.
- `Assets/Scripts/AI`: experimental Monte Carlo AI, move features, position hashing, memory, and weight persistence.
- `Assets/Scripts/UnityView`: Unity scene controller, input handling, camera setup, dice view, UI wiring, animations, and highlights.
- `Assets/Editor/BackgammonSceneBuilder.cs`: editor script that can rebuild the generated scene objects from the Unity menu.
- `Assets/Textures`, `Assets/Materials`, `Assets/Prefabs`: procedural board visuals, materials, checkers, dice, and highlight prefabs.
- `LegacyPrototypes/WpfPrototype`: older WPF/C# prototype kept as reference code.

## Notes

Unity cache folders, logs, user settings, temporary QA screenshots, and local editor output are intentionally excluded from the repository.
