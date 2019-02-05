# NationStatesAPIBot
A Bot for performing API Actions on NationStates (in future mainly via discord)

I wrote this bot to support the [region](https://www.nationstates.net/region=the_free_nations_region "The Free Nations Region") where i am currently member of. 

It's purpose is mainly sending of Recruitment Telegrams. 
It will be extended as needed.

It can be probably used for general purpose as well.

Feel free to contribute!

# Usage - Commandline Reference

Available Commands in version v1.0:  
/help, ? - Shows this help.  
/exit, /quit - Terminates this program.  
/new - Fetches all new nations and prints them out.  
/region <region> - Fetches all nations from specific region and prints them out.  
/new-in-region <region> - Fetches all nations from specific region and matches them with nations of that region fetched before.  
/recruit - Start recruiting process. Enter again to stop recruiting. Dry Run per default. Disable Dry Run to go productive.  
/dryrun - Switches Dry Run Mode. No API Calls are performed as long Dry Run Mode is enabled.  
/loglevel <Loglevel> - Changes Loglevel to either DEBUG, INFO, WARN or ERROR.  

# Roadmap

Next features:
- check if recipient would receive telegram if not skip to next pending recipient by calling:
https://www.nationstates.net/cgi-bin/api.cgi?nation=%nationName%&q=tgcanrecruit&from=%regionName%

- Rebuilding Bot to DiscordBot using Discord.Net to control it mainly via discord. 

- Maybe writing required data to SQLite Database instead of direct file system write.

- BasicStats, ExtendedStats, CustomStats about nations and regions

- Backing Up Logs of Channels using Discord Chat Exporter

- Verify Nation ownership using Nation States Verification API: https://www.nationstates.net/pages/api.html#verification and automatic role asignment if verified after specified time.

- Basic Moderator Features (Kick, Ban, Delete Messages, etc.)
