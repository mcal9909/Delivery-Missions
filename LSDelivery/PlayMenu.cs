using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Threading;
using GTA;
using GTA.Native;
using GTA.Math;
using NativeUI;
using iFruitAddon2;
using System.IO;
using Control = GTA.Control;
using static delivery.PlayMission;



namespace delivery
{
    public static class PlayMenu
    {
        static UIMenu playMenu = new UIMenu("LSDeliveries", "Select options.");
        static UIMenuItem play = new UIMenuItem("Play");
        static UIMenuCheckboxItem multiDrop = new UIMenuCheckboxItem("Multi Drop", false);   
        
        public static void SetupPlayMenu(MenuPool _pool)
        {
            playMenu.AddItem(play);
            play.Activated += Play_Activated;
            playMenu.AddItem(multiDrop);
            _pool.Add(playMenu);
        }

        private static void Play_Activated(UIMenu sender, UIMenuItem selectedItem)
        {
            GenerateMission();
        }

        public static void ShowPlayMenu()
        {
            playMenu.Visible = true;
        }
    }
}
