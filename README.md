# NationStatesAPIBot
A Bot for performing API Actions on NationStates

I wrote this bot to support the [region](https://www.nationstates.net/region=the_free_nations_region "The Free Nations Region") where i am currently member of. 

It's purpose is mainly sending of Recruitment Telegrams. 
It will be extended as needed.

It can be used for general purpose as well.

Feel free to contribute!

# Usage - Command Reference

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

Next feature:
- check if recipient would receive telegram if not skip to next pending recipient by calling:
https://www.nationstates.net/cgi-bin/api.cgi?nation=%nationName%&q=tgcanrecruit&from=%regionName%

- Maybe merging or integrating an Discord Bot into it. 
