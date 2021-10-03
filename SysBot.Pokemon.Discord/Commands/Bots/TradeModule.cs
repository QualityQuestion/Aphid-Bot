using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using PKHeX.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord
{
    [Summary("Queues new Link Code trades")]
    public class TradeModule : ModuleBase<SocketCommandContext>
    {
        private static TradeQueueInfo<PK8> Info => SysCordInstance.Self.Hub.Queues.Info;

        [Command("tradeList")]
        [Alias("tl")]
        [Summary("Prints the users in the trade queues.")]
        [RequireSudo]
        public async Task GetTradeListAsync()
        {
            string msg = Info.GetTradeList(PokeRoutineType.LinkTrade);
            var embed = new EmbedBuilder();
            embed.AddField(x =>
            {
                x.Name = "Pending Trades";
                x.Value = msg;
                x.IsInline = false;
            });
            await ReplyAsync("These are the users who are currently waiting:", embed: embed.Build()).ConfigureAwait(false);
        }

        [Command("tradehalp")]
        [Alias("th")]
        [Summary("Makes the bot trade you the provided Pokémon file.")]
        [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
        public async Task TradeAsyncAttach([Summary("Trade Code")] int code, int langid)
        {
            await TradeAsyncAttach(code, langid, Context.User).ConfigureAwait(false);

        }

        [Command("trade")]
        [Alias("t")]
        [Summary("Makes the bot trade you a Pokémon converted from the provided Showdown Set.")]
        [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
        public async Task TradeAsync([Summary("Trade Code")] int pseudorand, [Summary("Showdown Set")][Remainder] string content)
        {
            for (int i = 0; i <= discorduser.Capacity; i++)
            {
                try
                {
                    if (Context.User == discorduser[i])
                    {
                        var mesg = $"Sorry, you are already in the GTS queue.";
                        await ReplyAsync(mesg).ConfigureAwait(false);
                        return;
                    }
                }
                catch
                {
                    int xyz = 0; //just continue
                }
            }

            const int gen = 7;
            int psrl1; //pseudorandom letter 1
            int psrl2;
            int psrl3;
            int depositasint = 10;
            using (RNGCryptoServiceProvider rg = new RNGCryptoServiceProvider())
            {
                byte[] rno = new byte[5];
                byte[] rnp = new byte[4];
                rg.GetBytes(rno);
                pseudorand = Math.Abs(BitConverter.ToInt32(rno, 0) % 99999); //Gen7 limits you to 5 numbers in a nickname
                psrl1 = Math.Abs(BitConverter.ToInt32(rno, 0) % 26); //pseudorandom letter
                psrl2 = pseudorand % 26;
                rg.GetBytes(rnp);
                psrl3 = Math.Abs(BitConverter.ToInt32(rnp, 0) % 26);
                if (pseudorand < 10000)
                {
                    pseudorand = pseudorand + 10000; //force 5 digits
                }

            }

            string japaneseID = "ええ";
            string koreanID = "예예";
            string chineseID = "诺诺";
            bool isKorean = content.Contains(koreanID);
            bool isJapanese = content.Contains(japaneseID);
            bool isChinese = content.Contains(chineseID);

            if (isJapanese == true)
            {
                int finder = content.IndexOf("ええ");
                content = content.Remove(finder, 2);
            }
            else if (isKorean == true)
            {
                int finder = content.IndexOf("예예");
                content = content.Remove(finder, 2);
            }
            else if (isChinese == true)
            {
                int finder = content.IndexOf("诺诺");
                content = content.Remove(finder, 2);
            }
            int formater = content.IndexOf("??d: ");
            string depositnum = content.Substring(formater + 4);

            try
            {
                depositasint = Int32.Parse(depositnum);
            }
            catch
            {
                var msg = $"Unable to parse ??d: value, try using the help command for an example on what the ??d: value should look like.";
                await ReplyAsync(msg).ConfigureAwait(false);
                return;
            }
            int[] mythicals = { 151, 251, 385, 386, 494, 493, 492, 491, 490, 649, 648, 647, 721, 720, 719, 805, 806, 807 };
            int[] evolveByTrade = { 525, 366, 356, 125, 349, 75, 533, 93, 64, 588, 67, 126, 95, 708, 61, 137, 233, 710, 112, 123, 117, 616, 79, 682, 684 };
            if (mythicals.Contains(depositasint))
            {
                var msg = "Oops! Pokemon you are trying to deposit is mythical and cannot be deposited!";
                await ReplyAsync(msg).ConfigureAwait(false);
                return;
            }
            else if (evolveByTrade.Contains(depositasint))
            {
                var msg = "Oops! Pokemon you are trying to deposit is banned from being traded to me, as it can evolve when traded!";
                await ReplyAsync(msg).ConfigureAwait(false);
                return;
            }
            content = content.Substring(0, formater);
            content = ReusableActions.StripCodeBlock(content);
            var set = new ShowdownSet(content);
            var template = AutoLegalityWrapper.GetTemplate(set);

            if (set.InvalidLines.Count != 0)
            {
                var msg = $"Unable to parse Showdown Set:\n{string.Join("\n", set.InvalidLines)}";
                await ReplyAsync(msg).ConfigureAwait(false);
                return;
            }
            var savtempcheck = AutoLegalityWrapper.GetTrainerInfo(8);
            var pkmgen8check = savtempcheck.GetLegal(template, out var resulttemp);
            pkmgen8check = PKMConverter.ConvertToType(pkmgen8check, typeof(PK8), out _) ?? pkmgen8check;
            if (pkmgen8check.Species > 807) //checks to see if requested Pokemon is Gen 8 only
            {
                var msg1 = $"Oops! Requested Pokemon outside the range of the Generation 7 Pokedex";
                await ReplyAsync(msg1).ConfigureAwait(false);
                return;
            }
            try
            {
                var sav = AutoLegalityWrapper.GetTrainerInfo(gen);
                var pkm = sav.GetLegal(template, out var result);
                var la = new LegalityAnalysis(pkm);
                var spec = GameInfo.Strings.Species[template.Species];
                pkm = PKMConverter.ConvertToType(pkm, typeof(PK7), out _) ?? pkm;

                if (pkm is not PK7 || !la.Valid)
                {

                    var imsg = $"Oops! I wasn't able to create a legal Pokemon from that. This is usually due to either a formatting error in your request, or an illegal move, illegal EV spread, illegal shininess etc...";
                    await ReplyAsync(imsg).ConfigureAwait(false);
                    return;
                }
                else if (pkm.WasEvent == true) //event Pokemon cannot be traded through the GTS
                {
                    var msg1 = $"Oops! Requested Pokemon is an event and cannot be traded through the GTS";
                    await ReplyAsync(msg1).ConfigureAwait(false);
                    return;
                }
                else if (pkm.Species == 800 && (pkm.Move1 == 722 || pkm.Move2 == 722 || pkm.Move3 == 722 || pkm.Move4 == 722))
                {
                    var ncp = $"Oops! Necrozma cannot be traded on the GTS if it knows the move Photon Geyser";
                    await ReplyAsync(ncp).ConfigureAwait(false);
                    return;
                }
                string SpeciesName = pkm.FileName.Substring(0, 3); //retrieves Pokedex number of request
                string pwd = Directory.GetCurrentDirectory();
                bool exist = System.IO.Directory.Exists(pwd + "\\dump\\" + SpeciesName + "\\");
                if (!exist)
                {
                    System.IO.Directory.CreateDirectory(pwd + "\\dump\\" + SpeciesName + "\\");
                }
                pwd = pwd + "\\dump\\" + SpeciesName + "\\"; //store requested pokemon in \dump\[pokedex number]\[filename].pk7

                pkm.ResetPartyStats();

                if (pkm.HeldItem > 655 && pkm.HeldItem < 795) //0 out item if it is holding a mega/z stone
                {
                    pkm.HeldItem = 0;
                }
                else if (pkm.HeldItem > 796 && pkm.HeldItem < 837)
                {
                    pkm.HeldItem = 0;
                }
                else if (pkm.HeldItem > 920 && pkm.HeldItem < 960)
                {
                    pkm.HeldItem = 0;
                }
                else if (pkm.HeldItem == 534 || pkm.HeldItem == 535) //0 out blue/red orb
                {
                    pkm.HeldItem = 0;
                }
                var alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                var korean = "용의해지썬문새해복많이지우개굴닌자쏘드라마나피오키스"; //26
                var japanese = "トウキョーウチダアツトピカチュウデオキスマナフィゴス"; //26
                var chinese = "费洛美螂甲贺忍蛙墨海马臭泥瑪納霏梦幻代欧奇希斯皮卡丘"; //26
                var randlterrs = "";
                if (isJapanese == true)
                {
                    randlterrs = japanese.Substring(psrl1, 1); //if Japanese
                }
                else if (isKorean == true)
                {
                    randlterrs = korean.Substring(psrl1, 1); //if Korean
                }
                else if (isChinese == true)
                {
                    randlterrs = chinese.Substring(psrl1, 1); //if Chinese
                }
                else
                {
                    randlterrs = alphabet.Substring(psrl1, 1) + alphabet.Substring(psrl2, 1) + alphabet.Substring(psrl3, 1); //random 3 letter string
                }

                if (PKHeX.Core.WordFilter.IsFiltered(randlterrs, out string regMatch)) //check to see if random 3 letter string is on banned word list
                {
                    randlterrs = "GGG"; //if it's on the banned word list, replace with GGG
                }

                var temp = randlterrs + pseudorand.ToString();
                var pkoutput = @pwd + temp + ".pk7";
                File.WriteAllBytes(pkoutput, pkm.DecryptedPartyData); //write .pk7 to /dump/[pkm.species]/[temp].pk7
                /*var sig = Context.User.GetFavor();
                string SpeciesName = pkm.FileName;
                int formater = SpeciesName.IndexOf(" - ");
                int formater1 = SpeciesName.IndexOf(" - ", formater + 1);
                int formater2 = SpeciesName.IndexOf(".pk7");
                SpeciesName = SpeciesName.Remove(formater1 + 1, formater2 - formater1);
                SpeciesName = SpeciesName.Substring(5);
                formater = SpeciesName.IndexOf("pk7");
                SpeciesName = SpeciesName.Remove(formater, 3); //there has to be an easier way to do this shit but I'm fkn stupid */
                string ledybotspecies = Ledybotsearch(depositasint);
                var pkmnametest = "" + (Species)pkm.Species; //the easier way smfh
                var msg2 = $"Added to the GTS Trade Queue, deposit a " + ledybotspecies + " nicknamed:** " + temp + " **and request a** " + pkmnametest + " ** \nMake sure to set the Gender and Level for the requested Pokemon are both set to **ANY**";
                var msg3 = $"" + Context.Message.Author.Mention + " Added to the GTS Trade Queue. Receiving: " + pkmnametest;
                try
                {
                    await Context.User.SendMessageAsync(msg2).ConfigureAwait(false);
                    await ReplyAsync(msg3).ConfigureAwait(false);
                    await Context.Message.DeleteAsync(RequestOptions.Default).ConfigureAwait(false);
                }
                catch (HttpException ex)
                {
                    await Context.Channel.SendMessageAsync($"{ex.HttpCode}: {ex.Reason}!").ConfigureAwait(false);
                    var noAccessMsg = "You must enable private messages in order to be queued!";
                    await Context.Channel.SendMessageAsync(noAccessMsg).ConfigureAwait(false);
                    File.Delete(pkoutput);
                    return;
                }
                nicknamelist.Add(temp); //add temp to list of nicknames
                discorduser.Add(Context.User); //add discord requester to list of user at the same index their nickname was added in the nickname list
                deposited.Add(depositasint); //add deposited pokemon to list of deposited pokemon " "

            }
            catch
            {
                var msg = $"Oops! An unexpected problem happened with this Showdown Set:\n```{string.Join("\n", set.GetSetLines())}```";

                await ReplyAsync(msg).ConfigureAwait(false);
            }
        }

        [Command("trade")]
        [Alias("t")]
        [Summary("Makes the bot trade you a Pokémon converted from the provided Showdown Set.")]
        [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
        public async Task TradeAsync([Summary("Showdown Set")][Remainder] string content)
        {
            var code = Info.GetRandomTradeCode();
            await TradeAsync(code, content).ConfigureAwait(false);
        }

        [Command("tradefile")]
        [Alias("tf")]
        [Summary("Makes the bot trade you the attached file.")]
        [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
        public async Task TradeAsyncAttach([Summary("find this")][Remainder] string content)
        {
            int bless = 0;
            content = Context.Message.Content;
            string createText = "" + content + Environment.NewLine;
            string japaneseID = "ええ";
            string koreanID = "예예";
            string chineseID = "诺诺";
            bool isKorean = content.Contains(koreanID);
            bool isJapanese = content.Contains(japaneseID);
            bool isChinese = content.Contains(chineseID);
            int langid = 0;
            if (isJapanese == true)
            {
                int finder = content.IndexOf("ええ");
                content = content.Remove(finder, 2);
                langid = 1;
            }
            else if (isKorean == true)
            {
                int finder = content.IndexOf("예예");
                content = content.Remove(finder, 2);
                langid = 2;
            }
            else if (isChinese == true)
            {
                int finder = content.IndexOf("诺诺");
                content = content.Remove(finder, 2);
                langid = 3;
            }
            int formater = content.IndexOf("??d: ");
            string depositnum = content.Substring(formater + 4);
            try
            {
                bless = Int32.Parse(depositnum);
            }
            catch
            {
                var msg = $"Unable to parse ??d: value, try using the help command for an example on what the ??d: value should look like.";
                await ReplyAsync(msg).ConfigureAwait(false);
                return;
            }
            int[] mythicals = { 151, 251, 385, 386, 494, 493, 492, 491, 490, 649, 648, 647, 721, 720, 719, 805, 806, 807 };
            int[] evolveByTrade = { 525, 366, 356, 125, 349, 75, 533, 93, 64, 588, 67, 126, 95, 708, 61, 137, 233, 710, 112, 123, 117, 616, 79, 682, 684 };
            if (mythicals.Contains(bless))
            {
                var msg = "Oops! The Pokemon you are trying to deposit is mythical and cannot be deposited!";
                await ReplyAsync(msg).ConfigureAwait(false);
                return;
            }
            else if (evolveByTrade.Contains(bless))
            {
                var msg = "Oops! The Pokemon you are trying to deposit is banned from being traded to me, as it can evolve when traded!";
                await ReplyAsync(msg).ConfigureAwait(false);
                return;
            }
            await TradeAsyncAttach(bless, langid).ConfigureAwait(false);
        }

        [Command("queuestatus")]
        [Alias("qs")]
        [Summary("Checks Position in the Queue")]
        public async Task QueueStatus()
        {
            SocketUser user = Context.User;
            for (int i = 0; i <= discorduser.Capacity; i++)
            {
                try
                {
                    if (user == discorduser[i])
                    {
                        int readableposition = i + 1;
                        var msg = $"You are currently position **" + readableposition + "** in the GTS Trade Queue.";
                        await ReplyAsync(msg).ConfigureAwait(false);
                        return;
                    }
                    else if (i == discorduser.Capacity && user != discorduser[i])
                    {
                        var msg2 = $"You are not currently in the GTS Trade Queue.";
                        await ReplyAsync(msg2).ConfigureAwait(false);
                        return;
                    }
                }
                catch
                {
                    var msg3 = $"You are not currently in the Queue";
                    await ReplyAsync(msg3).ConfigureAwait(false);
                    return;
                }
            }
        }

        [Command("queueclear")]
        [Alias("qc")]
        [Summary("Clears Position from Queue")]
        public async Task QueueClear()
        {
            SocketUser user = Context.User;
            for (int i = 0; i <= discorduser.Capacity; i++)
            {
                try
                {
                    if (user == discorduser[i] && i != 0)
                    {
                        nicknamelist.RemoveAt(i);
                        discorduser.RemoveAt(i);
                        deposited.RemoveAt(i);
                        var msg = $"Removed from the Queue!";
                        await ReplyAsync(msg).ConfigureAwait(false);
                        return;
                    }
                    else if (user == discorduser[i] && i == 0)
                    {
                        var msg1 = $"I am currently searching for your trade, but I will try and remove you from the queue shortly!";
                        qcOnIndexZero = true;
                        await ReplyAsync(msg1).ConfigureAwait(false);
                        return;
                    }
                    else if (i == discorduser.Capacity && user != discorduser[i])
                    {
                        var msg2 = $"You are not currently in the Queue";
                        await ReplyAsync(msg2).ConfigureAwait(false);
                        return;
                    }
                }
                catch
                {
                    var msg3 = $"You are not currently in the Queue";
                    await ReplyAsync(msg3).ConfigureAwait(false);
                    return;
                }
            }
        }
        private async Task TradeAsyncAttach(int ddq, int langid, SocketUser usr)
        {
            for (int i = 0; i <= discorduser.Capacity; i++)
            {
                try
                {
                    if (usr == discorduser[i])
                    {
                        var mesg = $"Sorry, you are already in the GTS queue.";
                        await ReplyAsync(mesg).ConfigureAwait(false);
                        return;
                    }
                }
                catch
                {
                    int xyz = 0; //just continue
                }
            }
            var attachment = Context.Message.Attachments.FirstOrDefault();
            if (attachment == default)
            {
                await ReplyAsync("No attachment provided!").ConfigureAwait(false);
                return;
            }

            var att = await NetUtil.DownloadPKMAsync(attachment).ConfigureAwait(false);
            var pkm = GetRequest(att);
            pkm = (PK7?)(PKMConverter.ConvertToType(pkm, typeof(PK7), out _) ?? pkm);

            if (pkm == null)
            {
                await ReplyAsync("Attachment provided is not compatible with this module!").ConfigureAwait(false);
                return;
            }

            var la = new LegalityAnalysis(pkm);

            if (pkm is not PK7 || !la.Valid)
            {
                var msg = $"Oops! Attached PK7 is illegal and cannot be traded!";
                await ReplyAsync(msg).ConfigureAwait(false);
                return;
            }

            int pseudorand = 0;
            int psrl1;
            int psrl2;
            int psrl3;
            using (RNGCryptoServiceProvider rg = new RNGCryptoServiceProvider())
            {
                byte[] rno = new byte[5];
                rg.GetBytes(rno);
                pseudorand = Math.Abs(BitConverter.ToInt32(rno, 0) % 99999);
                psrl1 = Math.Abs(BitConverter.ToInt32(rno, 0) % 26);
                psrl2 = pseudorand % 26;
                psrl3 = (pseudorand) % (Math.Abs(psrl1 - psrl2));
                if (pseudorand < 10000)
                {
                    pseudorand = pseudorand + 10000; //force 5digits
                }
            }

            if (pkm.WasEvent == true)
            {
                var msg1 = $"Oops! Requested Pokemon is an event and cannot be traded through the GTS";
                await ReplyAsync(msg1).ConfigureAwait(false);
                return;
            }
            else if (pkm.Species == 800 && (pkm.Move1 == 722 || pkm.Move2 == 722 || pkm.Move3 == 722 || pkm.Move4 == 722))
            {
                var ncp = $"Oops! Necrozma cannot be traded on the GTS if it knows the move Photon Geyser";
                await ReplyAsync(ncp).ConfigureAwait(false);
                return;
            }
            pkm.ResetPartyStats();

            if (pkm.HeldItem > 655 && pkm.HeldItem < 795)
            {
                pkm.HeldItem = 0;
            }
            else if (pkm.HeldItem > 796 && pkm.HeldItem < 837)
            {
                pkm.HeldItem = 0;
            }
            else if (pkm.HeldItem > 920 && pkm.HeldItem < 960)
            {
                pkm.HeldItem = 0;
            }
            else if (pkm.HeldItem == 534 || pkm.HeldItem == 535)
            {
                pkm.HeldItem = 0;
            }

            string SpeciesName = pkm.FileName.Substring(0, 3);
            string pwd = Directory.GetCurrentDirectory();
            bool exist = System.IO.Directory.Exists(pwd + "\\dump\\" + SpeciesName + "\\");
            if (!exist)
            {
                System.IO.Directory.CreateDirectory(pwd + "\\dump\\" + SpeciesName + "\\");
            }
            pwd = pwd + "\\dump\\" + SpeciesName + "\\";
            var alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var korean = "용의해지썬문새해복많이지우개굴닌자쏘드라마나피오키스"; //26
            var japanese = "トウキョーウチダアツトピカチュウデオキスマナフィゴス"; //26
            var chinese = "费洛美螂甲贺忍蛙墨海马臭泥瑪納霏梦幻代欧奇希斯皮卡丘"; //26
            var randlterrs = "";
            if (langid == 1)
            {
                randlterrs = japanese.Substring(psrl1, 1); //if Japanese
            }
            else if (langid == 2)
            {
                randlterrs = korean.Substring(psrl1, 1); //if Korean
            }
            else if (langid == 3)
            {
                randlterrs = chinese.Substring(psrl1, 1); //if Chinese
            }
            else
            {
                randlterrs = alphabet.Substring(psrl1, 1) + alphabet.Substring(psrl2, 1) + alphabet.Substring(psrl3, 1); //random 3 letter string
            }
            if (PKHeX.Core.WordFilter.IsFiltered(randlterrs, out string regMatch)) //check to see if random 3 letter string is on banned word list
            {
                randlterrs = "GGG";
            }
            var temp = randlterrs + pseudorand.ToString();
            var pkoutput = @pwd + temp + ".pk7"; //new
            File.WriteAllBytes(pkoutput, pkm.DecryptedPartyData); //new
            var pkmnametest = "";
            string ledybotspecies = Ledybotsearch(ddq);
            pkmnametest = "" + (Species)pkm.Species; //the easier way smfh
            var msg3 = $"" + Context.Message.Author.Mention + " Added to the GTS Trade Queue. Receiving: " + pkmnametest;

            try
            {
                var msg2 = $"Added to the GTS Trade Queue, deposit a " + ledybotspecies + " nicknamed:** " + temp + " **and request a** " + pkmnametest + " ** \nMake sure to set the Gender and Level for the requested Pokemon are both set to **ANY**";
                await Context.User.SendMessageAsync(msg2).ConfigureAwait(false);
                await ReplyAsync(msg3).ConfigureAwait(false);
                await Context.Message.DeleteAsync(RequestOptions.Default).ConfigureAwait(false);
            }
            catch (HttpException ex)
            {
                await Context.Channel.SendMessageAsync($"{ex.HttpCode}: {ex.Reason}!").ConfigureAwait(false);
                var noAccessMsg = "You must enable private messages in order to be queued!";
                await Context.Channel.SendMessageAsync(noAccessMsg).ConfigureAwait(false);
                File.Delete(pkoutput);
                return;
            }

            nicknamelist.Add(temp);
            discorduser.Add(Context.User);
            deposited.Add(ddq);
            //await AddTradeToQueueAsync(pseudorand, usr.Username, pkm, usr).ConfigureAwait(false);
        }

        private static PK7? GetRequest(Download<PKM> dl)
        {
            if (!dl.Success)
                return null;
            return dl.Data switch
            {
                null => null,
                PK7 pk7 => pk7,
                _ => PKMConverter.ConvertToType(dl.Data, typeof(PK7), out _) as PK7
            };
        }

        public static List<string> nicknamelist = new List<string>();
        public static List<SocketUser> discorduser = new List<SocketUser>();
        public static List<int> deposited = new List<int>();
        private static List<string> filepaths = new List<string>();
        private static List<bool> hasbeentraded = new List<bool>();
        private static int counter = 0;
        public static bool qcOnIndexZero = false;
        public static async Task Discorduserfinder(string pseudorand)
        {

            for (int i = 0; i <= nicknamelist.Capacity; i++)
            {
                try
                {
                    if (pseudorand == nicknamelist[i])
                    {
                        var msg = $"Your Deposited Pokemon has been found! Trade will begin shortly!";
                        await discorduser[i].SendMessageAsync(msg).ConfigureAwait(false);
                        return;
                    }
                    else if (i == nicknamelist.Capacity && pseudorand != nicknamelist[i])
                    {
                        int j = 0;
                        return;
                        //not sure what to do here for exception handling
                    }
                }
                catch
                {
                    return;
                }
            }
        }
        public static async Task Discordpokesender(string pseudorand, string path)
        {

            for (int i = 0; i <= nicknamelist.Capacity; i++)
            {
                try
                {
                    if (pseudorand == nicknamelist[i])
                    {
                        var msg = $"Here is the .pk7 you deposited!";

                        byte[] holdz = File.ReadAllBytes(path);
                        PK7 pekay = new PK7(holdz);
                        pekay.ClearNickname();
                        pekay.IsNicknamed = false;
                        File.WriteAllBytes(path, pekay.DecryptedBoxData);
                        await discorduser[i].SendMessageAsync(msg).ConfigureAwait(false);
                        await discorduser[i].SendFileAsync(path).ConfigureAwait(false); //send back .pk7 they deposited
                        File.Delete(path); //delete it from local storage
                        nicknamelist.RemoveAt(i);
                        discorduser.RemoveAt(i); //pop from queue
                        deposited.RemoveAt(i);
                        return;
                    }
                    else if (i == nicknamelist.Capacity && pseudorand != nicknamelist[i])
                    {
                        int j = 0;
                        return;
                        //not sure what to do here for exception handling
                    }
                }
                catch
                {
                    return;
                }
            }
        }
        //public static string ledybotspecies = "";
        private static string Ledybotsearch(int ledybotdex)
        {
            string ledybotspeciez;
            var temp = new PK7 { Species = ledybotdex };
            //ledybotspecies = "" + (Species)temp.Species;
            ledybotspeciez = "" + (Species)temp.Species;
            return (ledybotspeciez);
        }

        private static string NatureDetermine(string first, string second, string last, int epoch)
        {
            string nature = "";
            if (first == " Spe" && second != " SpA")
            {
                nature = "Jolly";
            }
            else if (first == " Spe" && second != " Atk")
            {
                nature = "Timid";
            }
            else if ((first == " Atk" || second == " Atk") && (first != " SpA" && second != " SpA"))
            {
                nature = "Adamant";
            }
            else if ((first == " SpA" || second == " SpA") && (first != " Atk" && second != " Atk"))
            {
                nature = "Modest";
            }
            else if ((first == " Atk" || second == " Atk") && last == " Spe")
            {
                nature = "Brave";
            }
            else if ((first == " SpA" || second == " SpA") && last == " Spe")
            {
                nature = "Quiet";
            }
            else if ((first == " SpA" || second == " SpA") && ((first == " Atk" || second == " Atk")))
            {
                if(epoch%4 == 1)
                {
                    nature = "Rash";
                }
                else if(epoch%4 == 2)
                {
                    nature = "Naughty";
                }
                else if (epoch % 4 == 3)
                {
                    nature = "Lonely";
                }
                else
                {
                    nature = "Mild";
                }
            }
            else if (first == " Def" || second == " Def")
            {
                nature = "Relaxed";
            }
            else if (first == " SpD" || second == " SpD")
            {
                nature = "Sassy";
            }
            else
            {
                nature = "Bashful";
            }    


            return nature;
        }

        public static void LedybotIdleTask(int ledybotdex, int level, int gender, string nickname, bool shininess)
        {
            bool discordq = Determineifdiscord(nickname);
            if (discordq == true)
            {
                return; //don't generate a pokemon for someone else running a discord bot
            }

            /*int[] genderless = { 81, 82, 100, 101, 120, 121, 137, 233, 292, 337, 338, 343, 344, 374, 375, 376, 436, 437, 462, 474, 479, 489, 490, 599, 600, 601, 615, 622, 623, 703, 774, 781, 854, 855, 870, 132, 144, 145, 146, 150, 151, 201, 243, 244, 245, 249, 250, 251, 377, 378, 379, 382, 383, 384, 385, 386, 480, 481, 482, 483, 484, 486, 487, 491, 492, 493, 494, 638, 639, 640, 643, 644, 646, 647, 648, 649, 716, 717, 718, 719, 720, 721, 772, 773, 785, 786, 787, 788, 789, 790, 791, 792, 793, 794, 795, 796, 797, 798, 799, 800, 801, 802, 803, 804, 805, 806, 807 };
            if (genderless.Contains(ledybotdex) && (gender == 1 || gender == 2))
            {
                return; //prevents against corrupted GTS uploads asking for impossible pokemon
            }*/
            string shinyYes = "";
            int gen = 7;
            var tempa = new PK7 { Species = ledybotdex };
            string speciesa = "" + (Species)tempa.Species;
            var epoch = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
            var specieshold = GameData.GetPersonal(GameVersion.USUM)[ledybotdex];
            int atk = specieshold.ATK;
            int def = specieshold.DEF;
            int spd = specieshold.SPD;
            int spa = specieshold.SPA;
            int spe = specieshold.SPE;
            int hp = specieshold.HP;
            int total = atk + def + spd + spa + spe + hp;
            int[] stats = { hp, atk, def, spa, spd, spe };
            string[] statarraystring = { " HP", " Atk", " Def", " SpA", " SpD", " Spe" };
            Array.Sort(stats, statarraystring);
            string invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            foreach (char c in invalid)
            {
                nickname = nickname.Replace(c.ToString(), "a"); // protects against invalid file/path strings breaking the bot
            }
            level = level * 10;
            if (level == 0) //if they deposited requesting "ANY"
            {
                level = 90; //set level to 90, this leaves room for evolutions and avoids event Pokemon generation in some scenarios
            }
            string naturehold = NatureDetermine(statarraystring[5],statarraystring[4],statarraystring[0],epoch);
            string nature = "\n" + naturehold + " Nature";
            string evs = "";
            if (level >= 80 && total >= 395)
            {
                evs = "\n EVs: 252" + statarraystring[5] + " / 252" + statarraystring[4] + " / 4" + statarraystring[3];
            }
            if (((statarraystring[5] == " Atk" && statarraystring[4] == " SpA") && (naturehold != "Brave" || naturehold != "Quiet")))
            {
                    evs = "\n EVs: 252" + statarraystring[5] + " / 252 Spe / 4 HP";
            }
            else if ((statarraystring[5] == " SpA" && statarraystring[4] == " Atk") && (naturehold != "Brave" || naturehold != "Quiet"))
            {
                evs = "\n EVs: 252" + statarraystring[5] + " / 252 Spe / 4 HP";
            }
            else if ((statarraystring[5] == " SpD" && statarraystring[4] == " Spe") || (statarraystring[5] == " Spe" && statarraystring[4] == " SpD"))
            {
                if (epoch % 2 == 1)
                {
                    evs = "\n EVs: 252 HP / 252 SpD / 4 Def";
                }
                else
                {
                    evs = "\n EVs: 128 HP / 128 Def / 252 SpD";
                }
            }
            else if ((statarraystring[5] == " Def" && statarraystring[4] == " Spe") || (statarraystring[5] == " Spe" && statarraystring[4] == " Def"))
            {
                if (epoch % 2 == 1)
                {
                    evs = "\n EVs: 252 HP / 252 Def / 4 SpD";
                }
                else
                {
                    evs = "\n EVs: 128 HP / 128 SpD / 252 Def";
                }
            }
            if (level < 80 || total <= 395)
            {
                evs = "";
            }
            string ivsForSlow = "";
            if (statarraystring[0] == " Spe")
            {
                ivsForSlow = "\nIVS: 0 Spe";
            }
            var temp = new PK7 { Species = ledybotdex };
            string speciesstring = "" + (Species)temp.Species;
            string gendersymbol = "";

            if (gender == 1)
            {
                gendersymbol = " (M)";
            }
            else if (gender == 2)
            {
                gendersymbol = " (F)";
            }
            if (shininess == true) //if "Idle Pokemon are Shiny" is checked off in GUI
            {
                shinyYes = "\nShiny: Yes";
            }
            if (ledybotdex == 29) //Pokemon with special characters like spaces or dashes in their name need to be specified as in these if statements
            {
                speciesstring = "Nidoran-F";
            }
            else if (ledybotdex == 32)
            {
                speciesstring = "Nidoran-M";
            }
            else if (ledybotdex == 83)
            {
                speciesstring = "Farfetch'd";
            }
            else if (ledybotdex == 250 || ledybotdex == 249)
            {
                gen = 4; //forcing a HG/SS generation avoids generating an event pokemon
                if (ledybotdex == 250)
                {
                    speciesstring = "Ho-oh";
                }
            }
            else if (ledybotdex == 474)
            {
                speciesstring = "Porygon-Z";
            }
            else if (ledybotdex == 772)
            {
                speciesstring = "Type: Null";
            }
            else if (ledybotdex >= 782 && ledybotdex <= 784)
            {
                if (ledybotdex == 782)
                {
                    speciesstring = "Jangmo-o";
                }
                else if (ledybotdex == 783)
                {
                    speciesstring = "Hakamo-o";
                }
                else if (ledybotdex == 784)
                {
                    speciesstring = "Kommo-o";
                }
            }
            else if (ledybotdex >= 785 && ledybotdex <= 792)
            {
                shinyYes = ""; //these Pokemon cannot be shiny and traded on the GTS
                if (ledybotdex == 785)
                {
                    speciesstring = "Tapu Koko";
                }
                else if (ledybotdex == 786)
                {
                    speciesstring = "Tapu Lele";
                }
                else if (ledybotdex == 787)
                {
                    speciesstring = "Tapu Bulu";
                }
                else if (ledybotdex == 788)
                {
                    speciesstring = "Tapu Fini";
                }
            }
            else if (ledybotdex >= 800 && ledybotdex <= 802)
            {
                shinyYes = ""; //these Pokemon cannot be shiny and traded on the GTS
            }
            else if (ledybotdex == 807 || ledybotdex == 494 || ledybotdex == 647 || ledybotdex == 648 || ledybotdex == 718)
            {
                shinyYes = ""; //these Pokemon cannot be shiny and traded on the GTS
                if (ledybotdex == 718)
                {
                    gen = 6; //prevent generating an event pokemon
                    if (level == 100)
                    {
                        level = 95; //prevent generating an event pokemon
                    }
                }
            }
            else if (ledybotdex == 382 || ledybotdex == 383)
            {
                gen = 3; //prevent generating an event pokemon
            }
            else if (ledybotdex == 773 && level == 100)
            {
                level = 95; //prevent generating an event pokemon
            }
            try
            {
                string content = "" + speciesstring + gendersymbol + ivsForSlow + evs + "\nLevel: " + level + shinyYes + nature;
                var set = new ShowdownSet(content);
                var bleh = AutoLegalityWrapper.GetTemplate(set);
                var sav = AutoLegalityWrapper.GetTrainerInfo(gen);
                var pkm = sav.GetLegal(bleh, out var result);
                var la = new LegalityAnalysis(pkm);
                pkm = PKMConverter.ConvertToType(pkm, typeof(PK7), out _) ?? pkm;
                pkm.CurrentLevel = level; //prevent some sets from being generated at the wrong level
                if (pkm.Species != 800)
                {
                    pkm.Moves = MoveListSuggest.GetSuggestedCurrentMoves(la, MoveSourceType.All);
                }
                else if (pkm.Species == 800)
                {
                    pkm.Moves = MoveListSuggest.GetSuggestedCurrentMoves(la, MoveSourceType.AllMachines); //prevent generating a Necrozma with Photon Geyser
                }
                pkm.SetSuggestedMovePP(0);
                pkm.SetSuggestedMovePP(1); //needed to ensure legal PP value
                pkm.SetSuggestedMovePP(2);
                pkm.SetSuggestedMovePP(3);
                epoch = ((int)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMinutes) % 2;
                if (epoch == 1)
                {
                    pkm.HeldItem = 796; //50% chance for idle Pokemon to hold a gold bottle cap
                }
                else
                {
                    pkm.HeldItem = 1; //50% chance foridle Pokemon to hold a masterball
                }
                if (gen == 7)
                {
                    BallApplicator.ApplyBallLegalRandom(pkm); //give it a random legal ball
                }

                if (pkm is not PK7 || !la.Valid)
                {
                    return;
                }
                else if (pkm.WasEvent == true) //event Pokemon cannot be traded through the GTS
                {
                    return;
                }
                if (gender != 0 && ((pkm.Gender + 1) % 3 != gender)) //prevents against invalid GTS uploads asking for impossible pokemon like female Latios or male Ditto
                {
                    return;
                }
                string SpeciesName = pkm.FileName.Substring(0, 3); //retrieves Pokedex number of request
                string pwd = Directory.GetCurrentDirectory();
                bool exist = System.IO.Directory.Exists(pwd + "\\dump\\" + SpeciesName + "\\");
                if (!exist)
                {
                    System.IO.Directory.CreateDirectory(pwd + "\\dump\\" + SpeciesName + "\\");
                }
                pwd = pwd + "\\dump\\" + SpeciesName + "\\"; //store requested pokemon in \dump\[pokedex number]\[filename].pk7

                pkm.ResetPartyStats();
                var pkoutput = @pwd + nickname + ".pk7";
                bool exist2 = System.IO.File.Exists(pkoutput);
                if (!exist2) //do not overwrite any files, this can cause problems over a long time without the dump folder being cleared out
                {
                    File.WriteAllBytes(pkoutput, pkm.DecryptedPartyData);
                }
                else
                {
                    return;
                }
                filepaths[counter] = pkoutput; //unused currently
                hasbeentraded[counter] = false;
                counter++;
                Pk7idlecleanup(); //doesn't work currently
            }
            catch
            {
                string createText = "This Pokemon failed the idle function: " + ledybotdex + Environment.NewLine;
                string pwd = Directory.GetCurrentDirectory();
                File.WriteAllText(pwd + "\\idlefailure.txt", createText);
            }
        }
        public static void Pk7delete(int dex, string nickname)
        {
            string header = "";
            if (dex < 10)
            {
                header = "00"; //for matching the folder format
            }
            else if (dex < 100)
            {
                header = "0"; //for matching the folder format
            }
            string pk7location = Directory.GetCurrentDirectory() + "\\dump\\" + header + dex + "\\" + nickname + ".pk7";
            File.Delete(pk7location);
            return;
        }

        private static void Pk7idlecleanup()
        {
            try
            {
                if (filepaths.Capacity > 10)
                {
                    for (int i = 0; i < 7; i++)
                    {
                        bool exist = System.IO.Directory.Exists(filepaths[0]);
                        if (exist)
                        {
                            File.Delete(filepaths[0]); //this function does not work at all, haven't had time to debug 
                            filepaths.RemoveAt(0);
                        }
                        else
                        {
                            filepaths.RemoveAt(0);
                        }

                    }
                }

            }
            catch
            {
                return;
            }
            return;
        }

        public static bool Determineifdiscord(string nickname)
        {

            if (nickname.Length == 8)
            {
                string one = nickname.Substring(0, 3);
                string two = nickname.Substring(3);
                int i = 0;
                int j = 0;
                bool result = int.TryParse(two, out i); //the last 5 chars are numbers
                bool result2 = int.TryParse(one, out j); //first 3 are letters

                if (result2 == false && result == true)
                {
                    return true; //return true if it matches this format
                }
                else
                {
                    return false;
                }
            }
            else if (nickname.Length == 6)
            {
                string one = nickname.Substring(0, 1);
                string asianStrings = "용의해지썬문새해복많이지우개굴닌자쏘드라마나피오키스トウキョーウチダアツトピカチュウデオキスマナフィゴス费洛美螂甲贺忍蛙墨海马臭泥瑪納霏梦幻代欧奇希斯皮卡丘";
                bool b = asianStrings.Contains(one);
                if (b == false)
                {
                    return false;
                }
                else
                {
                    nickname = nickname.Remove(0, 1);

                    bool result3 = int.TryParse(nickname, out int i);

                    return result3;

                }

            }
            else
            {
                return false;
            }

        }

        public static async Task Tradenotfound(SocketUser trader)
        {
            nicknamelist.RemoveAt(0);
            discorduser.RemoveAt(0);
            deposited.RemoveAt(0);
            var msg2 = $"Oops! Your trade was not found within the alloted time! Skipping!";

            try
            {
                await trader.SendMessageAsync(msg2).ConfigureAwait(false);
            }
            catch (HttpException ex)
            {
                return;
            }

        }
    }

}
