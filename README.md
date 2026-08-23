# Info
JustBedwars is a Hypixel Bedwars Companion App for finding Player Stats.  
It was strongly inspired by [Abyss Overlay](https://github.com/Chit132/abyss-overlay) and is basically just a recreation of it running on WinUI with some extra features and optimization with how Hypixel hides usernames while waiting. 

# Installation
Go [Here](https://fex2000.github.io/JustBedwars/) and click "Download Installer", after there, I hope you have the knowledge of opening an .exe.  
Only Windows 11 (and maybe 10, but untested) on x64 is supported.

# Usage
1. On the welcome screen, set the location of your log file
2. Join a round of BedWars and after it starts run `/who`

# Features
- Showing stats of players in your current round
  - Ordering players based on their skill from stats
  - Detecting Nicks
- Exploring Hypixel Bedwars Leaderboards
- Finding stats for a specific player
- Showing information about a guild

# Linux/Mac Support?
Linux is currently under development as a native GTK App. If you have the required libraries installed, just clone the repo and use `dotnet run ./src/JustBedwars.GTK` to test the current version.  
GTK also work works on Mac, but you would need to build it yourself and install GTK4 + Adwaita. It also isn't tested, most features probably won't work at all.