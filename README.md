# DBD-API
Dead By Daylight API

## Requirements
- An account that owns dead by daylight. (technically not true)

## What is this for?
It allows you to communicate with Dead By Daylight's backend, though if you have any suggestions for new things to add to the API, I may add them for you.

## What purpose does it serve?
Currently I use it to retreive and display the shrine of secrets and output when the next one is.

## How does it work?
It logins into their API using your steam session token (which requires you to actually have the game), then makes requests to their backend using said token and outputs the data their backend outputs, basically a proxy which allows you to get the information from the games backend without having a session token. Though it may include some fo the information for example the shrine may tell you different things if you have actually bought something from the shrine, or the store may output different values if you have bought something (which is why its best to use a new account)

## Can I get banned for running it?
Currently, i've run it for around ~7k hours and haven't been banned, but it may differ if you abuse the api. So the answer is that im unsure, but considering its against their EULA to reverse engineer web requests, i think the answer is you can get banned for using it.

## Where is it hosted?
you can find it [here](https://dbd.wolfer.io)

## Endpoints
1. /api/shrineofsecrets(?pretty=true)
2. /api/outfits
3. /api/config
4. /api/catalog
5. /api/news
6. /api/features
7. /api/schedule
8. /api/bpevents
9. /api/specialevents
