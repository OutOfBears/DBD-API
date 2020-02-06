# DBD-API
Dead By Daylight API

## What is this for?
It allows you to communicate with Dead By Daylight's backend, though if you have any suggestions for new things to add to the API, I may add them for you.

## What purpose does it serve?
Currently I use it to retreive and display the shrine of secrets and output when the next one is.

## How does it work?
It logins in anonymously to the Dead By Daylight API, and retrieves information from endpoints from the api and returns them, additionally it does the same from Dead By Daylight's CDN. It also reads from the PAK file (which is what requires the steam user to download the files), to get information such as perks, offerings, characters, tunables, items, etc...

## Where is it hosted?
you can find it [here](https://dbd-stats.info)

## Endpoints
1.  /api/maps(?branch=live)
2.  /api/perks(?branch=live)
3.  /api/offerings(?branch=live)
4.  /api/characters(?branch=live)
5.  /api/tunables(?branch=live&killer=)
6.  /api/emblemtunables(?branch=live)
7.  /api/customizationitems(?branch=live)
8.  /api/gameconfigs(?branch=live)
9.  /api/ranksthresholds(?branch=live)
10. /api/itemaddons(?branch=live)
11. /api/items(?branch=live)
12. /api/stats/:steam_64: (Profile needs to be public)
13. /api/shrineofsecrets(?pretty=true&branch=live)
14. /api/storeoutfits(?branch=live)
15. /api/config(?branch=live)
16. /api/catalog(?branch=live)
17. /api/news(?branch=live)
18. /api/featured(?branch=live)
19. /api/schedule(?branch=live)
20. /api/bpevents(?branch=live)
21. /api/specialevents(?branch=live)
22. /api/archive(?branch=ptb&tome=Tome01)
23. /api/archiverewarddata(?branch=live)
