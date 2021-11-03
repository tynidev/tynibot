using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.Cea
{
    [Flags]
    enum SlashCommandOptions
    {
        none = 0,
        team = 1,
        org = 2,
        player = 4,

        TeamsFilteringSupport = team | org | player,
    }
}
