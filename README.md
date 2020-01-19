# DBD-API
Dead By Daylight API

## What is this for?
It allows you to communicate with Dead By Daylight's backend, though if you have any suggestions for new things to add to the API, I may add them for you.

## What purpose does it serve?
Currently I use it to retreive and display the shrine of secrets and output when the next one is.

## How does it work?
It logins in anonymously to the Dead By Daylight API, and retrieves information from endpoints from the api and returns them, additionally it does the same from Dead By Daylight's CDN. It also reads from the PAK file (which is what requires the steam user to download the files), to get information such as perks, offerings, characters, tunables, items, etc...

## Where is it hosted?
you can find it [here](https://dbd.wolfer.io)

## Endpoints
1.  /api/perks(?branch=live)
2.  /api/offerings(?branch=live)
3.  /api/characters(?branch=live)
4.  /api/tunables(?branch=live&killer=)
5.  /api/customizationitems(?branch=live)
6.  /api/itemaddons(?branch=live)
7.  /api/items(?branch=live)
8.  /api/stats/:steam_64: (Profile needs to be public)
9.  /api/shrineofsecrets(?pretty=true&branch=live)
10. /api/storeoutfits(?branch=live)
11. /api/config(?branch=live)
12. /api/catalog(?branch=live)
13. /api/news(?branch=live)
14. /api/featured(?branch=live)
15. /api/schedule(?branch=live)
16. /api/bpevents(?branch=live)
17. /api/specialevents(?branch=live)
18. /api/archive(?branch=ptb&tome=Tome01)
19. /api/archiverewarddata(?branch=live)
