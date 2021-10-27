using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace TyniBot
{
    public class Version : ModuleBase
    {
        [Command("version"), Summary("Command to see what version of TyniBot is running.")]
        public async Task VersionCommand()
        {
            var version = FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
            await Context.Channel.SendMessageAsync($"{ToDate(version.ProductBuildPart, version.ProductPrivatePart).ToString()} UTC");
        }

        private (int, int) ToVersion(DateTime date)
        {
            var partBuild = (int)(date.Subtract(new DateTime(2000, 1, 1))).TotalDays;
            var partPrivate = (int)(date.TimeOfDay.TotalSeconds / 2);
            return (partBuild, partPrivate);
        }

        private DateTime ToDate(int days, int seconds)
        {
            var date = new DateTime(2000, 1, 1);
            date = date.AddDays(days);
            date = date.AddSeconds(seconds * 2);
            return date;
        }
    }
}