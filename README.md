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
The features are **NOT** guaranteed to ship with the specified version. Feature order may change without notice.  
Release periods are **NOT** guaranteed and may change without notice. 

---
## Version 3
  
### Version 3.0 (Planned for June 20th/21st)
- Huge Refactoring to Configuration, Logging, Testability, API call systematics
- Maintain Existing Functionallities 

### Version 3.1 (Planned for July 15th)
- Implement Cache First Approach with help of dumps, etc.
- Add refounded nations to pending  
- Get Nations that were endorsed by a nation (endorsed) (on cache only) (and nne too)

### Version 3.2 (Planned for Mid August)
- BasicStats about the recruitment process (send, skipped, failed, pending count) for total and since the recruitment process was last started and in general from database
- Stats about success of manual and automatic recruitment
- Allow nations to have multiple status
- Help Command

## Later (unordered)
- Introduce Basic RP Economy & Finance System
- Backing Up Logs of Channels using Discord Chat Exporter
- Introduce Citizenship Management
- Verify Nation ownership using Nation States Verification API: https://www.nationstates.net/pages/api.html#verification and possibly automatic role asignment, if verified. e.g. Role "Verified"
- UpdateTime Command for R/D  
(- Generate Spreadsheet for R/D)?
- Activity Points for verified nations on NationStates activity and discord activity -> Extensive RP Economy Features
- Basic Moderator Features (Kick, Ban, Mute, Delete Messages, etc.)
- Games (Werewolf)  
- Polls
- RMB <-> Discord Bridge
- SpamDetect/Report to Moderators (for discord & RMB)
