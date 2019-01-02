# ProjectDMG

**ProjectDMG is a C# coded emulator of the Nintendo Game Boy wich was originally codenamed Dot Matrix Game (DMG).**

*This is a personal project with the scope to learn about hardware and the development of emulators.*

ProjectDMG dosn't use any external dependency and uses rather simplistyc C# code.

All the CPU opcodes are implemented and it passes Blaarg tests.
The emulator have accurate cycle timings to the opcode degree. Including Game Boy related hardware design flaws/bugs as the HALT bug, as some software relies on this specific behaviour.

> **Note:**  This is not a M-Cycle or micro-ops accurate emulator.
> Accuracy and syncronization between the various hardware subsystems as the Pixel Processing Unit (PPU), the Memory Managemet Unit (MMU) or the Timer relies on hardcoded fixed values and varies from 4 to 24 CPU cycles depending on the executed Opcode or hardware interrupts.

## Compability

Game Boy catalog compatibility should be around +95% as the support have focused on the most popular cartridges types (MBCs 1,2,3 and 5). The full list goes as follows:

|Supported Cartridge Types  | Unsupported Cartridge Types |
|--|--|
|00h  ROM ONLY  |  08h  ROM+RAM |
|01h  MBC1  |  09h  ROM+RAM+BATTERY |
|02h  MBC1+RAM  |   0Bh  MMM01|
|03h  MBC1+RAM+BATTERY  |  0Ch  MMM01+RAM |
|05h  MBC2  |  0Dh  MMM01+RAM+BATTERY |
|06h  MBC2+BATTERY  | 1Ch  MBC5+RUMBLE |
|0Fh  MBC3+TIMER+BATTERY  | 1Dh  MBC5+RUMBLE+RAM |
|10h  MBC3+TIMER+RAM+BATTERY  | 1Eh  MBC5+RUMBLE+RAM+BATTERY |
|11h  MBC3  | 20h  MBC6 |
|12h  MBC3+RAM  | 22h  MBC7+SENSOR+RUMBLE+RAM+BATTERY |
|13h  MBC3+RAM+BATTERY  | FCh  POCKET CAMERA |
|19h  MBC5  | FDh  BANDAI TAMA5 |
|1Ah  MBC5+RAM  | FEh  HuC3 |
|1Bh  MBC5+RAM+BATTERY  |  FFh  HuC1+RAM+BATTERY |

> **Note:**  SRAM save files are not supported at the moment so your progress on games will be lost on exit.


## Using the emulator

Execute the emulator and drag and drop a valid Game Boy rom dump to the GUI. The Game Boy will power on and begin execution.

> **Note:**  A valid Game Boy BootRom/BIOS file must be provided on the root folder of the emulator as: DMG_ROM.bin
 
Once power on, Input is mapped as:

* D-Pad UP: **Up** or **W**
* D-Pad Left: **Left** or **A**
* D-Pad Down: **Down** or **S**
* D-Pad Right: **Right** or **D**
* A: **Z** or **J**
* B: **X** or **K**
* Start: **V** or **Enter**
* Select: **C** or **Space**

## Screenshots

![blaargcpu](https://user-images.githubusercontent.com/28767885/50447289-692f0680-091a-11e9-92b6-583e7262658e.PNG)
![blaarginstrtiming](https://user-images.githubusercontent.com/28767885/50447290-692f0680-091a-11e9-9937-b4f23bd6d169.PNG)
![k1](https://user-images.githubusercontent.com/28767885/50447291-69c79d00-091a-11e9-89fd-c1931b37e30d.PNG)
![k2](https://user-images.githubusercontent.com/28767885/50447293-69c79d00-091a-11e9-927a-090f098f542b.PNG)
![m1](https://user-images.githubusercontent.com/28767885/50447294-69c79d00-091a-11e9-9965-e5ded1888aa2.PNG)
![m2](https://user-images.githubusercontent.com/28767885/50447295-69c79d00-091a-11e9-9780-82e5a8c8a90b.PNG)
![t](https://user-images.githubusercontent.com/28767885/50447296-6a603380-091a-11e9-9e2a-54b5c4d71b9f.PNG)
![z](https://user-images.githubusercontent.com/28767885/50447297-6a603380-091a-11e9-984b-c4a5ca9d20c3.PNG)
![p](https://user-images.githubusercontent.com/28767885/50447298-6a603380-091a-11e9-9b6a-fde84205564d.PNG)
![p2](https://user-images.githubusercontent.com/28767885/50447299-6a603380-091a-11e9-86fe-4e50c70f0f3e.PNG)
