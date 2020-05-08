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
        public Blip MissionBlip;
        float lastHelpMsgTime;
        public float distance = 0;
        public List<Mission> myList = new List<Mission>();

        public static UIMenu MainMenu;
        UIMenu EditMenu;
        UIMenu CreateMenu;
        UIMenuListItem mtype;
        UIMenuListItem mVehicleType;
        UIMenuCheckboxItem mUsePVehicle;
        UIMenuItem mVehicle;
        Vehicle mVehToUse = null;
        UIMenuItem mName;
        UIMenuItem mStreet;
        UIMenuItem mArea;
        UIMenuListItem mUseCase;
        UIMenuItem mStartPos;
        Vector3 mStartVector;
        UIMenuItem mEndPos;
        Vector3 mEndVector;
        UIMenuSliderItem mCount;
        UIMenuItem mAccept;
        UIMenuItem mCancel;

        MenuPool menuPool = new MenuPool();


        public static UIText myUIText;

        public Delivery()
        {
            Tick += OnTick;
            KeyDown += onKeyDown;
            Setup();
        }

        public void OnTick(object sender, EventArgs e)
        {
            if (menuPool != null) menuPool.ProcessMenus();
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
                                oldEndPos = oldMission.EndPos;
                            }
                            

                            Random rnd = new Random();
                            int index = rnd.Next(0, myList.Count); // create a random int between 0 and the total size of myList. to be used to grab a random position from myList.
                            for (int i = 0; i < 100; i++) // will try 100 times to find a new StartMission.
                            {
                                if (myList[index] != oldMission && myList[index].StartPos != oldEndPos)
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
                                if (myList[index] != CurrentMission && oldMission != myList[index] && myList[index].StartPos != oldEndPos)
                                {
                                    CurrentMission.EndPos = myList[index].StartPos; // Set our EndMission pos.  
                                    //if (DEBUG) UI.Notify("End:" + index);
                                    break;
                                }
                                else index = rnd.Next(0, myList.Count);
                            }
                            if (oldEndPos != CurrentMission.EndPos && oldMission != CurrentMission) // the deatils changed we can assume we have a new mission. 
                            {
                                MissionBlip = World.CreateBlip(CurrentMission.StartPos);
                                MissionBlip.IsShortRange = true;
                                MissionBlip.Position = CurrentMission.StartPos;
                                MissionBlip.Sprite = BlipSprite.ArmsTraffickingAir;
                                MissionBlip.Color = BlipColor.Yellow;
                                MissionBlip.Name = "Pilot Job";
                                CurrentState = MissionStates.Offered;
                            }
                        }
                        break;

                    case MissionStates.Offered:
                        distance = CurrentMission.StartPos.DistanceTo(Game.Player.Character.Position);
                        if (distance < 280)
                        {
                            World.DrawMarker(MarkerType.VerticalCylinder, CurrentMission.StartPos, Vector3.Zero, Vector3.Zero, new Vector3(4, 4, 1), Color.FromArgb(180, Color.Yellow));
                            if (distance < 20)
                            {
                                
                                if (MissionVehicle == null)
                                {
                                    if (Game.Player.Character.CurrentVehicle != null) MissionVehicle = Game.Player.Character.CurrentVehicle;
                                    else
                                    {
                                        MissionVehicle = World.CreateVehicle(VehicleHash.Buzzard2, CurrentMission.StartPos);
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
                                        MissionBlip.Position = CurrentMission.EndPos;
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
                        distance = CurrentMission.EndPos.DistanceTo(Game.Player.Character.Position);
                        if (distance < 280)
                        {
                            World.DrawMarker(MarkerType.VerticalCylinder, CurrentMission.EndPos, Vector3.Zero, Vector3.Zero, new Vector3(4, 4, 1), Color.FromArgb(180, Color.Yellow));
                            if (distance < 5 && MissionVehicle.IsStopped)
                            {
                                MissionBlip.Remove();
                                if (GeneratePed1.CurrentPedGroup == Game.Player.Character.CurrentPedGroup) GeneratePed1.LeaveGroup();
                                if (GeneratePed2.CurrentPedGroup == Game.Player.Character.CurrentPedGroup) GeneratePed2.LeaveGroup();
                                if (GeneratePed1.CurrentPedGroup != Game.Player.Character.CurrentPedGroup && GeneratePed2.CurrentPedGroup != Game.Player.Character.CurrentPedGroup)
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

            MainMenu = new UIMenu("LSDeliveries", "Menu");
            menuPool.Add(MainMenu);
            EditMenu = menuPool.AddSubMenu(MainMenu, "Edit Mission");            
            
            mName = new UIMenuItem("Name:  ");
            mStreet = new UIMenuItem("Street:  ");
            mArea = new UIMenuItem("Area:  ");

            List<dynamic> typeList = new List<dynamic>();
            foreach (MissionType t in Enum.GetValues(typeof(MissionType)))
            {
                typeList.Add(t.ToString());
            }
            mtype = new UIMenuListItem("Type:  ", typeList, 0);

            List<dynamic> vTypeList = new List<dynamic>();
            vTypeList.Add("Any");
            foreach (VehicleClass t in Enum.GetValues(typeof(VehicleClass)))
            {
                vTypeList.Add(t.ToString());
            }
            mVehicleType = new UIMenuListItem("Vehicle Type:", vTypeList, 0);
            mUsePVehicle = new UIMenuCheckboxItem("Use Player Vehicle:", true);
            mVehicle = new UIMenuItem("Vehcile:  None");
            mVehicle.Activated += VehicleSelected;

            List<dynamic> useList = new List<dynamic>();
            foreach (UseCase t in Enum.GetValues(typeof(UseCase)))
            {
                useList.Add(t.ToString());
            }
            mUseCase = new UIMenuListItem("Use:  ", useList, 3);

            mStartPos = new UIMenuItem("sPos:  ");
            mStartPos.Activated += StartPosSelected;

            mEndPos = new UIMenuItem("ePos:  ");
            mEndPos.Activated += EndPosSelected;

            mCount = new UIMenuSliderItem("Peds:  ");
            mCount.Maximum = 4;
            mCount.Multiplier = 1;            
            mCount.OnSliderChanged += CountSliderChange;

            mAccept = new UIMenuItem("Add Mission");
            mAccept.Activated += AcceptSelected;

            CreateMenu = menuPool.AddSubMenu(MainMenu, "Create Mission");

            CreateMenu.AddItem(mName);
            CreateMenu.AddItem(mStreet);
            CreateMenu.AddItem(mArea);
            CreateMenu.AddItem(mtype);
            CreateMenu.AddItem(mVehicleType);
            CreateMenu.AddItem(mUsePVehicle);
            CreateMenu.AddItem(mVehicle);
            CreateMenu.AddItem(mUseCase);
            CreateMenu.AddItem(mStartPos);
            CreateMenu.AddItem(mEndPos);
            CreateMenu.AddItem(mCount);
            CreateMenu.AddItem(mAccept);
            
            menuPool.RefreshIndex();
            
        }

        private void VehicleSelected(UIMenu sender, UIMenuItem selectedItem)
        {
            if(Game.Player.Character.IsInVehicle())
            {
                mVehToUse = Game.Player.Character.CurrentVehicle;
                selectedItem.Text = "Vehcile:  "+mVehToUse.DisplayName;
            }
            else
            {
                mVehToUse = null;
                selectedItem.Text = "Vehcile:  None";
            }
        }

        private void AcceptSelected(UIMenu sender, UIMenuItem selectedItem)
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

        private void EndPosSelected(UIMenu sender, UIMenuItem selectedItem)
        {
            mEndVector = Game.Player.Character.Position;
            selectedItem.Text = "EPos:  " + mEndVector;
            
        }

        private void StartPosSelected(UIMenu sender, UIMenuItem selectedItem)
        {
            mStartVector = Game.Player.Character.Position;
            selectedItem.Text = "SPos:  " + mStartVector;
            mStreet.Text = "Street:  "+World.GetStreetName(mStartVector);
            mArea.Text = "Area:  "+World.GetZoneName(mStartVector);
        }

        private void CountSliderChange(UIMenuSliderItem sender, int newIndex)
        {
            sender.Text = "Peds:  " + sender.Value;
        }

        void UpdateMenu()
        {
            mVehToUse = null;
            mStartVector = Game.Player.Character.Position;
            mName.Text = "Name:  ";
            mStreet.Text = "Street:  " + World.GetStreetName(mStartVector);
            mArea.Text = "Area:  " + World.GetZoneName(mStartVector);
            mtype.Text = "Type:";
            mVehicleType.Text = "Vehicle Type:";
            mUsePVehicle.Text = "Use Player Vehicle:";
            mVehicle.Text = "Vehcile:  None";
            mtype.Index = 0;
            mUseCase.Text = "UseCase:";
            mUseCase.Index = 3;
            mStartPos.Text = "SPos:  " + mStartVector;
            mEndPos.Text = "EPos:  " + Vector3.Zero;
            mCount.Value = 0;
            mCount.Text = "Peds:  " + mCount.Value;
        }

        void onKeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.O)
            {
                if (!menuPool.IsAnyMenuOpen())
                {
                    UpdateMenu();
                    MainMenu.Visible = true;
                }
            }
            if (e.KeyCode == Keys.P)
            {
                
            }
        }


        void AddMission(XElement element)
        {
            XElement root = new XElement("Mission");
            XElement type = new XElement("Type");
            XElement name = new XElement("Name");
            XElement street = new XElement("Street");
            XElement area = new XElement("Area");
            XElement vType = new XElement("Vehicle");
            XElement usePVeh = new XElement("UsePlayerVehicle");
            XElement DefaultVeh = new XElement("DefaultVehicle");
            XElement use = new XElement("UseCase");
            XElement count = new XElement("PedCount");
            XElement SposX = new XElement("SposX");
            XElement SposY = new XElement("SposY");
            XElement SposZ = new XElement("SposZ");
            XElement EposX = new XElement("EposX");
            XElement EposY = new XElement("EposY");
            XElement EposZ = new XElement("EposZ");

            type.Value = mtype.Items[mtype.Index].ToString();
            root.Add(type);

            name.Value = "";
            root.Add(name);

            street.Value = World.GetStreetName(mStartVector);
            root.Add(street);

            area.Value = World.GetZoneName(mStartVector);
            root.Add(area);

            vType.Value = mVehicleType.Items[mVehicleType.Index].ToString();
            root.Add(vType);

            usePVeh.Value = mUsePVehicle.Checked.ToString();
            root.Add(usePVeh);

            if (mVehToUse != null)
            {
                DefaultVeh.Value = mVehToUse.Model.Hash.ToString();
                root.Add(DefaultVeh);
            }
            use.Value = mUseCase.Items[mUseCase.Index].ToString();
            root.Add(use);

            count.Value = mCount.Value.ToString();
            root.Add(count);

            SposX.Value = mStartVector.X.ToString();
            SposY.Value = mStartVector.Y.ToString();
            SposZ.Value = mStartVector.Z.ToString();
            root.Add(SposX);
            root.Add(SposY);
            root.Add(SposZ);

            if (mEndVector != Vector3.Zero)
            {
                EposX.Value = mEndVector.X.ToString();
                EposY.Value = mEndVector.Y.ToString();
                EposZ.Value = mEndVector.Z.ToString();
                root.Add(EposX);
                root.Add(EposY);
                root.Add(EposZ);
            }       
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
                    string street = "Default";
                    string area = "Default";
                    string vehClass = "Any";
                    bool pVeh = true;
                    int hash = 0;
                    UseCase use = UseCase.STARTEND;
                    Vector3 sPos = Vector3.Zero;
                    Vector3 ePos = Vector3.Zero;
                    int count = 0;
                    foreach (XElement e in element.Elements())
                    {
                        if (e.Name == "Type") Enum.TryParse(e.Value, out type);
                        if (e.Name == "Name") name = e.Value;
                        if (e.Name == "Street") street = e.Value;
                        if (e.Name == "Area") area = e.Value;
                        if (e.Name == "Vehicle") vehClass = e.Value;
                        if (e.Name == "UsePlayerVehicle") bool.TryParse(e.Value, out pVeh);
                        if (e.Name == "DefaultVehicle") int.TryParse(e.Value, out hash);
                        if (e.Name == "UseCase") Enum.TryParse(e.Value, out use);
                        if (e.Name == "PedCount") int.TryParse(e.Value, out count);
                        if (e.Name == "SposX") float.TryParse(e.Value, out sPos.X);
                        if (e.Name == "SposY") float.TryParse(e.Value, out sPos.Y);
                        if (e.Name == "SposZ") float.TryParse(e.Value, out sPos.Z);
                        if (e.Name == "EposX") float.TryParse(e.Value, out ePos.X);
                        if (e.Name == "EposY") float.TryParse(e.Value, out ePos.Y);
                        if (e.Name == "EposZ") float.TryParse(e.Value, out ePos.Z);
                    }
                    if (sPos != Vector3.Zero) myList.Add(new Mission(type, name, street, area, vehClass, pVeh, hash, use, sPos, ePos,count));
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
            public string Street = "";
            public string Area = "";
            public VehicleClass vehicleClass = VehicleClass.Vans;
            public bool UsePlayerVehicle = true;
            public bool UseVehicleClass = true;
            public int VehHash = 0;
            public UseCase UseCase = UseCase.STARTEND;
            public Vector3 StartPos = Vector3.Zero;
            public Vector3 EndPos = Vector3.Zero;
            public int PedCount = 0;

            public Mission(MissionType _type, string _name, string _street, string _area, string _veh, bool _pVeh, int _hash,UseCase _use, Vector3 _sPos, Vector3 _ePos,int _count)
            {
                Type = _type;
                Name = _name;
                Street = _street;
                Area = _area;
                if(_veh != "Any")
                {                    
                    Enum.TryParse(_veh, out vehicleClass);
                }
                else
                {
                    UseVehicleClass = false;
                }
                VehHash = _hash;
                UsePlayerVehicle = _pVeh;                
                UseCase = _use;
                StartPos = _sPos;
                EndPos = _ePos;
                PedCount = _count;
            }
        }
    }
}