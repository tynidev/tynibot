using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.Bot.Utils
{
    public static class GuildIdMappings
    {

        public static readonly ImmutableDictionary<ulong, ulong> rolesChannels = new Dictionary<ulong, ulong> {
            { 902581441727197195, 904472700616073316}, //tynibot test
            { 124366291611025417,  549039583459934209}, //msft rl
            { 801598108467200031,  904856579403300905}, //tyni's server
            { 904804698484260874, 904804698484260877 } // nates server
        }.ToImmutableDictionary();


        public static readonly ImmutableDictionary<ulong, ulong> inhouseChannels = new Dictionary<ulong, ulong> {
            { 902581441727197195, 902729259905351701}, //tynibot test
            { 124366291611025417,  552350525555867658}, //msft rl
            { 801598108467200031,  904856579403300905}, //tyni's server
            { 904804698484260874, 904867794376618005 } // nates server
        }.ToImmutableDictionary();

        public static readonly ImmutableDictionary<ulong, ulong> recruitingChannels = new Dictionary<ulong, ulong> {
            { 902581441727197195, 903521423522398278}, //tynibot test
            { 124366291611025417,  541894310258278400}, //msft rl
            { 801598108467200031,  904856579403300905}, //tyni's server
            { 904804698484260874, 904867794376618005 } // nates server
        }.ToImmutableDictionary();

        public static readonly Dictionary<ulong, List<ApplicationCommandPermission>> defaultSlashCommandPermissions = new Dictionary<ulong, List<ApplicationCommandPermission>>();

        public static readonly Dictionary<ulong, List<ApplicationCommandPermission>> recruitingPermissions = new Dictionary<ulong, List<ApplicationCommandPermission>>()
        {
            { 902581441727197195, new List<ApplicationCommandPermission> { new ApplicationCommandPermission(903514452463325184, ApplicationCommandPermissionTarget.Role, true) } }, // tynibot test
            { 124366291611025417, new List<ApplicationCommandPermission> { new ApplicationCommandPermission(598569589512863764, ApplicationCommandPermissionTarget.Role, true) } }, // msft rl
            { 801598108467200031, new List<ApplicationCommandPermission>() }, // tyni's server
            { 904804698484260874, new List<ApplicationCommandPermission> { new ApplicationCommandPermission(904867602571100220, ApplicationCommandPermissionTarget.Role, true) } }, // nate server
        };

        public static readonly Dictionary<ulong, List<ApplicationCommandPermission>> adminRecruitingPermissions = new Dictionary<ulong, List<ApplicationCommandPermission>>()
        {
            { 902581441727197195, new List<ApplicationCommandPermission> { new ApplicationCommandPermission(903514452463325184, ApplicationCommandPermissionTarget.Role, true) } }, // tynibot test
            { 124366291611025417, new List<ApplicationCommandPermission> { new ApplicationCommandPermission(469941381075435523, ApplicationCommandPermissionTarget.Role, true), new ApplicationCommandPermission(480419333995233280, ApplicationCommandPermissionTarget.Role, true) } }, // msft rl
            { 801598108467200031, new List<ApplicationCommandPermission>() }, // tyni's server
            { 904804698484260874, new List<ApplicationCommandPermission> { new ApplicationCommandPermission(904867602571100220, ApplicationCommandPermissionTarget.Role, true) } }, // nate server
        };
    }
}
