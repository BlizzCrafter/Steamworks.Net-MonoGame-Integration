# Steamworks.Net MonoGame Integration
This repo is for everyone who is about to integrate the Steamworks.Net.dll into a MonoGame project.  It shows some Steamworks.Net features and how easy it is to integrate it into a MonoGame project.

![HelloSteamworks](Documentation/MonoGameSteamworksNet_03.jpg)
![HelloSteamworks](Documentation/MonoGameSteamworksNet_05.jpg)

### Building

The following is required to successfully compile the solution:

- MonoGame 3.5.1
- [Steamworks.Net](https://github.com/rlabrecque/Steamworks.NET) Precompiled .dlls are included in this repo. They are targeting **Steam SDK 1.39** (Steamworks.Net 9.0.0)

## Samples

- **Hello Steamworks.Net.csproj**: Simple sample which sets up bare basics of Steamworks.Net and displaying a welcome message which includes your steam user name.
- **Steamworks.Net MonoGame Integration.csproj**: Extendend sample which shows some features of Steamworks.Net like UserStats, PersonaState, LeaderboardData, NumberOfCurrentPlayers and so on.

> Note: You need to start your steam client before executing the examples. Otherwise you won't receive any data -obviously ;)

**Have fun!**
