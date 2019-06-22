# NationStatesAPIBot
A Bot for performing API Actions on NationStates and to provide other features via DiscordAPI. It can be controlled via discord.

I wrote this bot for the discord server of the [region](https://www.nationstates.net/region=the_free_nations_region "The Free Nations Region") where i am currently member of. 

It can send automatically recruitment telegrams via the NationStates API.
It will check if the recipient would receive the telegram before actually sending it, if not it will be skipped. Which increases the efficency because no telegrams are wasted.

It is intended to provide:
  - some statistics from NationStates API to discord users
  - verification of nation ownership via the NationStates API (To-Do)
  - backup functionality of discord channel chat logs. (To-Do)
  - basic moderation functionality for authorized users (To-Do)
  
It will be extended as needed.

It can probably be used for general purpose as well.

Feel free to contribute!

# Roadmap

**!! IMPORTANT NOTICE !!**  
The features are **NOT** guaranteed to ship with the specified version -> Feature order may change without notice.  
Release times are **NOT** guaranteed and may change without notice. 

All Points prefixed with Maybe are not know if there will be implemented with that version yet.

---
## Version 3

### Version 3.1 (Planned for July 15th)
- Implement Cache First Approach with help of dumps, etc.
- Add refounded nations to pending  
- Get Nations that were endorsed by a nation (endorsed) (on cache only) (and nne too)
- Stats about success of manual and automatic recruitment -> - (send, skipped, failed, pending count) for total from dump and db  
- Maybe (- Polls)

### Version 3.2 (Planned for Mid/Late August)
- Allow nations to have multiple status
- Help Command
- Polls  
- Maybe (- CleanUp of nation pool on regular basis)  
- Maybe (- Introduce Basic RP Economy & Finance System)  

### Version 3.3 (Planned for Late September)
- Introduce Basic RP Economy & Finance System
- Activity Points for verified nations on NationStates activity and discord activity -> Extensive RP Economy Features
- SpamDetect/Report to Moderators (for discord & RMB)
- Maybe (- RMB <-> Discord Bridge)

## Later (unordered)
- Backing Up Logs of Channels using Discord Chat Exporter
- Introduce Citizenship Management
- Verify Nation ownership using Nation States Verification API: https://www.nationstates.net/pages/api.html#verification and possibly automatic role asignment, if verified. e.g. Role "Verified"
- UpdateTime Command for R/D  
(- Generate Spreadsheet for R/D)?
- Basic Moderator Features (Kick, Ban, Mute, Delete Messages, etc.)
- Games (Werewolf)  
- RMB <-> Discord Bridge
