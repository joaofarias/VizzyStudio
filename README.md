# Vizzy Studio

A mod for Juno: New Origins (aka Simple Rockets 2) to improve and expand Vizzy, including usability improvements and brand new features.

Please report any bugs on the issues tab with as much information as possible to reproduce it, including a copy of the player log (AppData\LocalLow\Jundroo\SimpleRockets 2\Player.log).

On the very first time the game runs with this mod, a backup of your program files will be created next to the originals (AppData\LocalLow\Jundroo\SimpleRockets 2\UserData\FlightPrograms_backup). If anything goes wrong, remove the mod and restore the program files.

### Roadmap

#### v0.1 - Current Version
- Modules: organize your program into multiple modules for quick and easy access to specific logic
  - All blocks are global. You can have custom expressions/instructions in one module and use them in another
  - Move blocks between modules by dragging them to the chosen module on the right-hand side and placing them in a single movement
  - Last position and zoom level per module are saved so you can get right back into it
  - Imported programs are added to a new module with the program's name (unless it already has modules of its own)

Known issues: when importing a program with modules that match existing modules' name, those modules will be merged.

#### v0.2 - In development
- References: use custom expressions/instructions from external files without importing them into your program
  - Easier to maintain and update common logic
  - Prevents code duplication

#### v0.3
- Drag a single block out of a group
- Add shortcuts (e.g. Del key to delete selected block)

#### v0.4
- Local variables: variables available only within the current instruction set
  - Reduces clutter in the variables panel
  - Allows for cleaner, more readable code

#### v0.5
- 'Return' block: return values from custom instructions
  - Cleaner code by reducing the need for global variables
  - Allows better code partioning

#### v1.0
- Bugfixing
- Polish
