using Discord;
using Discord.Commands;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord
{
    // src: https://github.com/foxbot/patek/blob/master/src/Patek/Modules/InfoModule.cs
    // ISC License (ISC)
    // Copyright 2017, Christopher F. <foxbot@protonmail.com>
    public class InfoModule : ModuleBase<SocketCommandContext>
    {
        private const string detail = "I am a Discord bot powered by PKHeX.Core, Sysbot, and Ledybot.";
        private const string actualrepo = "https://github.com/QualityQuestion/Aphid-Bot";
        private const string repo = "https://github.com/kwsch/SysBot.NET";
        private const string repo2 = "https://github.com/olliz0r/Ledybot";
        [Command("info")]
        [Alias("about", "whoami", "owner")]
        public async Task InfoAsync()
        {
            var app = await Context.Client.GetApplicationInfoAsync().ConfigureAwait(false);

            var builder = new EmbedBuilder
            {
                Color = new Color(114, 137, 218),
                Description = detail,
            };

            builder.AddField("Info",
                $"- [Aphid Bot Source code]({actualrepo})\n" +
                $"- [Sysbot Source code]({repo})\n" +
                $"- [Ledybot Source code]({repo2})\n" +
                $"- {Format.Bold("Owner")}: {app.Owner} ({app.Owner.Id})\n" +
                $"- {Format.Bold("Library")}: Discord.Net ({DiscordConfig.Version})\n" +
                $"- {Format.Bold("Uptime")}: {GetUptime()}\n" +
                $"- {Format.Bold("Runtime")}: {RuntimeInformation.FrameworkDescription} {RuntimeInformation.ProcessArchitecture} " +
                $"({RuntimeInformation.OSDescription} {RuntimeInformation.OSArchitecture})\n" +
                $"- {Format.Bold("Buildtime")}: {GetBuildTime()}\n" +
                $"- {Format.Bold("Core")}: {GetCoreDate()}\n" +
                $"- {Format.Bold("AutoLegality")}: {GetALMDate()}\n"
                );

            builder.AddField("Stats",
                $"- {Format.Bold("Heap Size")}: {GetHeapSize()}MiB\n" +
                $"- {Format.Bold("Guilds")}: {Context.Client.Guilds.Count}\n" +
                $"- {Format.Bold("Channels")}: {Context.Client.Guilds.Sum(g => g.Channels.Count)}\n" +
                $"- {Format.Bold("Users")}: {Context.Client.Guilds.Sum(g => g.Users.Count)}\n"
                );

            await ReplyAsync("Here's a bit about me!", embed: builder.Build()).ConfigureAwait(false);
        }
        [Command("commands")]
        [Alias("Commands")]
        public async Task PossibleCMDs()
        {
            var app = await Context.Client.GetApplicationInfoAsync().ConfigureAwait(false);

            var builder = new EmbedBuilder
            {
                Color = new Color(114, 137, 218),
                Description = detail,
            };

            builder.AddField("Commands",
                $"- {Format.Bold("%trade or %t")}: trade command for a showdown set or format\n" +
                $"- {Format.Bold("%tradefile or %tf")}: trade command for a .pk7 file\n" +
                $"- {Format.Bold("%queuestatus or %qs")}: get your current position in the queue\n" +
                $"- {Format.Bold("%queueclear or %qc")}: remove yourself from the queue\n" +
                $"- {Format.Bold("%info")}: Info for nerds\n" +
                $"- {Format.Bold("%help")}: get help with using the GTS bot\n" +
                $"- {Format.Bold("%language")}: get help Eastern Asian Language Games.\n"
                );

            await ReplyAsync("Here are the possible commands!", embed: builder.Build()).ConfigureAwait(false);
        }
        [Command("help")]
        [Alias("about", "whoami", "owner")]
        public async Task HelpAsync()
        {
            var msg0 = "Here is an example of how to request a Pokemon with the GTS bot! \n";
            var msg1 = "`%trade Pikachu (F)` \n`Ball: Ultra Ball` \n`IVs: 22 HP / 30 Atk / 30 Def / 21 SpA / 14 SpD / 11 Spe` \n`EVs: 40 HP / 40 Atk / 48 Def / 50 SpA / 80 SpD / 252 Spe` \n`Ability: Static` \n`Level: 99` \n`Shiny: Yes` \n`Hardy Nature` \n`- Thunder Shock` \n`- Charm` \n`??d: 89`\n";
            var msg2 = "This command tells the bot to look on the GTS for a Muk (Muk is pokedex number 89 i.e ??d: 89) and to Generate a Shiny, Level 99 Pikachu, caught in an Ultra Ball, with all of the requested IVs/Evs\n";
            var msg3 = "The bot will then message you a code, change the nickname of your Pokemon that you are going to deposit to the code.";
            var msg4 = msg0 + msg1 + msg2 + msg3;

            await ReplyAsync(msg4).ConfigureAwait(false);
        }

        [Command("language")]
        [Alias("language")]
        public async Task LanguageAsync()
        {
            var msg0 = "If your game is in Japanese, Korean or Chinese you need to slighty change your ??d: value in your request for the bot to work with you.\n";
            var msg1 = "**Korean:** replace *??d:* with *예예??d:*\n";
            var msg2 = "**Japanese:** replace *??d:* with *ええ??d:*\n";
            var msg3 = "**Chinese:** replace *??d:* with *诺诺??d:*";
            var msg4 = msg0 + msg1 + msg2 + msg3;

            await ReplyAsync(msg4).ConfigureAwait(false);
        }

        private static string GetUptime() => (DateTime.Now - Process.GetCurrentProcess().StartTime).ToString(@"dd\.hh\:mm\:ss");
        private static string GetHeapSize() => Math.Round(GC.GetTotalMemory(true) / (1024.0 * 1024.0), 2).ToString(CultureInfo.CurrentCulture);

        private static string GetBuildTime()
        {
            var assembly = Assembly.GetEntryAssembly();
            return File.GetLastWriteTime(assembly.Location).ToString(@"yy-MM-dd\.hh\:mm");
        }

        public static string GetCoreDate() => GetDateOfDll("PKHeX.Core.dll");
        public static string GetALMDate() => GetDateOfDll("PKHeX.Core.AutoMod.dll");

        private static string GetDateOfDll(string dll)
        {
            var folder = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var path = Path.Combine(folder, dll);
            var date = File.GetLastWriteTime(path);
            return date.ToString(@"yy-MM-dd\.hh\:mm");
        }
    }
}