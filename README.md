# BoneSwinger

BoneSwinger is a lightweight Unity asset designed to create dynamic, physics-like swinging motions for objects. While existing physics engines might seem like a viable solution for animating bones, they often present unexpected challenges, such as:  

- Difficulty in achieving the desired motion due to complex settings.  
- Tedious setup, requiring component adjustments for each bone.  
- Erratic behaviors like sudden "explosions" or objects flying offscreen.  
- Gradual misalignment or detachment of bone connections during extended simulations.  
- **Critical Issue**: Physics engines don't work properly when bones are moved by an Animator.  

BoneSwinger solves these issues with a simple and intuitive system that avoids these pitfalls while delivering realistic results. Whether it's animating tails, ropes, or other flexible structures, this tool makes it easy and efficient to create smooth, controlled motions with minimal effort.


https://private-user-images.githubusercontent.com/11781380/388940330-f82edffe-7f76-4985-8ec8-91f8407d4d0b.mp4

- [日本語解説/Japanese guide](https://qiita.com/Shinoda_Naoki/items/ec19ef8d610b939dc312)

---
## Features
- **Supports Animator and Transform Changes**: Even when using an Animator where the existing physics engine does not work, it can sway just like with Transform changes.
- **Simplified Settings**: Multiple clusters (bone chains) can be registered in bulk by specifying keywords. Parameters are managed centrally, making it easy to copy and paste.
- **Simplified Physics Calculations**: It expresses air resistance and inertia while maintaining a lightweight design that prevents bones from collapsing.

---
## Installation
Clone the repository and load it as a Unity project. The package includes several sample scenes to explore and experiment with.
   ```bash
   git clone https://github.com/ShinodaNaoki/boneSwinger.git
   ```

---

## How to Use

1. **Prepare SpriteSkin**: Set bones for the part to be swing animated, adding short bones at the ends.  
2. **IK and Animator Setup**: Use IK for limbs if needed and apply X-axis scaling instead rotation for flipping.  
3. **Centralized Management Controller**: Use BoneDraggerManager for automatic bone setup.  
4. **Adjust Parameters**: Customize air resistance, inertia, etc., with BoneDraggerParameter.  
5. **Manual Setup**: Add BoneDragger manually if automatic setup is insufficient.

For more details, check the [Qiita article (Japanese)](https://qiita.com/Shinoda_Naoki/items/ec19ef8d610b939dc312).

---
## Contributing
Contributions are always welcome! Feel free to modify the code as you like. If you have a great improvement idea, I'd be happy to hear about it.

---
## License
This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
