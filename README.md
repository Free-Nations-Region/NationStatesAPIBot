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

# Configuration - v2.1+

The order of the lines is irrelevant. Write them into a file named "keys.config" in your execution directory.  

Required:

`clientKey=<your nation states clientKey>`  
`telegramId=<your nation states recruitment telegramId>`  
`secretKey=<your nation states telegram secretKey>`  
`contact=<your nation states nation or an email address or something like that>`  
`dbConnection=<your mysql database connection string>`  
`botLoginToken=<your discord bot login token>`  
`botAdminUser=<discord user id how is main admin on this bot>`  
`regionName=<name of the region you are recruiting for>`
  
Optional:  
`logLevel=<logLevel 0-5 0 = Critical - 5 = Debug>`
See Discord.Net.Core LogSeverity for details

Be sure to have at least dbConnection configured when you run `dotnet ef database update`.  
You need to have a copy of "keys.config" in the directory where you execute `dotnet ef database update` or `dotnet ef migrations add <name>`

# Roadmap

**!! IMPORTANT NOTICE !!**  
The features are **NOT** guaranteed to ship with the specified version. Feature order may change without notice.  
Release periods are **NOT** guaranteed and may change without notice. 

---
## Version 3
  
### Version 3.0 (Planned for Mid June)
- Huge Refactoring to Configuration, Logging, Testability, API call systematics
- Maintain Existing Functionallities 

### Version 3.1 (Planned for Mid July)
- Help Command
- Implement Cache First Approach with help of dumps, etc.
- Add refounded nations to pending  
- Get Nations that were endorsed by a nation (endorsed) (on cache only)

### Version 3.2 (Planned for Early August)
- UpdateTime Command for R/D  
(- Generate Spreadsheet for R/D)?

### Version 3.3 (Planned for Early/Mid September)
- Allow nations to have multiple status
- Introduce Citizenship Management
- Verify Nation ownership using Nation States Verification API: https://www.nationstates.net/pages/api.html#verification and possibly automatic role asignment, if verified. e.g. Role "Verified"

### Version 3.4 (Planned for Early/Mid October)
- Introduce Basic RP Economy & Finance System

## Version 4

### Version 4.0
- Backing Up Logs of Channels using Discord Chat Exporter

### Version 4.1
- BasicStats about the recruitment process (send, skipped, failed, pending count) for total and since the recruitment process was last started and in general from database

### Version 4.2
- Stats about success of manual and automatic recruitment

## Later
- CustomStats about nations and regions
- Activity Points for verified nations on NationStates activity and discord activity -> Extensive RP Economy Features
- Basic Moderator Features (Kick, Ban, Mute, Delete Messages, etc.)
- Games (Werewolf)  
- Polls  
