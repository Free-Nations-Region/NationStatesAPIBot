# NationStatesAPIBot
A Bot for performing API Actions on NationStates and to provide other features via DiscordAPI. It can be controlled via discord.

I wrote this bot for the discord server of the [region](https://www.nationstates.net/region=the_free_nations_region "The Free Nations Region") where i am currently member of. 

It can send automatically recruitment telegrams via the NationStates API.

It is intended to provide:
  - some statistics from NationStates API to discord users (To-Do)
  - verification of nation ownership via the NationStates API (To-Do)
  - backup functionality of discord channel chat logs. (To-Do)
  - basic moderation functionality for authorized users (To-Do)
  
It will be extended as needed.

It can probably be used for general purpose as well.

Feel free to contribute!

# Configuration - v2.0+

The order of the lines is irrelevant. Write them into a file named "keys.config" in your execution directory.
Required:

`clientKey=<your nation states clientKey>`  
`telegramId=<your nation states recruitment telegramId>`  
`secretKey=<your nation states telegram secretKey>`  
`contact=<your nation states nation or an email address or something like that>`  
`dbConnection=<your mysql database connection string>`  
`botLoginToken=<your discord bot login token>`  
`botAdminUser=<discord user id how is main admin on this bot>`  
  
Optional:  
`logLevel=<logLevel 0-5 0 = Critical - 5 = Debug>`
See Discord.Net.Core LogSeverity for details

Be sure to have at least dbConnection configured when you run `dotnet ef database update`

# Roadmap

Next features:
- migrate earlier file writes to database with EntityFrameworkCore

- check if recipient would receive telegram if not skip to next pending recipient by calling:
https://www.nationstates.net/cgi-bin/api.cgi?nation=%nationName%&q=tgcanrecruit&from=%regionName%

- BasicStats, ExtendedStats, CustomStats about nations and regions

- Backing Up Logs of Channels using Discord Chat Exporter

- Verify Nation ownership using Nation States Verification API: https://www.nationstates.net/pages/api.html#verification and automatic role asignment if verified after specified time.

- Basic Moderator Features (Kick, Ban, Delete Messages, etc.)
