# Aphid Bot
![License](https://img.shields.io/badge/License-AGPLv3-blue.svg)

## Support Discord:

For support on using or setting up your own instance of Aphid Bot, feel free to join the discord!

[<img src="https://canary.discordapp.com/api/guilds/678767684669669386/widget.png?style=banner2">](https://discord.gg/qYJwsZjMku)

## About Aphid Bot:
<img src="https://i.imgur.com/5pgJgMH.png" height=128 width=90 />

Aphid bot is a Sysbot inspired .NET application for Pokemon Ultra Sun and Ultra Moon. It is almost entirely based off [Ledybot](https://github.com/olliz0r/Ledybot) and [Sysbot](https://github.com/kwsch/SysBot.NET)

## Setting up Aphid Bot:

Video on how to setup Aphid bot: VIDEO LINK HERE.

Ensure you have BootNTR 3.6 and Inputredirection working on your 3DS.

Ensure you have all the necessary version of .NET to run Ledybot and Sysbot. Download the most recent release of Aphid bot from the Release tab in Github. Extract all of the contents into a folder of your choice. Edit giveawaydetails.xml and replace all "C:\XYXYXYXY\dump" with the folder location of your dump folder, then save the file. Edit the Config.json and replace the "DistributeFolder" and "DumpFolder" locations with the locations of your Distribute and Dump folders, then replace "Token" under the Discord section with your Discord bot's Token.

Launch AphidbBot.exe, input your 3Ds IP address. Go to the settings tab and ensure "from the front" is checked off under Trade Direction. Tune other settings as necessary. On your 3DS, launch Ultra Moon/Ultra Sun and navigate to the GTS until you are on the "Seek Pokemon" Button. Connect to the 3DS in Aphid Bot, Start the Discord Bot, and start the actual bot.

Uses [Discord.Net](https://github.com/discord-net/Discord.Net) and [StreamingClientLibary](https://github.com/SaviorXTanren/StreamingClientLibrary) as a dependency via Nuget.

## Other Dependencies
Pokémon API logic is provided by [PKHeX](https://github.com/kwsch/PKHeX/), and template generation is provided by [AutoMod](https://github.com/architdate/PKHeX-Plugins/).

# License
Refer to the `License.md` for details regarding licensing.
