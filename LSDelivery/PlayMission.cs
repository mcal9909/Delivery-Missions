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
using static delivery.Delivery;

namespace delivery
{
    public static class PlayMission
    {
        public static void GenerateMission()
        {
            CurrentMission.active = false;
            Mission oldMission = null;
            Vector3 oldEndPos = Vector3.Zero;
            if (CurrentMission != null)
            {
                oldMission = CurrentMission; // storing the old mission details
                oldEndPos = oldMission.GetNewEndPos();
            }
            Random rnd = new Random();
            int index = rnd.Next(0, myList.Count); // create a random int between 0 and the total size of myList. to be used to grab a random position from myList.
            for (int i = 0; i < 100; i++) // will try 100 times to find a new StartMission.
            {
                if (myList[index] != oldMission && myList[index].GetStartPos() != oldEndPos)
                {
                    CurrentMission = myList[index];
                    break;
                }
                else index = rnd.Next(0, myList.Count);
            }
            index = rnd.Next(0, myList.Count);
            for (int i = 0; i < 100; i++)
            {
                if (myList[index] != CurrentMission && oldMission != myList[index] && myList[index].GetStartPos() != oldEndPos)
                {
                    CurrentMission.SetNewEndPos(myList[index].GetStartPos());
                    break;
                }
                else
                {
                    index = rnd.Next(0, myList.Count);
                }
            }
            if (oldEndPos != CurrentMission.GetNewEndPos() && oldMission != CurrentMission) // the deatils changed we can assume we have a new mission. 
            {
                CurrentMission.active = true;
            }
        }
    }
}
