﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

using PixelBot.Json;
using PixelBot.Events;
using PixelBot.Commands.Dev;
using PixelBot.Commands.Fun;
using PixelBot.Commands.Main;

namespace PixelBot
{
    class Program
    {
        public static DiscordSocketClient _client;
        static void Main() => new Program().MainAsync().GetAwaiter().GetResult();
        public async Task MainAsync()
        {
            _client = new DiscordSocketClient();
            _client.MessageReceived += EventHandler;
            _client.MessageReceived += CommandHandler;
            _client.Log += Log;
            var token = BaseConfig.GetConfig().Token;
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();
            await Task.Delay(-1);
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private Task EventHandler(SocketMessage message)
        {
            if (message.Author.IsBot)
                return Task.CompletedTask;
            Xp.DoEvent(message);

            return Task.CompletedTask;
        }
        private Task CommandHandler(SocketMessage message)
        {
            if (!message.Content.StartsWith(BaseConfig.GetConfig().Prefix) || message.Author.IsBot)
                return Task.CompletedTask;
            string firstWord = message.Content.Split()[0];
            string command = firstWord.Substring(1, firstWord.Length - 1).ToLower();

            // Dev
            if (Test.Aliases().Contains(command) && Test.HasPerm(message))
                Test.DoCommand(message);
            // Fun
            if (Minesweeper.Aliases().Contains(command))
                Minesweeper.DoCommand(message);
            // Main
            if (Rank.Aliases().Contains(command))
                Rank.DoCommand(message);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Meghatározható típusú logolás a terminálba és a BaseConfigban beállított szobákba.
        /// </summary>
        /// <param name="mode">command, rankup</param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static async Task Log(string mode, SocketMessage message)
        {
            Console.Write(DateTime.Now.ToString("yyyy.MM.dd. HH:mm:ss") + " ");
            string output = "";
            switch (mode)
            {
                case "command":
                    output = $"Command run - {message.Author.Username}#{message.Author.Discriminator} in #{message.Channel}: {message.Content}";
                    break;
                case "rankup":
                    var members = Member.PullData();
                    output = $"Event - {message.Author.Username}#{message.Author.Discriminator} ranked up: {members[members.IndexOf(members.Find(x => x.ID == message.Author.Id))].Rank + 1}";
                    break;

                default:
                    return;
            }
            foreach (var id in BaseConfig.GetConfig().Channels.BotTerminal)
                await ((IMessageChannel)_client.GetChannel(id)).SendMessageAsync(output);
            Console.WriteLine(output);
        }
        /// <summary>
        /// ID, ping, név alapján megkeresi a keresett felhasználót és visszaadja az ID-jét.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inputName"></param>
        /// <returns></returns>
        public static ulong GetUserId(SocketMessage message, string inputName)
        {
            ulong id = 0;
            try { id = ulong.Parse(inputName); }
            catch (Exception)
            {
                try { id = message.MentionedUsers.First().Id; }
                catch (Exception)
                {
                    try
                    {
                        var users = ((SocketGuildChannel)message.Channel).Guild.Users;
                        string[] userStr = inputName.Split('#');
                        if (userStr.Length > 2)
                        {
                            message.Channel.SendMessageAsync("❌ Unknown user!");
                            return 0;
                        }
                        bool userMissing = true;
                        bool multipleFound = false;
                        if (userStr.Length == 2)
                        {
                            foreach (var user in users)
                            {
                                if (user.Username == userStr.First() && user.Discriminator == userStr.Last())
                                {
                                    id = user.Id;
                                    userMissing = false;
                                    break;
                                }
                            }
                        }
                        if (userMissing && userStr.Length == 2)
                        {
                            int usersFound = 0;
                            foreach (var user in users)
                            {
                                if (user.Username.ToLower() == userStr.First().ToLower() && user.Discriminator == userStr.Last())
                                {
                                    id = user.Id;
                                    usersFound++;
                                }
                            }
                            if (usersFound == 1)
                                userMissing = false;
                            else if (usersFound > 1)
                                multipleFound = true;
                        }
                        if (userStr.Length == 1)
                        {
                            int usersFound = 0;
                            foreach (var user in users)
                            {
                                if (user.Username == userStr.First())
                                {
                                    id = user.Id;
                                    usersFound++;
                                }
                            }
                            if (usersFound == 1)
                                userMissing = false;
                            else if (usersFound > 1)
                                multipleFound = true;
                        }
                        if (userMissing && userStr.Length == 1)
                        {
                            int usersFound = 0;
                            foreach (var user in users)
                            {
                                if (user.Username.ToLower() == userStr.First().ToLower())
                                {
                                    id = user.Id;
                                    usersFound++;
                                }
                            }
                            if (usersFound == 1)
                                userMissing = false;
                            else if (usersFound > 1)
                                multipleFound = true;
                        }

                        if (userMissing)
                        {
                            if (multipleFound)
                                message.Channel.SendMessageAsync("❌ Multiple users found!");
                            else
                                message.Channel.SendMessageAsync("❌ Unknown user!");
                            return 0;
                        }
                    }
                    catch (Exception)
                    {
                        message.Channel.SendMessageAsync("❌ Unknown user!");
                        return 0;
                    }
                }
            }
            return id;
        }
    }
}
