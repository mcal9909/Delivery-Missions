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

namespace delivery
{
    public class Delivery : Script
    {
        bool DEBUG = true;

        enum MissionStates { None, Offered, Start, Middle, End, Finish, Stop }
        public enum MissionType { Default, BoatMission, PlaneMission, CarMission, ArticulatedMission, TruckMisson };
        public enum UseCase { STARTEND, START, END };
        static MissionStates CurrentState = MissionStates.None;

        public Ped GeneratePed1;
        public Ped GeneratePed2;
        public Vehicle MissionVehicle;
        public Mission CurrentMission = null;
        public Mission EndMission = null;
        public Blip MissionBlip;
        float lastHelpMsgTime;
        public float distance = 0;
        public List<Mission> myList = new List<Mission>();
        PedGroup playerGroup;

        public static UIText myUIText;

        public Delivery()
        {
            Tick += OnTick;
            KeyDown += onKeyDown;
            Setup();
        }

        public void OnTick(object sender, EventArgs e)
        {

            if (DEBUG)
            {

                myUIText = new UIText("" + CurrentState + " Distance:" + distance, new Point(10, 10), 0.4f, Color.WhiteSmoke, 0, false);
                myUIText.Draw();
            }

            if (Game.Player.IsPlaying)
            {
                switch (CurrentState)
                {
                    case MissionStates.None: // Active When No Mission is present.
                        if (myList.Count > 0)
                        {
                            Mission oldMission = null;
                            Vector3 oldEndPos = Vector3.Zero;
                            if (CurrentMission != null)
                            {
                                oldMission = CurrentMission; // storing the old mission details
                                oldEndPos = oldMission.endPos;
                            }
                            

                            Random rnd = new Random();
                            int index = rnd.Next(0, myList.Count); // create a random int between 0 and the total size of myList. to be used to grab a random position from myList.
                            for (int i = 0; i < 100; i++) // will try 100 times to find a new StartMission.
                            {
                                if (myList[index] != oldMission || myList[index].startPos != oldEndPos)
                                {
                                    CurrentMission = myList[index];   // if the rnd Index in myList is not a current start or end position set our StatMission pos;
                                    //if (DEBUG) UI.Notify("Start:" + index);
                                    break;
                                }
                                else index = rnd.Next(0, myList.Count); // else pick a new Index and try again.
                            }
                            index = rnd.Next(0, myList.Count);
                            for (int i = 0; i < 100; i++)   // all the same as above but its looking for a new EndMission.
                            {
                                if (myList[index] != CurrentMission || oldMission != myList[index] || myList[index].startPos != oldEndPos)
                                {
                                    CurrentMission.endPos = myList[index].startPos; // Set our EndMission pos.  
                                    //if (DEBUG) UI.Notify("End:" + index);
                                    break;
                                }
                                else index = rnd.Next(0, myList.Count);
                            }
                            if (oldEndPos != CurrentMission.endPos && oldMission != CurrentMission) // the deatils changed we can assume we have a new mission. 
                            {
                                MissionBlip = World.CreateBlip(CurrentMission.startPos);
                                MissionBlip.IsShortRange = true;
                                MissionBlip.Position = CurrentMission.startPos;
                                MissionBlip.Sprite = BlipSprite.ArmsTraffickingAir;
                                MissionBlip.Color = BlipColor.Yellow;
                                MissionBlip.Name = "Pilot Job";
                                CurrentState = MissionStates.Offered;
                            }
                        }
                        break;

                    case MissionStates.Offered:
                        distance = CurrentMission.startPos.DistanceTo(Game.Player.Character.Position);
                        if (distance < 280)
                        {
                            World.DrawMarker(MarkerType.VerticalCylinder, CurrentMission.startPos, Vector3.Zero, Vector3.Zero, new Vector3(4, 4, 1), Color.FromArgb(180, Color.Yellow));
                            if (distance < 20)
                            {
                                
                                if (MissionVehicle == null)
                                {
                                    if (Game.Player.Character.CurrentVehicle != null) MissionVehicle = Game.Player.Character.CurrentVehicle;
                                    else
                                    {
                                        MissionVehicle = World.CreateVehicle(VehicleHash.Buzzard2, CurrentMission.startPos);
                                    }
                                    
                                }
                                else if (MissionVehicle != null && MissionVehicle.Driver != null && MissionVehicle.Driver.IsPlayer && MissionVehicle.IsStopped)
                                {
                                    GeneratePed1 = World.CreatePed(PedHash.Michael, Game.Player.Character.GetOffsetInWorldCoords(new Vector3(0, 5, 0)));
                                    GeneratePed1.BlockPermanentEvents = true;
                                    GeneratePed1.AlwaysKeepTask = true;
                                    GeneratePed2 = World.CreatePed(PedHash.Andreas, Game.Player.Character.GetOffsetInWorldCoords(new Vector3(0, 5, 0)));
                                    GeneratePed2.BlockPermanentEvents = true;
                                    GeneratePed2.AlwaysKeepTask = true;
                                    if (GeneratePed1 != null && GeneratePed2 != null)
                                    {
                                        CurrentState = MissionStates.Start; //Vehicle and Ped is spawned, and player is in vehicle. Change to Start state.
                                        MissionBlip.Position = CurrentMission.endPos;
                                        MissionBlip.Sprite = BlipSprite.Standard;
                                        MissionBlip.Color = BlipColor.Yellow;
                                        MissionBlip.Name = "Pilot Job Delivery";
                                        MissionBlip.ShowRoute = true;
                                    }
                                }
                            }
                        }
                        break;


                    case MissionStates.Start:
                        if (MissionVehicle.Driver != null && MissionVehicle.Driver.IsPlayer)
                        {
                            if (GeneratePed1.CurrentPedGroup != Game.Player.Character.CurrentPedGroup)
                            {
                                Game.Player.Character.CurrentPedGroup.Add(GeneratePed1, false);
                            }
                            if (GeneratePed2.CurrentPedGroup != Game.Player.Character.CurrentPedGroup)
                            {
                                Game.Player.Character.CurrentPedGroup.Add(GeneratePed2, false);
                            }
                            if (GeneratePed2.CurrentPedGroup == Game.Player.Character.CurrentPedGroup && GeneratePed2.CurrentPedGroup == Game.Player.Character.CurrentPedGroup)
                            {
                                GeneratePed1.Task.EnterVehicle(MissionVehicle, VehicleSeat.Passenger);
                                GeneratePed2.Task.EnterVehicle(MissionVehicle, VehicleSeat.Passenger);
                                CurrentState = MissionStates.Middle; //Player is in the generated vehicle, and in the drivers seat, tell ped to enter vehicle and change to middle mission state.
                            }
                        }
                        break;

                    case MissionStates.Middle:
                        if (GeneratePed1.CurrentVehicle == MissionVehicle && GeneratePed2.CurrentVehicle == MissionVehicle)
                        {
                            CurrentState = MissionStates.End; // Peds are in the vehicle, change to end mission state.
                        }
                        break;

                    case MissionStates.End:
                        distance = CurrentMission.endPos.DistanceTo(Game.Player.Character.Position);
                        if (distance < 280)
                        {
                            World.DrawMarker(MarkerType.VerticalCylinder, CurrentMission.endPos, Vector3.Zero, Vector3.Zero, new Vector3(4, 4, 1), Color.FromArgb(180, Color.Yellow));
                            if (distance < 5 && MissionVehicle.IsStopped)
                            {
                                MissionBlip.Remove();
                                if (GeneratePed1.CurrentPedGroup == playerGroup) playerGroup.Remove(GeneratePed1);
                                if (GeneratePed2.CurrentPedGroup == playerGroup) playerGroup.Remove(GeneratePed2);
                                if (GeneratePed1.CurrentPedGroup != playerGroup && GeneratePed2.CurrentPedGroup != playerGroup)
                                {
                                    GeneratePed1.Task.WanderAround();
                                    GeneratePed2.Task.WanderAround();
                                    GeneratePed1.MarkAsNoLongerNeeded();
                                    GeneratePed2.MarkAsNoLongerNeeded();
                                    CurrentState = MissionStates.Finish; // Player has arrived at the location, and vehicle is stopped. Tell ped to get out and change to finish state.
                                }
                            }
                        }
                        break;

                    case MissionStates.Finish:
                        if (!GeneratePed1.IsInVehicle() && !GeneratePed2.IsInVehicle())
                        {
                            Game.Player.Money += 2000;
                            CurrentState = MissionStates.None; // Ped has gotten out of vehicle, give player money and set us to having no mission.
                        }
                        break;

                }
            }
        }


        void Setup()
        {
            XDocument document = XDocument.Load("scripts/Delivery/Locations.xml");
            ReadMissions(document);
        }

        void onKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.P)
            {
                XDocument document;
                if (File.Exists("scripts/Delivery/Locations.xml"))
                {
                    document = XDocument.Load("scripts/Delivery/Locations.xml");
                }
                else
                {
                    document = new XDocument();
                    XElement XmlMission = new XElement("Missions");
                    document.Add(XmlMission);
                    document.Save("scripts/Delivery/Locations.xml");
                }
                foreach (XElement xElement in document.Descendants())
                {
                    if (xElement.Name == "Missions")
                    {
                        AddMission(xElement);
                        Wait(10);
                        document.Save("scripts/Delivery/Locations.xml");
                        ReadMissions(document);
                    }
                }
            }
        }


        void AddMission(XElement element)
        {
            XElement root = new XElement("Mission");
            XElement type = new XElement("Type");
            XElement name = new XElement("Name");
            XElement use = new XElement("UseCase");
            XElement count = new XElement("PedCount");
            XElement posX = new XElement("posX");
            XElement posY = new XElement("posY");
            XElement posZ = new XElement("posZ");

            type.Value = MissionType.Default.ToString();
            name.Value = "Default";
            use.Value = UseCase.STARTEND.ToString();
            count.Value = 2.ToString();
            posX.Value = Game.Player.Character.Position.X.ToString();
            posY.Value = Game.Player.Character.Position.Y.ToString();
            posZ.Value = Game.Player.Character.Position.Z.ToString();
            root.Add(type);
            root.Add(name);
            root.Add(use);
            root.Add(count);
            root.Add(posX);
            root.Add(posY);
            root.Add(posZ);
            element.Add(root);
        }

        void ReadMissions(XDocument doc)
        {            
            myList.Clear();
            foreach (XElement element in doc.Descendants())
            {
                if (element.Name == "Mission")
                {
                    MissionType type = MissionType.Default;
                    string name = "Default";
                    UseCase use = UseCase.STARTEND;
                    Vector3 PosToAdd = Vector3.Zero;
                    int count = 0;

                    foreach (XElement e in element.Elements())
                    {
                        if (e.Name == "Type") Enum.TryParse(e.Value, out type);
                        if (e.Name == "Name") name = e.Value;
                        if (e.Name == "UseCase") Enum.TryParse(e.Value, out use);
                        if (e.Name == "PedCount") int.TryParse(e.Value, out count);
                        if (e.Name == "posX") float.TryParse(e.Value, out PosToAdd.X);
                        if (e.Name == "posY") float.TryParse(e.Value, out PosToAdd.Y);
                        if (e.Name == "posZ") float.TryParse(e.Value, out PosToAdd.Z);
                    }
                    if (PosToAdd != Vector3.Zero) myList.Add(new Mission(type, name, use, PosToAdd, count));
                }
            }
            if (DEBUG) UI.Notify("Missions = " + myList.Count);
        }


        void DisplayHelpTextThisFrame(string text)
        {
            InputArgument[] arguments = new InputArgument[] { "STRING" };
            Function.Call(Hash._0x8509B634FBE7DA11, arguments);
            InputArgument[] argumentArray2 = new InputArgument[] { text };
            Function.Call(Hash._0x6C188BE134E074AA, argumentArray2);
            InputArgument[] argumentArray3 = new InputArgument[] { 0, 0, 1, -1 };
            Function.Call(Hash._0x238FFE5C7B0498A6, argumentArray3);
        }

        public class Mission
        {            
            public MissionType Type = MissionType.Default;
            public string Name = "Default";            
            public UseCase useCase = UseCase.STARTEND;
            public Vector3 startPos = Vector3.Zero;
            public Vector3 endPos = Vector3.Zero;
            public int PedCount = 0;

            public Mission(MissionType _type, string _name, UseCase _use, Vector3 _sPos, int _count)
            {
                Type = _type;
                Name = _name;
                useCase = _use;
                startPos = _sPos;
                PedCount = _count;
            }
        }
    }
}