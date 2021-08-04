# Aphid Bot
![License](https://img.shields.io/badge/License-AGPLv3-blue.svg)

## Support Discord:

For support on using or setting up your own instance of Aphid Bot, feel free to join the discord!

[<img src="https://canary.discordapp.com/api/guilds/678767684669669386/widget.png?style=banner2">](https://discord.gg/qYJwsZjMku)

## About Aphid Bot:
<img src="https://i.imgur.com/5pgJgMH.png" height=128 width=90 />

Aphid bot is a Sysbot inspired .NET application for Pokemon Ultra Sun and Ultra Moon. It is almost entirely based off [Ledybot](https://github.com/olliz0r/Ledybot) and [Sysbot](https://github.com/kwsch/SysBot.NET)

If you plan on contributing to the development of the bot, I apologize profusely in advance for the spaghetti code. I tried my best to throughouly comment any changes I made, but this was my first ever C# project and it really shows. There are a few non-essential functions like Pk7idlecleanup() that do not work at all as intended. I would like to fix these down the line; but I've spent so much of the last month fixing extremely minor bugs and QoL changes that I feel like if I don't release now, I am never going to.

## Setting up Aphid Bot:

Video on how to setup Aphid bot: VIDEO LINK HERE.

Ensure you have BootNTR 3.6 and Inputredirection working on your 3DS. Aphid Bot is only tested and working with bot owners with English Ultra Sun/Ultra Moon games. Users of Aphid bot can use any Generation 7 game, from any language/region.

Ensure you have all the necessary versions of .NET required to run Ledybot and Sysbot. Download the most recent release of Aphid bot from the Release tab in Github. Extract all of the contents into a folder of your choice. Edit giveawaydetails.xml and replace all "C:\XYXYXYXY\dump" with the folder location of your dump folder, then save the file. Edit the Config.json and replace the "DistributeFolder" and "DumpFolder" locations with the locations of your Distribute and Dump folders, then replace "Token" under the Discord section with your Discord bot's Token.

Launch AphidbBot.exe, input your 3Ds IP address. Go to the settings tab and ensure "from the front" is checked off under Trade Direction. Tune other settings as necessary. On your 3DS, launch Ultra Moon/Ultra Sun and navigate to the GTS until you are on the "Seek Pokemon" Button. Connect to the 3DS in Aphid Bot, Start the Discord Bot, and start the actual bot.

Uses [Discord.Net](https://github.com/discord-net/Discord.Net) and [StreamingClientLibary](https://github.com/SaviorXTanren/StreamingClientLibrary) as a dependency via Nuget.

## Using Aphid Bot:

Video on using Aphid bot: VIDEO LINK HERE.

* %help - an example trade request
* %trade - trade command for a showdown set
* %tradefile - trade command for a .pk7 file
* %info - information about the bot
* %queuestatus or %qs - get your current position in the queue
* %queueclear or %qc - remove yourself from the queue
* %language - get help Eastern Asian Language Games.

If your game is in Japanese, Korean or Chinese you need to slighty change your ??d: value in your request for the bot to work with you.
* Korean: replace "??d:" with "예예??d:"
* Japanese: replace "??d:" with "ええ??d:"
* Chinese: replace "??d:" with "诺诺??d:"

## Other Dependencies
Pokémon API logic is provided by [PKHeX](https://github.com/kwsch/PKHeX/), and template generation is provided by [AutoMod](https://github.com/architdate/PKHeX-Plugins/).

# License
Refer to the `License.md` for details regarding licensing.
