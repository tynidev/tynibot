using Azure;
using Azure.Data.Tables;
using Discord;
using Discord.Bot.Utils;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TyniBot.Recruiting
{
    public class Team : ValueWithEtag
    {
        public static string TableName = "teams";
        public static string FreeAgentTeam = "Free_Agents";

        public ulong MsgId { get; set; } = 0;
        public string Name { get; set; } = null;
        public bool LookingForPlayers { get; set; } = false;
        public ulong CategoryChannelId { get; set; } = 0;
        public ulong RoleId { get; set; } = 0;

        public Player Captain = null;
        public List<Player> Players { get; set; } = new List<Player>();

        #region Parsing and Formatting
        public static Team ParseTeam(ulong id, string msg)
        {
            Team team = new Team();
            team.MsgId = id;
            team.Name = msg.Substring(4, msg.IndexOf("**__") - 4);

            StringReader strReader = new StringReader(msg);
            var line = strReader.ReadLine();

            if (string.Equals(line.Substring(msg.IndexOf("**__") + 4).Trim(), "(looking for players)", StringComparison.OrdinalIgnoreCase))
            {
                team.LookingForPlayers = true;
            }

            line = strReader.ReadLine();
            if (team.Name != "Free_Agents")
            {
                string captainName = line.Substring("Captain:".Length).Trim();
                team.Captain = new Player() { DiscordUser = captainName };

                line = strReader.ReadLine();
            }


            while (line != null)
            {
                try
                {
                    team.Players.Add(Player.ParsePlayer(line));
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error parsing player {line}");
                }

                line = strReader.ReadLine();
            }

            team.Players.Sort((p1, p2) => p1.DiscordUser.CompareTo(p2.DiscordUser));

            return team;
        }

        public string ToMessage()
        {
            string msg = $"__**{this.Name}**__ { (this.LookingForPlayers ? "(looking for players)" : "")}\n";

            if (this.Name != "Free_Agents")
            {
                msg += $"Captain: {(this.Captain != null ? $"{this.Captain.DiscordUser}" : " ")}\n";
            }
            Players.Sort((p1, p2) => p1.DiscordUser.CompareTo(p2.DiscordUser));

            foreach (var player in Players)
            {
                msg += player.ToMessage();
            }

            return msg;
        }
        #endregion

        #region Instance Methods
        public Player FindPlayer(SocketGuildUser guildUser)
        {
            var exists = Players.Where((p) => p.DiscordId == guildUser.Id || (p.DiscordId== 0 && string.Equals(p.DiscordUser, guildUser.Nickname ?? guildUser.Username, StringComparison.OrdinalIgnoreCase)));
            if (exists.Any())
            {
                return exists.First();
            }
            return null;
        }

        public async Task RemovePlayerAsync(Player player, SocketGuildUser guildUser)
        {
            // If player was captain of old team remove that teams captain
            if (this.Captain?.DiscordId == player.DiscordId || (this.Captain?.DiscordId == 0 && this.Captain?.DiscordUser == player.DiscordUser))
                this.Captain = null;

            // Move Player
            this.Players.Remove(player);

            if (RoleId != 0)
            {
                await guildUser.RemoveRoleAsync(RoleId);
            }
        }
        
        public void AddPlayer(Player player)
        {
            Player? existingPlayer = this.Players.Find((p) => string.Equals(p.DiscordUser, player.DiscordUser, StringComparison.OrdinalIgnoreCase));

            if (existingPlayer != default(Player))
            {
                existingPlayer.Platform = player.Platform;
                existingPlayer.PlatformId = player.PlatformId;
            }
            else
            {
                this.Players.Add(player);
            }
        }

        public async Task ConfigureTeamAsync(DiscordSocketClient client, Guild guild, ISocketMessageChannel recruitingChannel)
        {
            if (Players.Any())
            {
                if (MsgId == 0)
                {
                    MsgId = (await recruitingChannel.SendMessageAsync(ToMessage())).Id;
                }
                else
                {
                    await recruitingChannel.ModifyMessageAsync(MsgId, (message) => message.Content = ToMessage());
                }

                if (CategoryChannelId == 0)
                {
                    await ConfigureNewTeamAsync(client, guild, recruitingChannel);
                }

                await AssignRolesToPlayersAsync((recruitingChannel as SocketTextChannel).Guild);
            }
            else
            {
                await CleanupDeletedTeamAsync(client, guild, recruitingChannel);
            }
        }

        private async Task AssignRolesToPlayersAsync(SocketGuild guild)
        {
            foreach (Player player in Players)
            {
                if (player.DiscordId == 0)
                {
                    player.DiscordId = guild.Users.Where(user => user.Username == player.DiscordUser || user.Nickname == player.DiscordUser).First().Id;
                }

                await guild.GetUser(player.DiscordId).AddRoleAsync(RoleId);
            }
        }

        public async Task CleanupDeletedTeamAsync(DiscordSocketClient client, Guild guild, ISocketMessageChannel recruitingChannel, bool seasonRefresh = false)
        {
            if (string.Equals(Name, FreeAgentTeam, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (CategoryChannelId != 0)
            {
                SocketGuild socketGuild = client.GetGuild(guild.Id);

                var categoryChannel = socketGuild.GetCategoryChannel(CategoryChannelId);
                if (categoryChannel != null)
                {
                    if (guild.Id != 124366291611025417 || seasonRefresh == false) // dont delete the channels in the msft discord for now
                    {
                        foreach (var channel in categoryChannel.Channels)
                        {
                            await channel.DeleteAsync();
                        }

                        await categoryChannel.DeleteAsync();

                        var role = socketGuild.GetRole(RoleId);

                        if (role != null)
                        {
                            await role.DeleteAsync();
                        }
                    }
                    else
                    {
                        await categoryChannel.ModifyAsync(props => props.Position = socketGuild.Channels.Count - 1);

                        var role = socketGuild.GetRole(RoleId);
                        if (role != null)
                        {
                            await role?.ModifyAsync(props =>
                            {
                                props.Name = $"{props.Name} {DateTime.UtcNow.Year % 2000.0}H{(DateTime.UtcNow.Month <= 6 ? "1" : "2")}";
                                props.Hoist = false;
                                props.Mentionable = false;
                            });
                        }
                    }
                }
            }

            await recruitingChannel.DeleteMessageAsync(MsgId);
        }


        private async Task ConfigureNewTeamAsync(DiscordSocketClient client, Guild guild, ISocketMessageChannel recruitingChannel)
        {
            if (string.Equals(Name, FreeAgentTeam, StringComparison.OrdinalIgnoreCase)){
                return;
            }

            SocketGuild socketGuild = client.GetGuild(guild.Id);

            var roles = socketGuild.Roles.Where(role => string.Equals(role.Name, Name, StringComparison.OrdinalIgnoreCase));
            var roleExisted = false;
            if (roles.Any())
            {
                RoleId = roles.First().Id;
            }
            else
            {
                var role = await socketGuild.CreateRoleAsync(Name, isMentionable: true, isHoisted: true);
                RoleId = role.Id;
            }

            var categoryChannels = socketGuild.CategoryChannels.Where(channel => string.Equals(channel.Name, Name, StringComparison.OrdinalIgnoreCase));

            if (categoryChannels.Any())
            {
                CategoryChannelId = categoryChannels.First().Id;
                if (!roleExisted)
                {
                    var channels = categoryChannels.First().Channels.ToList();
                    await CreateChannelRoles(client, socketGuild, channels[0] as SocketTextChannel, channels[1] as SocketTextChannel, channels[2] as SocketVoiceChannel);
                }
            }
            else {
                try
                {
                    var restCategoryChannel = await socketGuild.CreateCategoryChannelAsync(Name);

                    CategoryChannelId = restCategoryChannel.Id;
                    var teamChannel = await socketGuild.CreateTextChannelAsync(Name.Replace(' ', '-').Replace(".", ""), (props) => { props.Position = 0; props.CategoryId = CategoryChannelId; });
                    var replaysChannel = await socketGuild.CreateTextChannelAsync("Replays", (props) => { props.Position = 1; props.CategoryId = CategoryChannelId; });
                    var voiceChannel = await socketGuild.CreateVoiceChannelAsync("Team Voice", (props) => { props.Position = 2; props.CategoryId = CategoryChannelId; });
                    
                    var channel = socketGuild.GetCategoryChannel(restCategoryChannel.Id);

                    await CreateChannelRoles(client, socketGuild, teamChannel, replaysChannel, voiceChannel);
                }
                catch (Exception ex)
                {
                    // if it fails to create the channel's, it's not a big deal. the bot permissions probably weren't set correctly;
                    Console.WriteLine(ex);
                }
            }
        }

        private async Task CreateChannelRoles(DiscordSocketClient client, SocketGuild socketGuild, ITextChannel teamChannel, ITextChannel replayChannel, IVoiceChannel voiceChannel)
        {
            await teamChannel.ModifyAsync(func =>
                func.PermissionOverwrites = new List<Overwrite>
                {
                    //new Overwrite(socketGuild.GetUser(client.CurrentUser.Id).Roles.Where(role => string.Equals(role.Name, client.CurrentUser.Username, StringComparison.OrdinalIgnoreCase)).FirstOrDefault().Id, PermissionTarget.Role, new OverwritePermissions(manageChannel: PermValue.Allow)),
                    new Overwrite(client.CurrentUser.Id, PermissionTarget.User, new OverwritePermissions(manageChannel: PermValue.Allow, viewChannel: PermValue.Allow)),
                    new Overwrite(RoleId, PermissionTarget.Role, new OverwritePermissions(viewChannel: PermValue.Allow)),
                    new Overwrite(socketGuild.EveryoneRole.Id, PermissionTarget.Role, new OverwritePermissions(viewChannel: PermValue.Deny)),
                });

            await replayChannel.ModifyAsync(func =>
                func.PermissionOverwrites = new List<Overwrite>
                {
                    new Overwrite(client.CurrentUser.Id, PermissionTarget.User, new OverwritePermissions(manageChannel: PermValue.Allow, sendMessages: PermValue.Allow)),
                    new Overwrite(RoleId, PermissionTarget.Role, new OverwritePermissions(sendMessages: PermValue.Allow)),
                    new Overwrite(socketGuild.EveryoneRole.Id, PermissionTarget.Role,new OverwritePermissions(sendMessages: PermValue.Deny)),
                });

            await voiceChannel.ModifyAsync(func =>
                func.PermissionOverwrites = new List<Overwrite> 
                {
                    new Overwrite(client.CurrentUser.Id, PermissionTarget.User, new OverwritePermissions(manageChannel: PermValue.Allow, connect: PermValue.Allow)),
                    new Overwrite(RoleId, PermissionTarget.Role, new OverwritePermissions(connect: PermValue.Allow)),
                    new Overwrite(socketGuild.EveryoneRole.Id, PermissionTarget.Role,new OverwritePermissions(connect: PermValue.Deny)),
                });

        }
        #endregion

        #region Static Methods
        public static (Team, Player) FindPlayer(IEnumerable<Team> teams, SocketGuildUser discordUser)
        {
            foreach (var team in teams)
            {
                var player = team.FindPlayer(discordUser);
                if (player != null)
                {
                    return (team, player);
                }
            }
            return (null, null);
        }

        public static Team FindTeam(IEnumerable<Team> teams, string teamName)
        {
            foreach (var team in teams)
            {
                if (string.Equals(team.Name, teamName, StringComparison.OrdinalIgnoreCase))
                    return team;
            }
            return null;
        }

        public static Team AddPlayer(List<Team> teams, string teamName, Player player, bool captain = false)
        {
            var team = Team.FindTeam(teams, teamName);

            // Not found? -> Add Free Agent team
            if (team == null)
            {
                team = new Team()
                {
                    Name = teamName,
                    Players = new List<Player>()
                };

                teams.Add(team);
            }

            team.AddPlayer(player);
            
            if (captain)
            {
                team.Captain = player;
            }

            return team;
        }
        #endregion
    }
}
