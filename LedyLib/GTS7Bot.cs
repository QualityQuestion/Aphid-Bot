using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using SysBot.Pokemon.Discord;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace LedyLib
{
    public class ItemDetailsEventArgs : EventArgs
    {
        public string szTrainerName { get; set; }
        public string szNickname { get; set; }
        public string szCountry { get; set; }
        public string szSubRegion { get; set; }
        public string szSent { get; set; }
        public string fc { get; set; }
        public string page { get; set; }
        public string index { get; set; }
    }

    public class GTSBot7
    {

        //private System.IO.StreamWriter file = new StreamWriter(@"C:\Temp\ledylog.txt");

        public enum gtsbotstates { botstart, startsearch, pressSeek, openpokemonwanted, openwhatpokemon, typepokemon, presssearch, startfind, findfromend, findfromstart, trade, research, botexit, updatecomments, quicksearch, panic, queueempty };

        private string consoleName = "Ledybot";

        private TcpClient syncClient = new TcpClient();
        private IPEndPoint serverEndPointSync = null;
        private bool useLedySync = false;

        private const int SEARCHDIRECTION_FROMBACK = 0;
        private const int SEARCHDIRECTION_FROMBACKFIRSTPAGEONLY = 1;
        private const int SEARCHDIRECTION_FROMFRONT = 2;

        private uint addr_PageSize; //How many entries are on the current GTS page
        private uint addr_PageEndStartRecord; //This address holds the address to the last block in the entry-block-list
        private uint addr_PageStartStartRecord; //This address holds the address to the first block in the entry block-list
        private uint addr_PageCurrentView; //Current selected entry in the list
        private uint addr_PageStartingIndex; //To determine on which page we are, 0 = first page, 100 = second page, etc
        private uint addr_ListOfAllPageEntries; //Startingaddress of all up to 100 trade entries of the current page
        private uint addr_NoPokemonFound;

        private uint addr_box1slot1; //To inject the pokemon into box1slot1

        //private uint addr_SearchPokemonNameField = 0x301118D4; //Holds the currently typed in name in the "search pokemon" window

        private uint addr_currentScreen; //Hopefully a address to tell us in what screen we are (roughly)

        private uint addr_pokemonToFind;
        private uint addr_pokemonToFindGender;
        private uint addr_pokemonToFindLevel;

        private int val_PlazaScreen;
        private int val_Quit_SeekScreen;
        private int val_SearchScreen; //also in the box during selecting etc
        private int val_WhatPkmnScreen;
        private int val_GTSListScreen;
        private int val_BoxScreen;
        private int val_system; //during error, saving, early sending
        private int val_duringTrade; //trade is split in several steps, sometimes even 0x00

        private int iPokemonToFind = 0;
        private int iPokemonToFindGender = 0;
        private int iPokemonToFindLevel = 0;
        private int iPID = 0;
        private bool bBlacklist = false;
        private bool bReddit = false;
        private int searchDirection = 0;
        private int eidletask = 0;
        private bool eidleshiny = false;
        private int searchattempts = 0;
        private int dexnumber = 0;
        private string szFC = "";
        private byte[] principal = new byte[4];

        public bool botstop = false;
        private int botState = 0;
        public int botresult = 0;
        private int attempts = 0;
        Task<bool> waitTaskbool;
        private int commandtime = 250;
        private int delaytime = 150;
        private int o3dswaittime = 1000;
        private bool tradeQueue;
        private const int maxQueueAttempts = 3;
        private int queueAttempts = 0;

        private int listlength = 0;
        private int startIndex = 0;
        private byte[] block = new byte[256];
        private int tradeIndex = -1;
        private uint addr_PageEntry = 0;
        private bool foundLastPage = false;
        private int counter = 0;
        private int attemptz = 0;
        private int discordattempts = 0;
        private int passthroughs = 5;
        private int yaacounter = 0;
        private int generationlimiter = 0;
        private bool whaaa = false;
        private Tuple<string, string, int, int, int, ArrayList> details;

        private RemoteControl _helper;
        private LookupTable _pkTable;
        private ScriptHelper _scriptHelper;
        private Data _data;

        public delegate void changeStatus(string msg);

        public event EventHandler<ItemDetailsEventArgs> onItemDetails;

        public event changeStatus onChangeStatus;

        private async Task<bool> isCorrectWindow(int expectedScreen)
        {
            await Task.Delay(o3dswaittime);
            await _helper.waitNTRread(addr_currentScreen);
            int screenID = (int)_helper.lastRead;

            //file.WriteLine("Checkscreen: " + expectedScreen + " - " + screenID + " botstate:" + botState);
            //file.Flush();
            return expectedScreen == screenID;
        }

        private Boolean canThisTrade(byte[] principal, string consoleName, string trainerName, string country, string region, string pokemon, string szFC, string page, string index)
        {
            NetworkStream clientStream = syncClient.GetStream();
            byte[] buffer = new byte[4096];
            byte[] messageID = { 0x00 };
            string szmessage = consoleName + '\t' + trainerName + '\t' + country + '\t' + region + '\t' + pokemon + '\t' + page + "\t" + index + "\t";
            byte[] toSend = Encoding.UTF8.GetBytes(szmessage);

            buffer = messageID.Concat(principal).Concat(toSend).ToArray();
            clientStream.Write(buffer, 0, buffer.Length);
            clientStream.Flush();
            byte[] message = new byte[4096];
            try
            {
                //blocks until a client sends a message
                int bytesRead = clientStream.Read(message, 0, 4096);
                if (message[0] == 0x02)
                {
                    _data.banlist.Add(szFC);
                }
                return message[0] == 0x01;
            }
            catch
            {
                return false;
                //a socket error has occured
            }
        }

        public GTSBot7(int iP, int iPtF, int iPtFGender, int iPtFLevel, bool bBlacklist, bool bReddit, int iSearchDirection, string waittime, string consoleName, bool useLedySync, string ledySyncIp, string ledySyncPort, int game, bool tradeQueue, RemoteControl helper, LookupTable pkTable, Data data, ScriptHelper scriptHelper, int idletask, bool idleshiny, int discordsearchattempts)
        {
            this.iPokemonToFind = iPtF;
            this.iPokemonToFindGender = iPtFGender;
            this.iPokemonToFindLevel = iPtFLevel;
            this.iPID = iP;
            this.bBlacklist = bBlacklist;
            this.bReddit = bReddit;
            this.searchDirection = iSearchDirection;
            this.eidletask = idletask;
            this.eidleshiny = idleshiny;
            this.searchattempts = discordsearchattempts;
            this.o3dswaittime = Int32.Parse(waittime);
            _helper = helper;
            _pkTable = pkTable;
            _data = data;
            _scriptHelper = scriptHelper;
            if (useLedySync)
            {
                this.useLedySync = useLedySync;
                int iPort = Int32.Parse(ledySyncPort);
                this.serverEndPointSync = new IPEndPoint(IPAddress.Parse(ledySyncIp), iPort);
                syncClient.Connect(serverEndPointSync);
            }

            //if(useLedybotTV)
            //{
            //    this.useLedybotTV = useLedybotTV;
            //    int iPort = Int32.Parse(ledybotTVPort);
            //    this.serverEndPointTV = new IPEndPoint(IPAddress.Parse(ledybotTVIp), iPort);
            //    tvClient.Connect(serverEndPointTV);
            //}
            this.consoleName = consoleName;

            if (game == 0)
            {
                addr_PageSize = 0x32A6A1A4; //How many entries are on the current GTS page
                addr_NoPokemonFound = 0x3000172C;
                addr_PageEndStartRecord = 0x32A6A68C; //This address holds the address to the last block in the entry-block-list
                addr_PageStartStartRecord = 0x32A6A690; //This address holds the address to the first block in the entry block-list
                addr_PageCurrentView = 0x305ea384; //Current selected entry in the list
                addr_PageStartingIndex = 0x32A6A190; //To determine on which page we are, 0 = first page, 100 = second page, etc
                addr_ListOfAllPageEntries = 0x32A6A7C4; //Startingaddress of all up to 100 trade entries of the current page


                addr_box1slot1 = 0x330D9838; //To inject the pokemon into box1slot1

                addr_currentScreen = 0x00674802; //Hopefully a address to tell us in what screen we are (roughly)

                addr_pokemonToFind = 0x32A6A180;
                addr_pokemonToFindGender = 0x32A6A184;
                addr_pokemonToFindLevel = 0x32A6A188;

                val_PlazaScreen = 0x00;
                val_Quit_SeekScreen = 0x3F2B;
                val_SearchScreen = 0x4128; //also in the box during selecting etc
                val_WhatPkmnScreen = 0x4160;
                val_GTSListScreen = 0x4180;
                val_BoxScreen = 0x4120;
                val_system = 0x41A8; //during error, saving, early sending
                val_duringTrade = 0x3FD5; //trade is split in several steps, sometimes even 0x00
            }
            else if (game == 1)
            {
                addr_PageSize = 0x329921A4;
                addr_PageEndStartRecord = 0x3299268C;
                addr_PageStartStartRecord = 0x32992690;
                addr_PageCurrentView = 0x305CD9F4;
                addr_PageStartingIndex = 0x32992190;
                addr_ListOfAllPageEntries = 0x329927C4;
                addr_NoPokemonFound = 0x3000172C;

                addr_box1slot1 = 0x33015AB0;

                addr_currentScreen = 0x006A610A;

                addr_pokemonToFind = 0x32992180;
                addr_pokemonToFindGender = 0x32992184;
                addr_pokemonToFindLevel = 0x32992188;

                val_PlazaScreen = 0x00;
                val_Quit_SeekScreen = 0x3F2B;
                val_SearchScreen = 0x412A;
                val_WhatPkmnScreen = 0x1040;
                val_GTSListScreen = 0x4180;
                val_BoxScreen = 0x4120;
                val_system = 0x1C848;
                val_duringTrade = 0x3FD5;
            }

            this.tradeQueue = tradeQueue;

        }

        public async Task<int> RunBot()
        {
            bool correctScreen = true;
            byte[] pokemonIndex = new byte[2];
            byte pokemonGender = 0x0;
            byte pokemonLevel = 0x0;
            int holdPokemontoFind = iPokemonToFind;
            int idlePokemontoFind = iPokemonToFind;
            byte[] full = BitConverter.GetBytes(holdPokemontoFind);
            pokemonIndex[0] = full[0];
            pokemonIndex[1] = full[1];
            full = BitConverter.GetBytes(iPokemonToFindGender);
            pokemonGender = full[0];
            full = BitConverter.GetBytes(iPokemonToFindLevel);
            pokemonLevel = full[0];
            int panicAttempts = 0;

            if (tradeQueue)
            {
                botState = (int)gtsbotstates.queueempty;
            }

            while (!botstop)
            {
                if (botState != (int)gtsbotstates.panic)
                {
                    panicAttempts = 0;
                }
                switch (botState)
                {
                    case (int)gtsbotstates.queueempty:
                        await Task.Delay(2000);
                        if (_data.tradeQueueRec.Count > 0)
                        {
                            botState = (int)gtsbotstates.startsearch;
                        }

                        break;
                    case (int)gtsbotstates.botstart:
                        if (bReddit)
                            _data.updateJSON();
                        botState = (int)gtsbotstates.startsearch;
                        break;
                    case (int)gtsbotstates.updatecomments:
                        _data.updateJSON();
                        botState = (int)gtsbotstates.research;
                        break;
                    case (int)gtsbotstates.startsearch:
                        if (tradeQueue)
                        {
                            full = BitConverter.GetBytes(_data.tradeQueueRec[0].Item2);
                            pokemonIndex[0] = full[0];
                            pokemonIndex[1] = full[1];
                            _data.tradeQueueRec[0] = new Tuple<string, int, int>(_data.tradeQueueRec[0].Item1, _data.tradeQueueRec[0].Item2, _data.tradeQueueRec[0].Item3 + 1);
                        }
                        onChangeStatus?.Invoke("Setting Pokemon to find");
                        waitTaskbool = _helper.waitNTRwrite(addr_pokemonToFind, pokemonIndex, iPID);
                        waitTaskbool = _helper.waitNTRwrite(addr_pokemonToFindGender, pokemonGender, iPID);
                        waitTaskbool = _helper.waitNTRwrite(addr_pokemonToFindLevel, pokemonLevel, iPID);
                        botState = (int)gtsbotstates.pressSeek;
                        break;
                    case (int)gtsbotstates.pressSeek:
                        onChangeStatus?.Invoke("Pressing seek button");
                        //Seek/Deposite pokemon screen
                        correctScreen = await isCorrectWindow(val_Quit_SeekScreen);
                        if (!correctScreen)
                        {
                            botState = (int)gtsbotstates.panic;
                            break;
                        }
                        await _helper.waittouch(160, 80);
                        await Task.Delay(commandtime + delaytime);
                        botState = (int)gtsbotstates.presssearch;
                        break;
                    case (int)gtsbotstates.presssearch:
                        onChangeStatus?.Invoke("Press search button");
                        correctScreen = await isCorrectWindow(val_SearchScreen);
                        if (!correctScreen)
                        {
                            botState = (int)gtsbotstates.panic;
                            break;
                        }
                        //Pokemon wanted screen again, this time with filled out information
                        await Task.Delay(2000);
                        waitTaskbool = _helper.waittouch(160, 185);
                        if (await waitTaskbool)
                        {
                            botState = (int)gtsbotstates.findfromstart;
                            await Task.Delay(2250);
                        }
                        else
                        {
                            attempts++;
                            botresult = 6;
                            botState = (int)gtsbotstates.startsearch;
                        }
                        break;
                    case (int)gtsbotstates.findfromstart:
                        correctScreen = await isCorrectWindow(val_GTSListScreen);
                        if (!correctScreen)
                        {
                            uint lastreadhold = _helper.lastRead;

                            await _helper.waitNTRread(addr_NoPokemonFound);
                            uint npfid = _helper.lastRead;
                            if (npfid == 7274574)
                            {
                                attemptz = 4;
                                holdPokemontoFind = idlePokemontoFind + yaacounter;
                                await Task.Delay(commandtime + delaytime + 500);
                                botState = (int)gtsbotstates.startsearch;
                            }
                            //Hotfix for Only one Pokemon on List
                            else if (lastreadhold != 0x40C0)
                            {
                                botState = (int)gtsbotstates.panic;
                                break;
                            }
                        }
                        //GTS entry list screen, cursor at position 1
                        await _helper.waitNTRread(addr_PageSize);

                        attempts = 0;
                        listlength = (int)_helper.lastRead;

                        if (listlength == 100 && !foundLastPage && searchDirection == SEARCHDIRECTION_FROMBACK)
                        {
                            onChangeStatus?.Invoke("Moving to last page");
                            waitTaskbool = _helper.waitNTRread(addr_PageStartingIndex);
                            if (await waitTaskbool)
                            {
                                startIndex = (int)_helper.lastRead;
                                waitTaskbool = _helper.waitNTRwrite(addr_PageStartingIndex, (uint)(startIndex + 200), iPID);
                                if (await waitTaskbool)
                                {
                                    startIndex += 100;
                                    _helper.quickbuton(_pkTable.DpadRIGHT, commandtime);
                                    await Task.Delay(commandtime + delaytime);
                                    _helper.quickbuton(_pkTable.DpadLEFT, commandtime);
                                    await Task.Delay(commandtime + delaytime);
                                    _helper.quickbuton(_pkTable.DpadLEFT, commandtime);
                                    await Task.Delay(commandtime + delaytime);
                                    //prevent potential loop by going left once more before the page is actually loaded
                                    _helper.quickbuton(_pkTable.DpadLEFT, commandtime);
                                    await Task.Delay(commandtime + delaytime);
                                    await Task.Delay(3000);
                                    _helper.quicktouch(10, 10, commandtime);
                                    await Task.Delay(commandtime + delaytime + 250);
                                    _helper.quicktouch(10, 10, commandtime);
                                    await Task.Delay(commandtime + delaytime + 250);
                                    _helper.quicktouch(10, 10, commandtime);
                                    await Task.Delay(commandtime + delaytime + 250);
                                    _helper.quicktouch(10, 10, commandtime);
                                    await Task.Delay(commandtime + delaytime + 250);
                                    await _helper.waitNTRread(addr_PageStartingIndex);
                                    if (_helper.lastRead == 0)
                                    {
                                        foundLastPage = true;
                                    }
                                    else
                                    {
                                        botState = (int)gtsbotstates.findfromend;
                                    }

                                }
                            }
                        }
                        else
                        {

                            onChangeStatus?.Invoke("Looking for a pokemon to trade");

                            foundLastPage = true;
                            attempts = 0;
                            listlength = (int)_helper.lastRead;
                            dexnumber = 0;
                            if (searchDirection == SEARCHDIRECTION_FROMBACK || searchDirection == SEARCHDIRECTION_FROMBACKFIRSTPAGEONLY)
                            {
                                await _helper.waitNTRread(addr_PageEndStartRecord);
                            }
                            else
                            {
                                await _helper.waitNTRread(addr_PageStartStartRecord);
                            }
                            addr_PageEntry = _helper.lastRead;
                            await _helper.waitNTRread(addr_ListOfAllPageEntries, (uint)(256 * 100));
                            byte[] blockBytes = _helper.lastArray;
                            int iStartIndex, iEndIndex, iDirection, iNextPrevBlockOffest;
                            if (searchDirection == SEARCHDIRECTION_FROMBACK || searchDirection == SEARCHDIRECTION_FROMBACKFIRSTPAGEONLY)
                            {
                                iStartIndex = listlength;
                                iEndIndex = 0;
                                iDirection = -1;
                                iNextPrevBlockOffest = 0;
                            }
                            else
                            {
                                iStartIndex = 1;
                                iEndIndex = listlength + 1;
                                iDirection = 1;
                                iNextPrevBlockOffest = 4;
                            }
                            for (int i = iStartIndex; i * iDirection < iEndIndex; i += iDirection)
                            {
                                Array.Copy(blockBytes, addr_PageEntry - addr_ListOfAllPageEntries, block, 0, 256);
                                dexnumber = BitConverter.ToInt16(block, 0xC);
                                Array.Copy(block, 0x48, principal, 0, 4);
                                byte checksum = _data.calculateChecksum(principal);
                                byte[] fc = new byte[8];
                                Array.Copy(principal, 0, fc, 0, 4);
                                fc[4] = checksum;
                                long iFC = BitConverter.ToInt64(fc, 0);
                                szFC = iFC.ToString().PadLeft(12, '0');
                                if (tradeQueue)
                                {
                                    if (!_data.tradeQueueRec.Any())
                                    {
                                        tradeIndex = -1;
                                        startIndex = 0;
                                        break;
                                    }
                                    if (_data.tradeQueueRec[0].Item1 != szFC)
                                    {
                                        addr_PageEntry = BitConverter.ToUInt32(block, iNextPrevBlockOffest);
                                        continue;
                                    }
                                }
                                if (_data.giveawayDetails.ContainsKey(dexnumber))
                                {
                                    _data.giveawayDetails.TryGetValue(dexnumber, out details);
                                    if (details.Item1 == "")
                                    {
                                        string szNickname = Encoding.Unicode.GetString(block, 0x14, 24).Trim('\0'); //fix to prevent nickname clipping. Count should be 24, 2 bytes per letter, 2x12=24, not 20.
                                        string invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
                                        Task t = Task.Run(() =>
                                       {
                                           foreach (char c in invalid)
                                           {
                                               szNickname = szNickname.Replace(c.ToString(), "a"); //very rare edge case where swapping an invalid char doesn't work, after 20 seconds we panic
                                           }
                                       });
                                        TimeSpan ts = TimeSpan.FromMilliseconds(20000);
                                        if (!t.Wait(ts))
                                        {
                                            botState = (int)gtsbotstates.panic;
                                        }
                                        string szFileToFind = details.Item2 + szNickname + ".pk7";
                                        if (eidletask == 1) //if idletask is checked off
                                        {
                                            if (generationlimiter < 2) //only generate two pokemon at a time in the GTS idle queue
                                            {
                                                int leveltemp = block[0xF];
                                                int gendertemp = block[0xE];
                                                if ((gendertemp == 0 || gendertemp == 1 || gendertemp == 2) && (leveltemp <= 10 || leveltemp >= 0)) //only accept non-corrupted GTS deposits
                                                {
                                                    TradeModule.LedybotIdleTask(dexnumber, leveltemp, gendertemp, szNickname, eidleshiny);
                                                }
                                                generationlimiter++;
                                            }
                                        }
                                        if (!File.Exists(szFileToFind))
                                        {
                                            addr_PageEntry = BitConverter.ToUInt32(block, iNextPrevBlockOffest);
                                            continue;
                                        }
                                        bool isdiscord = TradeModule.Determineifdiscord(szNickname); //if this is a discord trade, message the user
                                        if (isdiscord == true)
                                        {
                                            discordattempts = 0;
                                            await TradeModule.Discorduserfinder(szNickname);
                                            whaaa = true;
                                        }

                                    }

                                    int gender = block[0xE];
                                    int level = block[0xF];
                                    if ((gender == 0 || gender == 1 || gender == 2) && (level <= 10 || level >= 0) && counter > 0) //nly accept non-corrupted GTS deposits and ensure it has looked at the queue once to generate index 0 and 1
                                    {
                                        string szTrainerName = Encoding.Unicode.GetString(block, 0x4C, 24).Trim('\0');
                                        int countryIndex = BitConverter.ToInt16(block, 0x68);
                                        string country = "-";
                                        _data.countries.TryGetValue(countryIndex, out country);
                                        _data.getSubRegions(countryIndex);
                                        int subRegionIndex = BitConverter.ToInt16(block, 0x6A);
                                        string subregion = "-";
                                        _data.regions.TryGetValue(subRegionIndex, out subregion);
                                        int ipage = Convert.ToInt32(Math.Floor(startIndex / 100.0)) + 1;
                                        if (useLedySync && !_data.banlist.Contains(szFC) && canThisTrade(principal, consoleName, szTrainerName, country, subregion, _pkTable.Species7[dexnumber - 1], szFC, ipage + "", (i - 1) + ""))
                                        {
                                            onChangeStatus?.Invoke("Found a pokemon to trade");
                                            tradeIndex = i - 1;
                                            botState = (int)gtsbotstates.trade;
                                            break;
                                        }
                                        if (!useLedySync)
                                        {
                                            if ((!bReddit || _data.commented.Contains(szFC)) && !details.Item6.Contains(BitConverter.ToInt32(principal, 0)) && !_data.banlist.Contains(szFC))
                                            {
                                                if (whaaa == true)
                                                {
                                                    passthroughs = 5;
                                                    whaaa = false;
                                                }
                                                passthroughs++;
                                                generationlimiter = 0;
                                                attemptz = 0;
                                                counter++;
                                                tradeIndex = i - 1;
                                                botState = (int)gtsbotstates.trade;
                                                break;
                                            }
                                        }
                                    }
                                }
                                addr_PageEntry = BitConverter.ToUInt32(block, iNextPrevBlockOffest);
                            }
                            if (tradeIndex == -1)
                            {
                                if (startIndex == 0)
                                {
                                    onChangeStatus?.Invoke("No pokemon to trade found");
                                    generationlimiter = 0;
                                    attemptz++;
                                    _helper.quickbuton(_pkTable.keyB, commandtime);
                                    await Task.Delay(commandtime + delaytime + 500);
                                    _helper.quickbuton(_pkTable.keyB, commandtime);
                                    await Task.Delay(commandtime + delaytime + 500);
                                    if (tradeQueue)
                                    {
                                        if (!_data.tradeQueueRec.Any())
                                            botState = (int)gtsbotstates.queueempty;
                                        else if (_data.tradeQueueRec[0].Item3 >= maxQueueAttempts)
                                        {
                                            botState = (int)gtsbotstates.queueempty;
                                            _data.RemoveFromQueue(0);
                                        }
                                        else
                                        {
                                            botState = (int)gtsbotstates.research;
                                        }
                                    }
                                    else if (bReddit)
                                    {
                                        botState = (int)gtsbotstates.updatecomments;
                                    }
                                    else
                                    {
                                        botState = (int)gtsbotstates.research;
                                    }
                                }
                                else
                                {
                                    onChangeStatus?.Invoke("No pokemon to trade on this page, try previous page");
                                    attemptz++;
                                    startIndex -= 100;
                                    _helper.quickbuton(_pkTable.DpadLEFT, commandtime);
                                    await Task.Delay(commandtime + delaytime);
                                    await Task.Delay(2250);
                                    botState = (int)gtsbotstates.findfromend;
                                }
                            }
                            counter++;
                            passthroughs++;
                            bool isEmpty = !TradeModule.deposited.Any(); //determine if anyone is in the GTS queue
                            if (attemptz > 3 && isEmpty == true) //if the queue is empty, only search on the idle Pokemon empty page 3 times.
                            {
                                yaacounter++;
                                counter = 0; //reset values
                                attemptz = 0;

                                int[] mythicals = { 151, 251, 385, 386, 494, 493, 492, 491, 490, 649, 648, 647, 721, 720, 719, 805, 806, 807 };
                                int[] evolveByTrade = { 525, 366, 356, 125, 349, 75, 533, 93, 64, 588, 67, 126, 95, 708, 61, 137, 233, 710, 112, 123, 117, 616, 79, 682, 684 };
                                if (mythicals.Contains(idlePokemontoFind + yaacounter)) //avoid searching for impossible GTS deposits
                                {
                                    do
                                    {
                                        yaacounter++;

                                    } while (mythicals.Contains(idlePokemontoFind + yaacounter));
                                }
                                else if (evolveByTrade.Contains(idlePokemontoFind + yaacounter)) //avoid searching for impossible GTS deposits
                                {
                                    do
                                    {
                                        yaacounter++;

                                    } while (evolveByTrade.Contains(idlePokemontoFind + yaacounter));
                                }
                                if (idlePokemontoFind >= 804) //if we hit the end of the GTS pokedex, go back to the beginning.
                                {
                                    yaacounter = 0;
                                    idlePokemontoFind = 1;
                                }
                                full = BitConverter.GetBytes(idlePokemontoFind + yaacounter);
                                pokemonIndex[0] = full[0];
                                pokemonIndex[1] = full[1];
                                await Task.Delay(2250);
                                botState = (int)gtsbotstates.startsearch;
                            }

                            if (isEmpty == false) //someone is in the GTS queue
                            {
                                if (TradeModule.qcOnIndexZero == true) //someone tried to qc at position 0 in the queue
                                {
                                    discordattempts = 100;
                                    TradeModule.qcOnIndexZero = false;
                                }

                                if (discordattempts > searchattempts) //if we've gone past the amount of search attempts specified in the GUI
                                {
                                    await TradeModule.Tradenotfound(TradeModule.discorduser[0]); //tell the user they're skipped and pop them from the queue
                                    holdPokemontoFind = idlePokemontoFind + yaacounter; //reset holdpokemontofind value to current idle pokemon
                                    discordattempts = 0; //reset our discord search attempt counter
                                    isEmpty = !TradeModule.deposited.Any(); //check to see if anyone else is in the queue
                                    counter = 0;
                                    attemptz = 0; //reset values
                                    passthroughs = 0;
                                }

                                discordattempts++;
                                if (isEmpty == false && (holdPokemontoFind != TradeModule.deposited[0])) //if someone is in the queue, and we aren't searching for what they deposited
                                {
                                    holdPokemontoFind = TradeModule.deposited[0];
                                    full = BitConverter.GetBytes(holdPokemontoFind); //search for what they deposited
                                    pokemonIndex[0] = full[0];
                                    pokemonIndex[1] = full[1];
                                    _helper.quickbuton(_pkTable.keyB, commandtime);
                                    await Task.Delay(commandtime + delaytime + 500);
                                    _helper.quickbuton(_pkTable.keyB, commandtime);
                                    await Task.Delay(commandtime + delaytime + 500);
                                    await Task.Delay(2250);
                                    tradeIndex = -1;
                                    startIndex = 0;
                                    botState = (int)gtsbotstates.startsearch;
                                }
                            }
                        }

                        break;
                    case (int)gtsbotstates.findfromend:
                        correctScreen = await isCorrectWindow(val_GTSListScreen);
                        if (!correctScreen)
                        {
                            //Hotfix for Only one Pokemon on List
                            if (_helper.lastRead != 0x40C0)
                            {
                                botState = (int)gtsbotstates.panic;
                                break;
                            }
                        }
                        //also GTS entry list screen, but cursor is at the end of the list in this case
                        await _helper.waitNTRread(addr_PageSize);

                        attempts = 0;
                        listlength = (int)_helper.lastRead;
                        if (listlength == 100 && !foundLastPage)
                        {
                            onChangeStatus?.Invoke("Moving to last page");
                            startIndex += 100;
                            _helper.quickbuton(_pkTable.DpadRIGHT, commandtime);
                            await Task.Delay(commandtime + delaytime);
                            await Task.Delay(3000);
                            _helper.quicktouch(10, 10, commandtime);
                            await Task.Delay(commandtime + delaytime + 250);
                            _helper.quicktouch(10, 10, commandtime);
                            await Task.Delay(commandtime + delaytime + 250);
                            _helper.quicktouch(10, 10, commandtime);
                            await Task.Delay(commandtime + delaytime + 250);
                            _helper.quicktouch(10, 10, commandtime);
                            await Task.Delay(commandtime + delaytime + 250);
                            await _helper.waitNTRread(addr_PageStartingIndex);
                            if (_helper.lastRead == 0)
                            {
                                foundLastPage = true;
                            }
                            botState = (int)gtsbotstates.findfromstart;
                        }
                        else
                        {
                            foundLastPage = true;
                            attempts = 0;
                            listlength = (int)_helper.lastRead;
                            dexnumber = 0;
                            await _helper.waitNTRread(addr_PageEndStartRecord);
                            addr_PageEntry = _helper.lastRead;
                            await _helper.waitNTRread(addr_ListOfAllPageEntries, (uint)(256 * 100));
                            byte[] blockBytes = _helper.lastArray;
                            for (int i = listlength; i > 0; i--)
                            {
                                onChangeStatus?.Invoke("Looking for a pokemon to trade");
                                Array.Copy(blockBytes, addr_PageEntry - addr_ListOfAllPageEntries, block, 0, 256);
                                dexnumber = BitConverter.ToInt16(block, 0xC);
                                Array.Copy(block, 0x48, principal, 0, 4);
                                byte checksum = _data.calculateChecksum(principal);
                                byte[] fc = new byte[8];
                                Array.Copy(principal, 0, fc, 0, 4);
                                fc[4] = checksum;
                                long iFC = BitConverter.ToInt64(fc, 0);
                                szFC = iFC.ToString().PadLeft(12, '0');
                                if (tradeQueue)
                                {
                                    if (!_data.tradeQueueRec.Any())
                                    {
                                        tradeIndex = -1;
                                        startIndex = 0;
                                        break;
                                    }
                                    if (_data.tradeQueueRec[0].Item1 != szFC)
                                    {
                                        addr_PageEntry = BitConverter.ToUInt32(block, 0);
                                        continue;
                                    }
                                }
                                if (_data.giveawayDetails.ContainsKey(dexnumber))
                                {
                                    _data.giveawayDetails.TryGetValue(dexnumber, out details);
                                    if (details.Item1 == "")
                                    {
                                        string szNickname = Encoding.Unicode.GetString(block, 0x14, 24).Trim('\0'); //fix to prevent nickname clipping. Count should be 24, 2 bytes per letter, 2x12=24, not 20.
                                        string szFileToFind = details.Item2 + szNickname + ".pk7";
                                        if (!File.Exists(szFileToFind))
                                        {
                                            addr_PageEntry = BitConverter.ToUInt32(block, 0);
                                            continue;
                                        }

                                    }

                                    int gender = block[0xE];
                                    int level = block[0xF];
                                    if ((gender == 0 || gender == 1 || gender == 2) && (level <= 10 || level >= 0) && counter > 0)
                                    {
                                        string szTrainerName = Encoding.Unicode.GetString(block, 0x4C, 24).Trim('\0');
                                        int countryIndex = BitConverter.ToInt16(block, 0x68);
                                        string country = "-";
                                        _data.countries.TryGetValue(countryIndex, out country);
                                        _data.getSubRegions(countryIndex);
                                        int subRegionIndex = BitConverter.ToInt16(block, 0x6A);
                                        string subregion = "-";
                                        _data.regions.TryGetValue(subRegionIndex, out subregion);
                                        int ipage = Convert.ToInt32(Math.Floor(startIndex / 100.0)) + 1;
                                        if (useLedySync && !_data.banlist.Contains(szFC) && canThisTrade(principal, consoleName, szTrainerName, country, subregion, _pkTable.Species7[dexnumber - 1], szFC, ipage + "", (i - 1) + ""))
                                        {
                                            onChangeStatus?.Invoke("Found a pokemon to trade");
                                            tradeIndex = i - 1;
                                            botState = (int)gtsbotstates.trade;
                                            break;
                                        }
                                        if (!useLedySync)
                                        {
                                            if ((!bReddit || _data.commented.Contains(szFC)) && !details.Item6.Contains(BitConverter.ToInt32(principal, 0)) && !_data.banlist.Contains(szFC))
                                            {
                                                if (whaaa == true)
                                                {
                                                    passthroughs = 5;
                                                    whaaa = false;
                                                }
                                                passthroughs++;
                                                generationlimiter = 0;
                                                attemptz = 0;
                                                counter++;
                                                tradeIndex = i - 1;
                                                botState = (int)gtsbotstates.trade;
                                                break;
                                            }
                                        }
                                    }
                                }
                                addr_PageEntry = BitConverter.ToUInt32(block, 0);

                            }
                            if (tradeIndex == -1)
                            {
                                if (listlength < 100 && startIndex >= 200)
                                {
                                    onChangeStatus?.Invoke("No pokemon to trade on this page, try previous page");
                                    attemptz++;
                                    for (int i = 0; i < listlength; i++)
                                    {
                                        _helper.quickbuton(_pkTable.DpadLEFT, commandtime);
                                        await Task.Delay(commandtime + delaytime);
                                    }
                                    startIndex -= 100;
                                    await Task.Delay(2250);
                                    botState = (int)gtsbotstates.findfromend; //hope this is right
                                }
                                else if (startIndex >= 200)
                                {
                                    onChangeStatus?.Invoke("No pokemon to trade on this page, try previous page");
                                    attemptz++;
                                    waitTaskbool = _helper.waitNTRwrite(addr_PageStartingIndex, (uint)(startIndex - 200), iPID);
                                    if (await waitTaskbool)
                                    {
                                        await _helper.waitNTRwrite(addr_PageSize, 0x64, iPID);
                                        startIndex -= 100;
                                        _helper.quickbuton(_pkTable.DpadLEFT, commandtime);
                                        await Task.Delay(commandtime + delaytime);
                                        _helper.quickbuton(_pkTable.DpadRIGHT, commandtime);
                                        await Task.Delay(commandtime + delaytime);
                                        _helper.quickbuton(_pkTable.DpadRIGHT, commandtime);
                                        await Task.Delay(commandtime + delaytime);
                                        await Task.Delay(3000);
                                        _helper.quicktouch(10, 10, commandtime);
                                        await Task.Delay(commandtime + delaytime + 250);
                                        _helper.quicktouch(10, 10, commandtime);
                                        await Task.Delay(commandtime + delaytime + 250);
                                        _helper.quicktouch(10, 10, commandtime);
                                        await Task.Delay(commandtime + delaytime + 250);
                                        _helper.quicktouch(10, 10, commandtime);
                                        await Task.Delay(commandtime + delaytime + 250);
                                        botState = (int)gtsbotstates.findfromstart;
                                    }
                                }
                                else if (startIndex == 0)
                                {
                                    onChangeStatus?.Invoke("No pokemon to trade found");
                                    generationlimiter = 0;
                                    attemptz++;
                                    _helper.quickbuton(_pkTable.keyB, commandtime);
                                    await Task.Delay(commandtime + delaytime + 500);
                                    _helper.quickbuton(_pkTable.keyB, commandtime);
                                    await Task.Delay(commandtime + delaytime + 500);
                                    if (tradeQueue)
                                    {
                                        if (!_data.tradeQueueRec.Any())
                                            botState = (int)gtsbotstates.queueempty;
                                        else if (_data.tradeQueueRec[0].Item3 >= maxQueueAttempts)
                                        {
                                            botState = (int)gtsbotstates.queueempty;
                                            _data.RemoveFromQueue(0);
                                        }
                                        else
                                        {
                                            botState = (int)gtsbotstates.research;
                                        }
                                    }
                                    else if (bReddit)
                                    {
                                        botState = (int)gtsbotstates.updatecomments;
                                    }
                                    else
                                    {
                                        botState = (int)gtsbotstates.research;
                                    }
                                }
                                else if (startIndex < 200)
                                {
                                    onChangeStatus?.Invoke("No pokemon to trade found");
                                    generationlimiter = 0;
                                    attemptz++;
                                    botState = (int)gtsbotstates.quicksearch;
                                }
                            }

                        }

                        break;
                    case (int)gtsbotstates.trade:
                        //still in GTS list screen
                        //write index we want to trade
                        int page = Convert.ToInt32(Math.Floor(startIndex / 100.0)) + 1;
                        onChangeStatus?.Invoke("Trading pokemon on page " + page + " index " + tradeIndex + "");

                        waitTaskbool = _helper.waitNTRwrite(addr_PageCurrentView, BitConverter.GetBytes(tradeIndex), iPID);
                        if (await waitTaskbool)
                        {
                            string szNickname = Encoding.Unicode.GetString(block, 0x14, 24).Trim('\0'); //fix to prevent nickname clipping. Count should be 24, 2 bytes per letter, 2x12=24, not 20.
                            string invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
                            foreach (char c in invalid)
                            {
                                szNickname = szNickname.Replace(c.ToString(), "a");
                            }
                            string szPath = details.Item1;
                            string szFileToFind = details.Item2 + szNickname + ".pk7";
                            if (File.Exists(szFileToFind))
                            {
                                szPath = szFileToFind;
                            }

                            byte[] pkmEncrypted = System.IO.File.ReadAllBytes(szPath);
                            byte[] cloneshort = PKHaX.encryptArray(pkmEncrypted.Take(232).ToArray());
                            string ek7 = BitConverter.ToString(cloneshort).Replace("-", ", 0x");

                            //optional: grab some trainer data
                            string szTrainerName = Encoding.Unicode.GetString(block, 0x4C, 24).Trim('\0');
                            int countryIndex = BitConverter.ToInt16(block, 0x68);
                            string country = "-";
                            _data.countries.TryGetValue(countryIndex, out country);
                            _data.getSubRegions(countryIndex);
                            int subRegionIndex = BitConverter.ToInt16(block, 0x6A);
                            string subregion = "-";
                            _data.regions.TryGetValue(subRegionIndex, out subregion);

                            //Inject the Pokemon to box1slot1
                            _scriptHelper.write(addr_box1slot1, cloneshort, iPID);
                            //spam a to trade pokemon
                            _helper.quickbuton(_pkTable.keyA, commandtime);
                            await Task.Delay(commandtime + delaytime + 2500 + o3dswaittime);
                            _helper.quickbuton(_pkTable.keyA, commandtime);
                            await Task.Delay(commandtime + delaytime);
                            _helper.quickbuton(_pkTable.keyA, commandtime);
                            await Task.Delay(commandtime + delaytime);
                            _helper.quickbuton(_pkTable.keyA, commandtime);
                            await Task.Delay(commandtime + delaytime);

                            if (bBlacklist)
                            {
                                details.Item6.Add(BitConverter.ToInt32(principal, 0));
                            }

                            //during the trade spam a/b to get back to the start screen in case of "this pokemon has been traded"
                            await Task.Delay(10250);
                            _helper.quickbuton(_pkTable.keyA, commandtime);
                            await Task.Delay(commandtime + delaytime);
                            await Task.Delay(1000);
                            _helper.quickbuton(_pkTable.keyB, commandtime);
                            await Task.Delay(commandtime + delaytime);
                            await Task.Delay(1000);
                            _helper.quickbuton(_pkTable.keyB, commandtime);
                            await Task.Delay(commandtime + delaytime);
                            await Task.Delay(1000);
                            _helper.quickbuton(_pkTable.keyB, commandtime);
                            await Task.Delay(commandtime + delaytime);
                            await Task.Delay(32000);
                            TradeModule.Pk7delete(dexnumber, szNickname);
                            generationlimiter = 0;
                            bool isdiscord = TradeModule.Determineifdiscord(szNickname);
                            if (isdiscord == true) //if the person we just traded was in our GTS queue
                            {
                                szPath = details.Item2 + szNickname + "_RET.pk7"; //create a .pk7 of what they deposited
                                _scriptHelper.read(addr_box1slot1, (uint)cloneshort.Length, szPath, iPID);
                                await Task.Delay(1000);
                                await TradeModule.Discordpokesender(szNickname, szPath); //send it to them
                                holdPokemontoFind = idlePokemontoFind + yaacounter; //change value
                            }
                            await _helper.waitNTRread(addr_box1slot1 + 0x8, 2);

                            byte[] ek7ID = new byte[2];

                            Array.Copy(cloneshort, 8, ek7ID, 0, 2);

                            if (tradeQueue)
                                _data.RemoveFromQueue(0);

                            if (!_helper.lastArray.SequenceEqual(ek7ID))
                            {
                                var args = new ItemDetailsEventArgs()
                                {
                                    fc = szFC,
                                    index = tradeIndex + "",
                                    page = page + "",
                                    szCountry = country,
                                    szNickname = szNickname,
                                    szSent = _pkTable.Species7[dexnumber - 1],
                                    szSubRegion = subregion,
                                    szTrainerName = szTrainerName
                                };

                                onItemDetails?.Invoke(this, args);
                                if (details.Item5 > 0)
                                {
                                    _data.giveawayDetails[dexnumber] = new Tuple<string, string, int, int, int, ArrayList>(details.Item1, details.Item2, details.Item3, details.Item4, details.Item5 - 1, details.Item6);
                                    foreach (System.Data.DataRow row in _data.gdetails.Rows)
                                    {
                                        if (row[0].ToString() == dexnumber.ToString())
                                        {
                                            int count = int.Parse(row[5].ToString()) - 1;
                                            row[5] = count;
                                            break;
                                        }
                                    }
                                }

                                foreach (System.Data.DataRow row in _data.gdetails.Rows)
                                {
                                    if (row[0].ToString() == dexnumber.ToString())
                                    {
                                        int amount = int.Parse(row[6].ToString()) + 1;
                                        row[6] = amount;
                                        break;
                                    }
                                }
                            }

                            bool cont = false;
                            foreach (KeyValuePair<int, Tuple<string, string, int, int, int, ArrayList>> pair in _data.giveawayDetails)
                            {
                                if (pair.Value.Item5 != 0)
                                {
                                    cont = true;
                                    break;
                                }
                            }
                            if (!cont)
                            {
                                botresult = 1;
                                botState = (int)gtsbotstates.botexit;
                                break;
                            }

                            startIndex = 0;
                            tradeIndex = -1;
                            listlength = 0;
                            addr_PageEntry = 0;
                            foundLastPage = false;

                            if (tradeQueue)
                            {
                                botState = (int)gtsbotstates.queueempty;
                            }
                            else if (bReddit)
                            {
                                botState = (int)gtsbotstates.updatecomments;
                            }
                            else
                            {
                                botState = (int)gtsbotstates.research;
                            }
                        }
                        break;
                    case (int)gtsbotstates.quicksearch:
                        //end of list reach, press b and "search" again to reach GTS list again
                        _helper.quickbuton(_pkTable.keyB, commandtime);
                        await Task.Delay(commandtime + delaytime + 500);
                        await _helper.waittouch(160, 185);
                        await Task.Delay(2250);
                        botState = (int)gtsbotstates.findfromstart;
                        break;
                    case (int)gtsbotstates.research:
                        //press a and "search" again to reach GTS list again
                        _helper.quickbuton(_pkTable.keyA, commandtime);
                        await Task.Delay(commandtime + delaytime + 1000);
                        await _helper.waittouch(160, 185);
                        await Task.Delay(2250);
                        if (tradeQueue)
                            _data.tradeQueueRec[0] = new Tuple<string, int, int>(_data.tradeQueueRec[0].Item1, _data.tradeQueueRec[0].Item2, _data.tradeQueueRec[0].Item3 + 1);
                        botState = (int)gtsbotstates.findfromstart;
                        break;
                    case (int)gtsbotstates.botexit:
                        onChangeStatus?.Invoke("Stopped");
                        botstop = true;
                        break;
                    case (int)gtsbotstates.panic:
                        onChangeStatus?.Invoke("Recovery mode!");
                        //recover from weird state here
                        await _helper.waitNTRread(addr_currentScreen);
                        int screenID = (int)_helper.lastRead;

                        if (screenID == val_PlazaScreen)
                        {
                            await _helper.waittouch(200, 120);
                            await Task.Delay(1000);
                            await _helper.waittouch(200, 120);
                            await Task.Delay(8000);
                            correctScreen = await isCorrectWindow(val_Quit_SeekScreen);
                            if (correctScreen)
                            {
                                botState = (int)gtsbotstates.startsearch;
                                break;
                            }
                            else
                            {
                                botState = (int)gtsbotstates.botexit;
                                break;
                            }
                        }
                        else if (screenID == val_Quit_SeekScreen)
                        {
                            //press b, press where seek button would be, press b again -> guaranteed seek screen
                            _helper.quickbuton(_pkTable.keyB, commandtime);
                            await Task.Delay(commandtime + delaytime + 500);
                            await _helper.waittouch(160, 80);
                            await Task.Delay(2250);
                            _helper.quickbuton(_pkTable.keyB, commandtime);
                            await Task.Delay(commandtime + delaytime + 500);
                            botState = (int)gtsbotstates.startsearch;
                            break;
                        }
                        else if (screenID == val_WhatPkmnScreen)
                        {
                            //can only exit this one by pressing the ok button
                            waitTaskbool = _helper.waitbutton(_pkTable.keySTART);
                            if (await waitTaskbool)
                            {
                                waitTaskbool = _helper.waitbutton(_pkTable.keyA);
                                if (await waitTaskbool)
                                {
                                    botState = (int)gtsbotstates.panic;
                                    break;
                                }
                            }
                        }
                        else // if(screenID == val_SearchScreen || screenID == val_BoxScreen || screenID == val_GTSListScreen)
                        {
                            //spam b a lot and hope we get to val_quit_seekscreen like this
                            for (int i = 0; i < 5; i++)
                            {
                                _helper.quickbuton(_pkTable.keyB, commandtime);
                                await Task.Delay(commandtime + delaytime + 500);
                                await Task.Delay(1000);
                            }
                            correctScreen = await isCorrectWindow(val_Quit_SeekScreen);
                            if (correctScreen)
                            {
                                botState = (int)gtsbotstates.panic;
                                break;
                            }
                            else
                            {
                                _helper.quickbuton(_pkTable.keyA, commandtime);
                                await Task.Delay(commandtime + delaytime + 500);
                                for (int i = 0; i < 5; i++)
                                {
                                    _helper.quickbuton(_pkTable.keyB, commandtime);
                                    await Task.Delay(commandtime + delaytime + 500);
                                    await Task.Delay(1000);
                                }
                                correctScreen = await isCorrectWindow(val_Quit_SeekScreen);
                                if (correctScreen)
                                {
                                    botState = (int)gtsbotstates.panic;
                                    break;
                                }
                                else
                                {
                                    if (panicAttempts == 0)
                                    {
                                        panicAttempts++;
                                        botState = (int)gtsbotstates.panic;
                                        break;
                                    }
                                    botState = (int)gtsbotstates.botexit;
                                    break;
                                }
                            }
                        }
                        break;
                    default:
                        botresult = -1;
                        botstop = true;
                        break;
                }
            }
            if (this.serverEndPointSync != null)
            {
                syncClient.Close();
            }
            return botresult;
        }

        public void RequestStop()
        {
            botstop = true;
        }

    }
}
