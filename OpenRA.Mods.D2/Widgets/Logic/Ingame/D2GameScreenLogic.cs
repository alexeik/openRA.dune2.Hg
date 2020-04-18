using OpenRA.Mods.Common.Widgets;
using OpenRA.Widgets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.D2.Widgets.Logic.Ingame
{
    public class D2GameScreenLogic :ChromeLogic
    {
        bool vismentata = false;
        BackgroundWidget mentatabkg;

        [ObjectCreator.UseCtor]
        public D2GameScreenLogic(Widget widget, World world)
        {
            var mainMenu = widget.Get<ButtonWidget>("mentat");
            mainMenu.OnClick = OpenMentat;
            mentatabkg = widget.Get<BackgroundWidget>("mentatabkg");
            mentatabkg.IsVisible = () => vismentata;
        }

        public void OpenMentat()
        {
            vismentata = !vismentata;
            mentatabkg.IsVisible = () => vismentata;
        }
    }
}
