using OpenRA;
using OpenRA.Traits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.D2.Traits.Buildings
{
    public class D2ConretePassableInfo : ITraitInfo, ITemporaryBlockerInfo
    {
        public virtual object Create(ActorInitializer init) { return new D2ConretePassable(init.Self); }
    }

    public class D2ConretePassable : ITemporaryBlocker
    {
        public D2ConretePassable(Actor self) { }

        bool ITemporaryBlocker.IsBlocking(Actor self, CPos cell)
        {
            return false;
        }
        bool ITemporaryBlocker.CanRemoveBlockage(Actor self, Actor blocking)
        {
            return true;
        }
    }



}
