# MemoryEngine: Advanced C# Game Trainer Framework

MemoryEngine is a professional-grade C# framework built for developers to create game trainers, memory manipulation tools, and reverse engineering utilities. The framework simplifies complex Windows API interactions, providing a robust abstraction layer for memory reading, writing, and inline code injection (detours).

## Key Features

* **Robust Memory Management:** Efficiently read and write arbitrary data types, structures, and pointer chains within external processes.
* **Pattern Scanning:** Robust AOB (Array of Bytes) scanning to locate game functions and variables dynamically, ensuring compatibility across different game versions.
* **Modern Assembly Manipulation:** Integrated support for the 'Iced' assembler, allowing for safe, high-performance runtime assembly modification without external native dependencies.
* **Advanced Hooking (Detours):** Powerful trampoline hook implementation that intercepts game execution, redirects the flow, and restores original instructions seamlessly.
* **Memory Freezing:** Built-in background stabilization engine to keep memory values locked at user-defined states.
* **Symbol Registry:** Integrated variable management to track, name, and control memory addresses globally.

## Technical Architecture

### Core Modules

* **Engine:** The foundation layer. It handles process identification, handle management, and privilege escalation (virtual memory protection). It acts as the primary interface for all raw memory access operations.
* **PatternScanner:** Designed to solve the "static address" problem. By utilizing byte pattern matching, it maps memory segments of game modules to find offsets at runtime, eliminating the need for hardcoded address updates.
* **Hooking & Code Caves:** This system allows for precise code modification. It utilizes "Code Caves" (newly allocated memory regions) to house custom logic. When a function is hooked, the system redirects execution to the cave, performs the injected logic, and safely returns to the original code flow.
* **Iced Assembler:** The brain of the injection system. It disassembles original game code into manageable instructions, allowing the user to modify or remove specific logical operations directly through the library without writing brittle, manual assembly strings.

## Stability & Safety

The framework focuses heavily on process integrity. By using advanced memory protection handling (VirtualProtectEx), it ensures that modifications do not trigger access violations within the target process.

**Critical Cleanup:**
To maintain stability and prevent process crashes, the framework includes a complete cleanup mechanism. It is mandatory to execute a global cleanup routine upon application closure. This process removes all active hooks, restores original byte sequences to the target memory, and releases all reserved memory regions (Code Caves). Failure to perform this step can lead to process instability or game crashes after the trainer has been closed.

## Prerequisites

* **.NET 6.0 or newer.**
* **Iced NuGet Package:** Required for internal assembly decoding and encoding.

## Disclaimer

MemoryEngine is strictly intended for educational purposes, software research, and single-player game modifications. The author bears no responsibility for any misuse, violations of game Terms of Service, or any damages arising from the use of this framework.