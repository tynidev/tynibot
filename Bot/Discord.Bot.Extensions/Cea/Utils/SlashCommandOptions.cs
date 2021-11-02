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
        None = 0,
        Team = 1,
        Org = 2,

        TeamsFilteringSupport = Team | Org,
    }
}
