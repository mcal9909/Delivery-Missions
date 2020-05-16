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
using static delivery.PlayMenu;
using static delivery.PlayMission;

namespace delivery
{
    public class Delivery : Script
    {
        bool DEBUG = true;

        enum MissionStates { None, Offered, Start, Middle, End, Finish, Stop, Fail }
        public enum MissionType { Default, Passenger, Cargo };
        public enum UseCase { STARTEND, START, END };
        static MissionStates CurrentState = MissionStates.None;

        bool DrawStartPos = false;
        public List<Ped> MissionPeds = new List<Ped>();
        public Vehicle MissionVehicle;
        public static Mission CurrentMission = null;
        public Blip MissionBlip;
        float lastHelpMsgTime;
        public float distance = 0;
        public static List<Mission> myList = new List<Mission>();
        bool InputWidowOpen = false;

        public static UIMenu MainMenu;
        UIMenu EditMenu;
        UIMenu CreateMenu;
        UIMenuListItem mtype;
        UIMenuListItem mVehicleType;
        UIMenuCheckboxItem mUsePVehicle;
        UIMenuItem mVehicle;
        Vehicle mVehToUse = null;
        UIMenuItem mName;
        string mNameString = "";
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
            if(InputWidowOpen)
            {
                if (menuPool.IsAnyMenuOpen()) menuPool.CloseAllMenus();
                switch (Function.Call<int>(Hash.UPDATE_ONSCREEN_KEYBOARD))
                {
                    default:
                        break;
                    case 1:
                        mNameString = Function.Call<string>(Hash.GET_ONSCREEN_KEYBOARD_RESULT);                     
                        InputWidowOpen = false;
                        CreateMenu.Visible = true;
                        mName.Text = "Name:  "+ mNameString;
                        break;
                    case 2:
                        InputWidowOpen = false;
                        CreateMenu.Visible = true;
                        break;
                }
            }

            // Fail states
            

            if (menuPool != null)
            {
                menuPool.ProcessMenus();
            }

            if (DEBUG)
            {
                myUIText = new UIText("" + CurrentState + " Distance:" + distance, new Point(10, 10), 0.4f, Color.WhiteSmoke, 0, false);
                myUIText.Draw();
            }
            if (!menuPool.IsAnyMenuOpen())
            {
                if (DrawStartPos)
                {
                    DrawStartPos = false;
                }
            }
            else if (DrawStartPos)
            {
                World.DrawMarker(MarkerType.VerticalCylinder, mStartVector, Vector3.Zero, Vector3.Zero, new Vector3(4, 4, 1), Color.FromArgb(180, Color.Yellow));
            }

            if (Game.Player.IsPlaying)
            {
                if ((int)CurrentState >= 2 && (int)CurrentState < 6)
                {
                    if (CurrentMission.PedCount > 0 && MissionPeds.FindAll(p => !p.IsAlive).Count > 0)
                    {                        
                        CurrentState = MissionStates.Fail;
                    }
                    if (MissionVehicle == null) CurrentState = MissionStates.Fail;
                    else if (MissionVehicle.IsDead) CurrentState = MissionStates.Fail;
                }
                switch (CurrentState)
                {
                    case MissionStates.None: // Active When No Mission is present.
                        if (CurrentMission != null)
                        {                            
                            if (CurrentMission.active)
                            {
                                MissionBlipStart(CurrentMission, MissionBlip);
                                CurrentState = MissionStates.Offered;
                            }
                        }
                        break;

                    case MissionStates.Offered:
                        distance = CurrentMission.GetStartPos().DistanceTo(Game.Player.Character.Position);
                        if (distance < 280)
                        {
                            World.DrawMarker(MarkerType.VerticalCylinder, CurrentMission.GetStartPos(), Vector3.Zero, Vector3.Zero, new Vector3(4, 4, 1), Color.FromArgb(180, Color.Yellow));
                            if (distance < 20)
                            {

                                if (MissionVehicle == null)
                                {
                                    MissionVehicle = GetMissionVehicle(CurrentMission);
                                }                                 
                                if(distance < 4)
                                {
                                    if (MissionVehicle != null && MissionVehicle.Driver != null && MissionVehicle.Driver.IsPlayer && MissionVehicle.IsStopped)
                                    {
                                        bool start = false;
                                        if (CurrentMission.PedCount > 0)
                                        {
                                            GeneratePeds(CurrentMission, MissionPeds);
                                            if (MissionPeds.Count > 0 && MissionPeds.Count == CurrentMission.PedCount) start = true;
                                        }
                                        else start = true;

                                        if (start)
                                        {
                                            MissionBlipEnd(CurrentMission, MissionBlip);
                                            CurrentState = MissionStates.Start; //Vehicle and Ped is spawned, and player is in vehicle. Change to Start state.
                                        }
                                    }
                                }                                
                            }
                        }
                        break;


                    case MissionStates.Start:
                        if (MissionVehicle.Driver != null && MissionVehicle.Driver.IsPlayer)
                        {
                            if(CurrentMission.PedCount > 0)
                            {
                                if (MissionPeds.Count > 0)
                                {
                                    foreach (Ped ped in MissionPeds)
                                    {
                                        if (ped.CurrentPedGroup != Game.Player.Character.CurrentPedGroup)
                                        {
                                            Game.Player.Character.CurrentPedGroup.Add(ped, false);
                                        }
                                    }
                                }
                                if (MissionPeds.FindAll(p => p.CurrentPedGroup != Game.Player.Character.CurrentPedGroup).Count == 0)
                                {
                                    foreach (Ped ped in MissionPeds)
                                    {
                                        if (ped.CurrentPedGroup == Game.Player.Character.CurrentPedGroup)
                                        {
                                            ped.Task.EnterVehicle(MissionVehicle, VehicleSeat.Passenger);
                                        }
                                    }
                                    CurrentState = MissionStates.Middle;
                                }
                            }
                            else
                            {
                                CurrentState = MissionStates.Middle;
                            }                            
                        }
                        break;

                    case MissionStates.Middle:
                        if(CurrentMission.PedCount > 0)
                        {
                            if(MissionPeds.FindAll(p => p.CurrentVehicle == MissionVehicle).Count == 0)
                            {
                                CurrentState = MissionStates.End;
                            }
                        }
                        else
                        {
                            CurrentState = MissionStates.End;
                        }
                        break;

                    case MissionStates.End:
                        distance = CurrentMission.GetNewEndPos().DistanceTo(Game.Player.Character.Position);
                        if (distance < 280)
                        {
                            World.DrawMarker(MarkerType.VerticalCylinder, CurrentMission.GetNewEndPos(), Vector3.Zero, Vector3.Zero, new Vector3(4, 4, 1), Color.FromArgb(180, Color.Yellow));
                            if (distance < 5 && MissionVehicle.IsStopped)
                            {
                                MissionBlip.Remove();
                                if (CurrentMission.PedCount > 0)
                                {
                                    foreach (Ped p in MissionPeds)
                                    {
                                        RemovePed(p);                                        
                                    }
                                    if (MissionPeds.FindAll(p => p.CurrentPedGroup == Game.Player.Character.CurrentPedGroup).Count == 0) CurrentState = MissionStates.Finish;
                                }
                                else CurrentState = MissionStates.Finish;                           
                            }
                        }
                        break;

                    case MissionStates.Finish:
                        MissionVehicle = null;
                        if (MissionPeds.FindAll(p => p.IsInVehicle()).Count == 0)
                        {
                            Game.Player.Money += 2000;
                            MissionPeds.Clear();
                            CurrentState = MissionStates.None; // Ped has gotten out of vehicle, give player money and set us to having no mission.
                        }                        
                        break;

                    default:
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

            CreateMenu = menuPool.AddSubMenu(MainMenu, "Create Mission");
            EditMenu = menuPool.AddSubMenu(MainMenu, "Edit Mission");

            #region Create Sub Menu
            mName = new UIMenuItem("Name:  ");
            mName.Activated += MName_Activated;
            CreateMenu.AddItem(mName);

            mStreet = new UIMenuItem("Street:  ");
            CreateMenu.AddItem(mStreet);

            mArea = new UIMenuItem("Area:  ");
            CreateMenu.AddItem(mArea);

            List<dynamic> typeList = new List<dynamic>();
            foreach (MissionType t in Enum.GetValues(typeof(MissionType)))
            {
                typeList.Add(t.ToString());
            }
            mtype = new UIMenuListItem("Type:  ", typeList, 0);
            CreateMenu.AddItem(mtype);

            List<dynamic> vTypeList = new List<dynamic>();
            vTypeList.Add("Any");
            foreach (VehicleClass t in Enum.GetValues(typeof(VehicleClass)))
            {
                vTypeList.Add(t.ToString());
            }
            mVehicleType = new UIMenuListItem("Vehicle Type:", vTypeList, 0);
            CreateMenu.AddItem(mVehicleType);            

            mVehicle = new UIMenuItem("Vehcile:  None");
            mVehicle.Activated += VehicleSelected;
            CreateMenu.AddItem(mVehicle);

            mUsePVehicle = new UIMenuCheckboxItem("Use Player Vehicle:", true);
            CreateMenu.AddItem(mUsePVehicle);

            List<dynamic> useList = new List<dynamic>();
            foreach (UseCase t in Enum.GetValues(typeof(UseCase)))
            {
                useList.Add(t.ToString());
            }
            mUseCase = new UIMenuListItem("Use:  ", useList, 3);
            CreateMenu.AddItem(mUseCase);

            mStartPos = new UIMenuItem("sPos:  ");
            mStartPos.Activated += StartPosSelected;
            CreateMenu.AddItem(mStartPos);

            mEndPos = new UIMenuItem("ePos:  ");
            mEndPos.Activated += EndPosSelected;
            CreateMenu.AddItem(mEndPos);

            mCount = new UIMenuSliderItem("Peds:  ");
            mCount.Maximum = 4;
            mCount.Multiplier = 1;
            mCount.OnSliderChanged += CountSliderChange;
            CreateMenu.AddItem(mCount);

            mAccept = new UIMenuItem("Add Mission");
            mAccept.Activated += AcceptSelected;
            CreateMenu.AddItem(mAccept);

            CreateMenu.OnMenuOpen += CreateMenu_OnMenuOpen;
            #endregion

            SetupPlayMenu(menuPool);

            menuPool.RefreshIndex();
        }

        #region Menu Events
        private void MName_Activated(UIMenu sender, UIMenuItem selectedItem)
        {
            if(!InputWidowOpen)
            {
                OpenInputWindow();
            }
        }

        private void CreateMenu_OnMenuOpen(UIMenu sender)
        {
            UpdateMenu();
            DrawStartPos = true;
        }

        private void VehicleSelected(UIMenu sender, UIMenuItem selectedItem)
        {
            if (Game.Player.Character.IsInVehicle())
            {
                mVehToUse = Game.Player.Character.CurrentVehicle;
                selectedItem.Text = "Spawned Vehicle:  " + mVehToUse.DisplayName;
            }
            else
            {
                mVehToUse = null;
                selectedItem.Text = "Vehcile:  None";
            }
        }

        private void AcceptSelected(UIMenu sender, UIMenuItem selectedItem)
        {

            if (mVehToUse != null)
            {
                if (mVehicleType.Items[mVehicleType.Index] == "Any")
                {
                    upLists(sender);
                }
                else
                {
                    VehicleClass missionVehicleClass;
                    Enum.TryParse(mVehicleType.Items[mVehicleType.Index].ToString(), out missionVehicleClass);
                    if (mVehToUse.ClassType == missionVehicleClass)
                    {
                        upLists(sender);
                    }
                    else
                    {
                        UI.ShowSubtitle("Your default vehicle does not match the required vehicle class.");
                    }
                }
            }
            else
            {
                UI.ShowSubtitle("Must Select a Default Vehicle.", 4000);
            }
        }

        public void upLists(UIMenu sender)
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
                    UI.ShowSubtitle("Mission added.", 4000);
                    mVehToUse = null;
                    sender.GoBack();
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
            Enum.TryParse(mVehicleType.Items[mUseCase.Index].ToString(), out VehicleClass _case);
            if(_case != VehicleClass.Boats) mStartVector.Z = World.GetGroundHeight(mStartVector);
            selectedItem.Text = "SPos:  " + mStartVector;
            mStreet.Text = "Street:  " + World.GetStreetName(mStartVector);
            mArea.Text = "Area:  " + World.GetZoneName(mStartVector);
        }

        private void CountSliderChange(UIMenuSliderItem sender, int newIndex)
        {
            sender.Text = "Peds:  " + sender.Value;
        }
        #endregion

        void UpdateMenu() // Create a Mission Menu
        {
            mVehToUse = null;
            mStartVector = Game.Player.Character.Position;
            Enum.TryParse(mVehicleType.Items[mUseCase.Index].ToString(), out VehicleClass _case);
            if (_case != VehicleClass.Boats) mStartVector.Z = World.GetGroundHeight(mStartVector);
            mName.Text = "Name:  "+mNameString;
            mStreet.Text = "Street:  " + World.GetStreetName(mStartVector);
            mArea.Text = "Area:  " + World.GetZoneName(mStartVector);
            mtype.Text = "Type:";
            mVehicleType.Text = "Vehicle Type:";
            mUsePVehicle.Text = "Use Player Vehicle:";
            mVehicle.Text = "Spawned Vehicle:  ";
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
            if (e.KeyCode == Keys.O)
            {
                if (!menuPool.IsAnyMenuOpen() && !InputWidowOpen)
                {
                    MainMenu.Visible = true;
                }
            }
            if (e.KeyCode == Keys.I)
            {
                //OpenInputWindow();
            }
        }

        void AddMission(XElement element)
        {
            var guid = Guid.NewGuid();

            foreach (XElement e in element.Elements())
            {
                if (e.Name == "Mission" && e.HasAttributes)
                {
                    if (e.FirstAttribute.Value == guid.ToString())
                    {
                        guid = Guid.NewGuid();
                    }
                }
            }
            XElement root = new XElement("Mission");
            root.SetAttributeValue("id", guid.ToString());
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

            name.Value = mNameString;
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
                        if (e.Name == "Type")
                        {
                            Enum.TryParse(e.Value, out type);
                        }

                        if (e.Name == "Name")
                        {
                            name = e.Value;
                        }

                        if (e.Name == "Street")
                        {
                            street = e.Value;
                        }

                        if (e.Name == "Area")
                        {
                            area = e.Value;
                        }

                        if (e.Name == "Vehicle")
                        {
                            vehClass = e.Value;
                        }

                        if (e.Name == "UsePlayerVehicle")
                        {
                            bool.TryParse(e.Value, out pVeh);
                        }

                        if (e.Name == "DefaultVehicle")
                        {
                            int.TryParse(e.Value, out hash);
                        }

                        if (e.Name == "UseCase")
                        {
                            Enum.TryParse(e.Value, out use);
                        }

                        if (e.Name == "PedCount")
                        {
                            int.TryParse(e.Value, out count);
                        }

                        if (e.Name == "SposX")
                        {
                            float.TryParse(e.Value, out sPos.X);
                        }

                        if (e.Name == "SposY")
                        {
                            float.TryParse(e.Value, out sPos.Y);
                        }

                        if (e.Name == "SposZ")
                        {
                            float.TryParse(e.Value, out sPos.Z);
                        }

                        if (e.Name == "EposX")
                        {
                            float.TryParse(e.Value, out ePos.X);
                        }

                        if (e.Name == "EposY")
                        {
                            float.TryParse(e.Value, out ePos.Y);
                        }

                        if (e.Name == "EposZ")
                        {
                            float.TryParse(e.Value, out ePos.Z);
                        }
                    }
                    if (sPos != Vector3.Zero)
                    {
                        myList.Add(new Mission(type, name, street, area, vehClass, pVeh, hash, use, sPos, ePos, count));
                    }
                }
            }
            if (DEBUG)
            {
                UI.Notify("Missions = " + myList.Count);
            }
        }

        public Vehicle GetMissionVehicle(Mission _mission)
        {
            if (_mission.UsePlayerVehicle && Game.Player.Character.IsInVehicle())
            {
                Vehicle v = Game.Player.Character.CurrentVehicle;
                if (_mission.UseVehicleClass && v.ClassType == _mission.vehicleClass)
                {
                    if(_mission.PedCount == 0) return v;
                    else if(_mission.PedCount > 0 && v.PassengerSeats >= _mission.PedCount) return v;
                    else return World.CreateVehicle(_mission.VehHash, _mission.GetStartPos());
                }
                else if (!_mission.UseVehicleClass)
                {
                    return v;
                }
                else
                {
                    return World.CreateVehicle(_mission.VehHash, _mission.GetStartPos());
                }
            }
            else
            {
                return World.CreateVehicle(_mission.VehHash, _mission.GetStartPos());
            }
        }

        void GeneratePeds(Mission _mission, List<Ped> _peds)
        {
            if(_mission.PedCount > 0)
            {                
                for (int i = 0; i < _mission.PedCount; i++)
                {
                    if (_peds.Count == _mission.PedCount) break;
                    Ped p = World.CreateRandomPed(_mission.GetStartPos().Around(4));
                    ConfigPedForMission(p);
                    _peds.Add(p);
                }
            }            
        }

        void ConfigPedForMission(Ped _ped)
        {
            _ped.BlockPermanentEvents = true;
            _ped.AlwaysKeepTask = true;
            _ped.AddBlip();
            _ped.CurrentBlip.IsFriendly = true;
            _ped.CurrentBlip.Scale = 0.5f;
        }

        void MissionBlipStart(Mission _mission, Blip _blip)
        {
            while(_blip==null)
            {
                _blip = World.CreateBlip(_mission.GetStartPos());
                Wait(10);
            }
            _blip.IsShortRange = false;
            _blip.Position = _mission.GetStartPos();
            _blip.Color = BlipColor.Yellow;
            _blip.Name = _mission.Name;
            switch (_mission.vehicleClass)
            {
                case VehicleClass.Planes:
                    _blip.Sprite = BlipSprite.ArmsTraffickingAir;
                    break;
                case VehicleClass.Helicopters:
                    _blip.Sprite = BlipSprite.Helicopter;
                    break;
                case VehicleClass.Emergency:
                    _blip.Sprite = BlipSprite.PolicePlayer;
                    break;

                default:
                    _blip.Sprite = BlipSprite.Standard;                   
                    break;

            }
        }
        void MissionBlipEnd(Mission _mission, Blip _blip)
        {
            while (_blip == null)
            {
                _blip = World.CreateBlip(_mission.GetNewEndPos());
                Wait(10);
            }
            if(_blip.IsShortRange) _blip.IsShortRange = false;
            _blip.Position = _mission.GetNewEndPos();
            _blip.Color = BlipColor.Yellow;
            _blip.Name = _mission.Name;
            _blip.ShowRoute = true;
            switch (_mission.vehicleClass)
            {
                default:
                    _blip.Sprite = BlipSprite.Standard;
                    break;
            }
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

        void RemovePed(Ped _ped)
        {
            _ped.CurrentBlip.Remove();
            _ped.LeaveGroup();
            if (!_ped.IsInVehicle()) _ped.Task.WanderAround();
            _ped.MarkAsNoLongerNeeded();
        }
        
        void OpenInputWindow()
        {
            if(!InputWidowOpen)
            {
                Function.Call(Hash.DISPLAY_ONSCREEN_KEYBOARD, false, "FMMC_KEY_TIP", "", "", "", "", "", 40);
                InputWidowOpen = true;
            }
            
        }//Call on tick and check for the result. //ie: UpdateOnscreenKeyboard(out result); if (result != null) { UI.ShowSubtitle(result); } void UpdateOnscreenKeyboard(out string result)        {            if (!updatingKeyboard)            { result = null; return; }            if (Function.Call<int>(Hash.UPDATE_ONSCREEN_KEYBOARD) == 2)            { updatingKeyboard = false; result = null; return; }            while (Function.Call<int>(Hash.UPDATE_ONSCREEN_KEYBOARD) == 0)                Script.Wait(0);            if (Function.Call<string>(Hash.GET_ONSCREEN_KEYBOARD_RESULT) == null) { updatingKeyboard = false; result = null; return; }            result = Function.Call<string>(Hash.GET_ONSCREEN_KEYBOARD_RESULT);            updatingKeyboard = false;        }

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
            private Vector3 StartPos = Vector3.Zero;
            private Vector3 EndPos = Vector3.Zero;
            private Vector3 newEndPos = Vector3.Zero;
            public int PedCount = 0;
            public bool active = false;

            public Mission(MissionType _type, string _name, string _street, string _area, string _veh, bool _pVeh, int _hash, UseCase _use, Vector3 _sPos, Vector3 _ePos, int _count)
            {
                Type = _type;
                Name = _name;
                Street = _street;
                Area = _area;
                if (_veh != "Any")
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
            public Vector3 GetStartPos()
            {
                return StartPos;
            }
            public Vector3 GetNewEndPos()
            {
                return newEndPos;
            }
            public void SetNewEndPos(Vector3 _pos)
            {
                if (EndPos != Vector3.Zero) newEndPos = EndPos;
                else newEndPos = _pos;
            }
        }
    }
}