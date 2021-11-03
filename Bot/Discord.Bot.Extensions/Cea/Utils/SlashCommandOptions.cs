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
        TeamName = 1,
        OrgName = 2,

        TeamsFilteringSupport = TeamName | OrgName,
    }
}
