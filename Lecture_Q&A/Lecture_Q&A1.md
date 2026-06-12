**Game name:** Shadow Path



**Game:** A platform adventure game where the player navigates and overcomes obstacles by moving through the shadows cast by objects, rather than the physical objects themselves.



**Vertical slice:** The smallest playable version of the game is a single short level where the player controls a small character who must reach a goal by jumping across the projected shadows of simple objects.



**Player:** The player repeatedly adjusts the angle of the light source to change the shape and position of object shadows, then uses those shadows as platforms to move, jump, and reach the goal.



**Core system:** The core system is built around shadow-based platforming. The player-controller handles movement and jumping, while the shadow-platform system defines which projected shadows can be stood on. A light-projection-system connects lights, objects, and their shadows, allowing physical objects to create playable paths. The smallest version also requires a goal trigger and a simple game manager to handle level completion and player reset.



**GitHub status:** https://github.com/ColdFF/FF\_GamePro.git



**Biggest risk:** The biggest challenge is making the shadow-platform mechanic succeed as a clear, fair, and playable system. The player needs to easily understand how light angle changes the shadows and how those shadows can be used to reach the goal.



**Next action:** My next action is to build a very small playable prototype first. I will start by creating a simple Unity scene with a small player character, one light source, a few basic objects, and a goal area. Then I will test whether the character can move and jump on shadow platforms, and whether changing the light angle can create a usable path to the goal. At this stage, I will focus only on proving that the core shadow-platform mechanic works before adding extra levels, art, sound, or advanced puzzle features.



**What my game is:**

My game is a shadow-based puzzle platformer where the player uses the projected shadows of objects as platforms instead of the physical objects themselves.



**What the player does:**

The player repeatedly adjusts the angle of the light source to change the shape and position of shadows, then moves and jumps across those shadows to reach the goal.



**What I will build as my vertical slice:**

I will build one short playable level with a player character, one adjustable light source, several objects that cast shadow platforms, and a goal area. This vertical slice will demonstrate the core mechanic: changing the light angle to create a path and using the shadows to complete the level.



**Must-have:**

The game must include a controllable player character, basic movement and jumping, adjustable light angle, objects that cast shadows, shadow platforms the character can stand on, and one goal-based level.

**Should-have:**

The game should include smooth controls, clear visual feedback, simple UI for light control, checkpoints or restart, and basic sound effects.

**Could-have:**

The game could include multiple levels, moving objects, collectibles, different light sources, character animations, and extra puzzle variations.



