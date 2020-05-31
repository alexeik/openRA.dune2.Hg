using OpenRA.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.D2.Widgets
{
    public class CampaignData
    {
        public string CampaignName;
        public string CampaignDesc;
        public float3 CampaignForFractionCode;
        public string CampaignForFractionName;
        public string Command;
        public string CommandArgs;
        public List<CampaignPlayers> Players;
        public List<CampaignLevel> Levels;
        public int CurrentLevel;
    }
    public class CampaignPlayers
    {
        public string Name;
        public float3 Color;
        public float3 RegionColor;
    }
    public class CampaignLevel
    {
        public int Num;
        public List<LevelPlayers> PlayersRegions;
        public string Description;
        public Dictionary<float3, string> PickRegions =new Dictionary<float3, string>();
        public Brief Brief;

    }
    public class Brief
    {
        public string Background;
        public string SubBkgSequence;
        public string SubBkgSequenceGroup;
    }
    public class ReignRegion
    {
        public float3 Color;
    }
    public class LevelPlayers
    {
        public List<ReignRegion> ReignRegions=new List<ReignRegion>();
        public string Name;
    }
}
