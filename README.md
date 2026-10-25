# BoneSwinger

BoneSwinger is a lightweight Unity asset that adds dynamic, physics-like swinging to 2D bone chains (hair, ribbons, skirts, tails...) built with SpriteSkin / 2D Animation. Existing physics engines might seem like a viable solution, but they often present unexpected challenges, such as:

- Difficulty in achieving the desired motion due to complex settings.
- Tedious setup, requiring component adjustments for each bone.
- Erratic behaviors like sudden "explosions" or objects flying offscreen.
- Gradual misalignment or detachment of bone connections during extended simulations.
- **Critical Issue**: Physics engines don't work properly when bones are moved by an Animator.

BoneSwinger avoids these pitfalls with a small, stable simulation that only rotates bones (never moves them), works on top of Animator output, and is tuned with a handful of intuitive parameters.

https://private-user-images.githubusercontent.com/11781380/388940330-f82edffe-7f76-4985-8ec8-91f8407d4d0b.mp4

- [日本語解説/Japanese guide](https://qiita.com/Shinoda_Naoki/items/ec19ef8d610b939dc312) (describes the legacy `BoneDragger`)

---
## Features
- **Works with Animator and Transform changes**: The pose written by the Animator each frame is used as the rest pose, so animated and non-animated bones both swing naturally. Any Animator update mode is supported.
- **Scale and frame-rate independent**: All lengths are normalized by the chain length and time is advanced in fixed sub-steps, so the same parameters look the same in any scene, at any character size and fps.
- **Stable**: Air drag and inertia are integrated exactly, the restoring spring is solved implicitly and never overshoots, so even extreme settings do not explode.
- **Intuitive parameters**: Inertia, air drag, stiffness, damping, angle limit, gravity and wind, each with a clear physical meaning.
- **Natural flipping**: Turning around by negative X scale leaves the chain where it was, and it then swings over to the new side.

---
## Installation
Clone the repository and load it as a Unity project (Unity 2022.3).
```bash
git clone https://github.com/ShinodaNaoki/boneSwinger.git
```

---
## Quick start
Open `Assets/Scenes/SwingCompare.unity` and press Play. The legacy `BoneDragger` (left) and the new `BoneSwing` (right) are shown side by side with the same motion. Use the on-screen panel (or keys 1-8) to switch motions and tweak every `BoneSwing` parameter live. If the scene is missing, regenerate it with **Tools > BoneSwinger > Create Comparison Scene**.

---
## How to use

1. **Prepare SpriteSkin**: Set bones for the parts to swing, and add a short extra bone at each end of a chain. The last bone of a chain only marks the tip and is not rotated.
2. **Flip by scale**: Use IK for limbs if needed, and flip characters with negative X scale instead of Y rotation.
3. **Add `BoneSwing`**: Add the component to any GameObject and list the chain's bones from root to tip. Existing `BoneDragger` setups can be converted with the **Convert to BoneSwing** button in the `BoneDragger` inspector, so you can still use `BoneDraggerManager`'s keyword-based bulk setup to create the chains and then convert them.
4. **Tune parameters**: Edit the settings on the component, or create a shared `BoneSwingProfile` asset (**Create > BoneDragger > Create BoneSwing Profile**) and assign it to several chains or characters.

### Parameters (`BoneSwingSettings`)

| Parameter | Range | Meaning |
|---|---|---|
| Inertia | 0–1 | 0: no momentum, moves only with the air (ribbon). 1: keeps momentum and swings back like a pendulum. |
| Air Drag | 0–1 | How strongly the air holds the chain. Higher values make it trail along the direction of motion and return slowly. |
| Stiffness | 0–1 | Strength of returning to the rest (animated) pose. Swing period ≈ 2π√(Inertia / (Stiffness × 200)) s. |
| Damping | 0–1 | Damping ratio of the bending motion only. 1 stops without swinging back. Unlike air drag, it does not make the chain trail. |
| Flutter | 0–1 | Wave-like fluttering near the tip, proportional to the speed relative to the air (ribbons, flags). |
| Angle Limit | 1–180° | Maximum bend from the rest pose. Bending gets harder as it approaches the limit (soft limit). |
| Hardening | 1–8 | How the soft limit stiffens: 2 = gradually like bamboo, 6+ = soft until just before the limit like a rubber stopper. |
| Gravity / Gravity Direction | 0–2 | Gravity in chain lengths per second². |
| Wind | vector | Air velocity in chain lengths per second. |
| Simulate In Local Space | bool | Simulate in the character's space, ignoring the character's own movement and flips. |
| Stiffness Curve | curve | Stiffness multiplier from root (0) to tip (1). Low only at the root = swings like a stick; lower toward the tip = whip-like bending. |
| Air Drag Curve | curve | Air-catching area multiplier from root (0) to tip (1). |

Mass and air-catching area of each bone are proportional to its length (a uniform rope); the curves only redistribute them.

### How it works
Each joint of the chain is a particle; the root follows the animated bone.

1. **Inertia and air drag**: `m·dv/dt = c·(v_air − v) + F` is solved exactly for each step (exponential integration), stable even when mass or drag is zero.
2. **Length constraint**: Position-based dynamics distributes corrections by how hard each particle is to move (inertia + drag). With zero inertia the chain trails behind its root like a towed rope, which is what makes it turn toward the direction of motion.
3. **Bending**: Each joint's bend angle θ is pulled back by a hardening spring `T(θ) = K·θ / (1 − |θ/θmax|^p)`, solved implicitly together with the downstream moment of inertia, air drag and damping. Every joint swings with the same period, so tips do not jitter.
4. **Apply**: Particle directions are applied to the bones as rotation deltas on top of the animated pose (in parent space, so negative scale works).

It runs in `LateUpdate` with execution order -3: after the Animator, before IK (-2) and SpriteSkin deformation (-1).

---
## Legacy: BoneDragger
The original implementation (`BoneDragger`, `BoneDraggerParameters`, `BoneDraggerManager`) is still included and described in the [Qiita article (Japanese)](https://qiita.com/Shinoda_Naoki/items/ec19ef8d610b939dc312). `BoneSwing` replaces it with a more stable and scale-independent simulation and fewer parameters.

---
## Contributing
Contributions are always welcome! Feel free to modify the code as you like. If you have a great improvement idea, I'd be happy to hear about it.

---
## License
This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
