using System.Windows;
using System.Windows.Forms;
using System;
using JPMorrow.Tools.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using JPMorrow.Tools.Data;
using JPMorrow.Panels;
using Autodesk.Revit.DB;
using JPMorrow.UI.Views;
using System.Collections.ObjectModel;
using JPMorrow.Revit.ElementCollection;

namespace JPMorrow.UI.ViewModels
{
	public partial class ParentViewModel
    {
        /// <summary>
        /// Save current session and exit
        /// </summary>
        public void DirtyClose(Window window)
        {
            if(Boat_File_Path != "None")
            {
                var result = System.Windows.MessageBox.Show(
                    "You have some unsaved data...\n" +
                    "Would you like to save before quiting?\n" +
                    "All unsaved data will be lost.",
                    "Unsaved Data", MessageBoxButton.YesNoCancel);

                if(result == MessageBoxResult.Cancel) return;
                if(result == MessageBoxResult.Yes)
                    SaveAndClose(window);
            }
            window.Close();
        }
        public void SaveAndClose(Window window)
        {
            try
            {
                if(Boat_File_Path != "None")
                {
                    PartialSave();
                }
                /*
                File.WriteAllText(Action_Log_File_Path, Action_Log);
                */
                window.Close();
            }
            catch(Exception ex)
            {
                debugger.show(err:ex.ToString());
            }
        }

        public void HelpWindow(Window window)
        {
            try
            {
                HelpView h = new HelpView(Info);
                h.Show();
            }
            catch(Exception ex)
            {
                debugger.show(err: ex.ToString());
            }
        }

        /// <summary>
        /// Load the current boat file
        /// </summary>
        public void LoadBoat(Window window)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Json files (*.json)|*.json";
            ofd.Title = "Load a Project File";
            var result = ofd.ShowDialog();
            if(result == DialogResult.OK || result == DialogResult.Yes)
            {
                UpdateBoatFilePath(ofd.FileName);

                //off-load boat
                Boat_File = JSON_Serialization.DeserializeFromFile<PanelBoatFile>(Boat_File_Path);

                Panels = new List<PanelInfo>(Boat_File.Panels);

                if(Panels.Any(x => x.Panel_Name == Panel_Name_Items[P_Name_Selected]))
                {
                    PanelInfo info = Panels.Find(x => x.Panel_Name == Panel_Name_Items[P_Name_Selected]);
                    RefreshDataGrids(info.Breakers);
                    UpdatePanelVoltageTxt(info.Panel_Voltage);
                }


                //fix header texts
                //WriteToLog("Project file loaded from " + Boat_File_Path);
            }
        }

        /// <summary>
        /// Save the current boat file
        /// </summary>
        public void SaveBoat(Window window)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Json files (*.json)|*.json";
            var result = sfd.ShowDialog();
            if(result == DialogResult.OK || result == DialogResult.Yes)
            {
                UpdateBoatFilePath("None");
                PanelBoatFile file = new PanelBoatFile(Panels);
                JSON_Serialization.SerializeToFile<PanelBoatFile>(file, sfd.FileName);
                //WriteToLog("Project file saved at " + Boat_File_Path);
            }
        }

        public void RefreshPanel(Window window)
        {
            try
            {
                //check boat file
                if(!Boat_File.Is_Loaded)
                {
                    debugger.show(err:"Please load a project file.");
                    return;
                }

                if(!ToggleHighlights()) return;

                // get user selected panel
                string panel_name_sel = Panel_Name_Items[P_Name_Selected];
                if(String.IsNullOrWhiteSpace(panel_name_sel))
                {
                    debugger.show(err:"Panel Name is blank.");
                    return;
                }

                //parse circuit max
                int max = -1;
                if(!GetMaxCircuits(out max)) return;

                //stag / seq
                Is_Sequential = false;

                // search for info in current panelinfos
                if(Panels.Any(x => x.Panel_Name == panel_name_sel))
                {
                    PanelInfo info = Panels.Find(x => x.Panel_Name == panel_name_sel);
                    Panels.Remove(info);

                    List<BreakerEntry> brks = new List<BreakerEntry>(info.Breakers);

                    // remove breakers if over
                    if(brks.Count > max)
                    {
                        int rem = brks.RemoveAll(x => x.Circuit_Number > max);
                        //debugger.show(err:rem.ToString());
                    }

                    // add breakers if under
                    if(brks.Count < max)
                    {
                        int added = 0;
                        for(var i = brks.OrderBy(x => x.Circuit_Number).Last().Circuit_Number + 1; i <= max; i++)
                        {
                            brks.Add(new BreakerEntry(i));
                            added++;
                        }
                    }

                    Element panel = PanelInfo.GetPanel(Info, panel_name_sel);
                    string new_voltage = PanelInfo.GetVoltage(panel);
                    if(new_voltage == null)
                        new_voltage = "";

                    Is_Sequential = info.Is_Sequential;
                    PanelInfo add_panel = new PanelInfo(
                        info.Panel_Name, new_voltage,
                        brks, Is_Sequential, true);

                    Panels.Add(add_panel);
                }
                else // create new panel
                {
                    Element panel = PanelInfo.GetPanel(Info, panel_name_sel);
                    if(panel == null)
                    {
                        debugger.show(err:"This panel no longer exists in the current model. Deleting Entry");
                        Panel_Name_Items.Remove(panel_name_sel);
                        RaisePropertyChanged("Panel_Name_Items");
                        return;
                    }

                    bool success = MakeNewPanelEntry(panel.Id, max);
                    if(!success)
                    {
                        debugger.show(err:"Couldn't create new panel");
                        return;
                    }
                }

                PanelInfo display_info = Panels.Find(x => x.Panel_Name == Panel_Name_Items[P_Name_Selected]);
                RefreshDataGrids(display_info.Breakers, Is_Sequential);
                UpdatePanelVoltageTxt(display_info.Panel_Voltage);
            }
            catch(Exception ex)
            {
                debugger.show(err:ex.ToString());
            }
        }

        public void SeqChanged(Window window)
        {
            try
            {
                PanelInfo p = Panels.Find(x => x.Panel_Name == Panel_Name_Items[P_Name_Selected]);
                if(!p.Is_Valid) return;
                Panels.Remove(p);
                Panels.Add(new PanelInfo(p.Panel_Name, p.Panel_Voltage, p.Breakers, Is_Sequential, true));
                RefreshDataGrids(p.Breakers, Is_Sequential);
            }
            catch(Exception ex)
            {
                debugger.show(err:ex.ToString());
            }
        }

        public void PanelChanged(Window window)
        {
            //check boat file
            if(!Boat_File.Is_Loaded) return;

            string panel_name_sel = Panel_Name_Items[P_Name_Selected];
            if(String.IsNullOrWhiteSpace(panel_name_sel))
            {
                debugger.show(err:"Panel Name is blank.");
                return;
            }

            if(Panels.Any(x => x.Panel_Name == panel_name_sel))
            {
                PanelInfo info = Panels.Find(x => x.Panel_Name == panel_name_sel);
                UpdatePanelVoltageTxt(info.Panel_Voltage);

                Is_Sequential = info.Is_Sequential;
                RaisePropertyChanged("Is_Sequential");
                RefreshDataGrids(info.Breakers, info.Is_Sequential);
            }
            else
            {
                UpdatePanelVoltageTxt();
                Left_Panel_Items.Clear();
                Right_Panel_Items.Clear();
                RaisePropertyChanged("Left_Panel_Items");
                RaisePropertyChanged("Right_Panel_Items");
            }
        }

        public void UpdateBreakers(Window window)
        {
            if(!ToggleHighlights()) return;

            var selected_breakers = Left_Panel_Items
                .Where(x => x.IsSelected)
                .Concat(Right_Panel_Items.Where(x => x.IsSelected)).Select(x => x.Value).ToList();

            string panel_name_sel = Panel_Name_Items[P_Name_Selected];
            if(Panels.Any(x => x.Panel_Name == panel_name_sel))
            {
                PanelInfo p = Panels.Find(x => x.Panel_Name == panel_name_sel);
                var brks = p.Breakers;
                var new_brks = brks.Except(selected_breakers).ToList();

                int amps = int.Parse(Amps_Items[Amps_Selected]);
                int poles = int.Parse(Poles_Items[Poles_Selected]);
                string e_name = String.IsNullOrWhiteSpace(New_Entry_Name) ? "SPACE" : New_Entry_Name;

                foreach(var brk in selected_breakers)
                {
                    new_brks.Add(new BreakerEntry(
                        brk.Circuit_Number, amps, poles,
                        Hot_Circuit_Items[Hot_Circuit_Selected],
                        Nuetral_Circuit_Items[Nuetral_Selected],
                        Grd_Circuit_Items[Ground_Selected],
                        e_name, -1  ));
                }

                Panels.Remove(p);

                PanelInfo new_pan = new PanelInfo(
                    p.Panel_Name, p.Panel_Voltage,
                    new_brks, Is_Sequential, true);

                Panels.Add(new_pan);
                RefreshDataGrids(new_pan.Breakers, new_pan.Is_Sequential);
            }

        }

        public void ChangeBreakersToSpare(Window window)
        {
            if(!ToggleHighlights()) return;

            var selected_breakers = Left_Panel_Items
                .Where(x => x.IsSelected)
                .Concat(Right_Panel_Items.Where(x => x.IsSelected)).Select(x => x.Value).ToList();

            string panel_name_sel = Panel_Name_Items[P_Name_Selected];
            if(Panels.Any(x => x.Panel_Name == panel_name_sel))
            {
                PanelInfo p = Panels.Find(x => x.Panel_Name == panel_name_sel);
                var brks = p.Breakers;
                var new_brks = brks.Except(selected_breakers).ToList();

                foreach(var brk in selected_breakers)
                {
                    new_brks.Add(new BreakerEntry(
                        brk.Circuit_Number, 20, 1,
                        "#12", "#12", "#12", "SPARE", -1  ));
                }

                Panels.Remove(p);
                PanelInfo new_pan = new PanelInfo(p.Panel_Name, p.Panel_Voltage, new_brks, Is_Sequential, true);
                Panels.Add(new_pan);
                RefreshDataGrids(new_pan.Breakers, new_pan.Is_Sequential);
            }
        }

        public void PlaceJbox(Window window)
        {
            if(!ToggleHighlights()) return;

            var selected_circs = Left_Panel_Items
            .Where(x => x.IsSelected).Concat(Right_Panel_Items.Where(x => x.IsSelected))
            .Select(w => w.Value.Circuit_Number).OrderBy(x => x).ToList();

            if(!selected_circs.Any())
            {
                debugger.show(err:"Please select some entries in the panel schedule to be paired with the placed boxes.");
                return;
            }

            string to_circuits = String.Join(", ", selected_circs);

            double placement_elev = 0;
            bool success = cvt_str(Box_Elevation, out placement_elev);

            if(!success)
            {
                debugger.show(err:"That is an invalid box elevation.");
                return;
            }

            string panel_name_sel = Panel_Name_Items[P_Name_Selected];
            if(String.IsNullOrWhiteSpace(panel_name_sel)) return;
            PanelInfo p = Panels.Find(x => x.Panel_Name == panel_name_sel);
            if(!p.Is_Valid)
            {
                debugger.show(err:"Invalid Panel during junction box placement. Has it been created yet?");
                return;
            }

            JBoxHandler.PlaceBoxes(Info, placement_elev, 1, p.Panel_Name, to_circuits);
        }

        private bool highlight_toggle = false;
        private bool ToggleHighlights()
        {
            if(highlight_toggle)
            {
                ClearBreakerColors();
                highlight_toggle = false;
                return false;
            }
            return true;
        }

        public void HighlightCircuits(Window window)
        {
            try
            {
                if(!ToggleHighlights()) return;

                //check boat file
                if(!Boat_File.Is_Loaded)
                {
                    debugger.show(err:"Please load a project file.");
                    return;
                }

                var coll = ElementCollector.CollectElements(Info, true,  BuiltInCategory.OST_ElectricalFixtures, "Conduit Junction Box");
                if(!coll.Has_Ids) return;
                List<Element> boxes_to_proc = new List<Element>();
                foreach(var id in coll.Element_Ids)
                {
                    Element el = Info.DOC.GetElement(id);
                    Parameter from = el.LookupParameter("From");

                    if(from == null) continue;
                    if(String.IsNullOrWhiteSpace(from.AsString())) continue;
                    if(from.AsString().Trim(' ') != Panel_Name_Items[P_Name_Selected]) continue;

                    boxes_to_proc.Add(el);
                }

                if(!boxes_to_proc.Any()) return;

                // get all of the circuits off the boxes
                List<int> circuits = new List<int>();
                List<int> dupe_circs = new List<int>();
                foreach(var box in boxes_to_proc)
                {
                    Parameter to = box.LookupParameter("To");

                    if( to == null || String.IsNullOrWhiteSpace(to.AsString()))
                        continue;

                    string[] to_split = to.AsString().Split(',');
                    bool success = false;

                    foreach(var c in to_split)
                    {
                        int c_num = -1;
                        success = int.TryParse(c.Trim(' '), out c_num);
                        if(!success) continue;

                        if(circuits.Any(x => x == c_num))
                        {
                            dupe_circs.Add(c_num);
                            circuits.Remove(c_num);
                            continue;
                        }

                        circuits.Add(c_num);
                    }
                }

                ChangeBreakerColors(BreakerColor.Good, circuits.ToArray());
                ChangeBreakerColors(BreakerColor.Fatal, dupe_circs.ToArray());
                highlight_toggle = true;
            }
            catch(Exception ex)
            {
                debugger.show(err:ex.ToString());
            }
        }

        public void ImportExcelCircuits(Window window)
        {
            try
            {
                //check boat file
                if(!Boat_File.Is_Loaded)
                {
                    debugger.show(err:"Please load a project file.");
                    return;
                }

                if(!ToggleHighlights()) return;

                var help_window = new PanelImportHelpView(Info);
			    help_window.ShowDialog();

                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "Excel Files|*.xlsx;";
                ofd.Title = "Select an excel file";
                var result = ofd.ShowDialog();
                if(result == DialogResult.OK || result == DialogResult.Yes)
                {
                    List<BreakerEntry> entries_to_add = PSEI.Import(Info, ofd.FileName);

                    string panel_name_sel = Panel_Name_Items[P_Name_Selected];
                    PanelInfo p = Panels.Find(x => x.Panel_Name == panel_name_sel);
                    PanelInfo new_pan;

                    string p_name = panel_name_sel;
                    if(p.Is_Valid)
                    {
                        Panels.Remove(p);

                        Element panel = PanelInfo.GetPanel(Info, panel_name_sel);
                        string new_voltage = PanelInfo.GetVoltage(panel);
                        if(new_voltage == null)
                            new_voltage = "";

                        new_pan = new PanelInfo(
                            p.Panel_Name, new_voltage,
                            entries_to_add, Is_Sequential, true);
                    }
                    else
                    {
                        Element panel = PanelInfo.GetPanel(Info, panel_name_sel);
                        if(panel == null)
                        {
                            debugger.show(err:"This panel no longer exists in the current model. Deleting Entry");
                            Panel_Name_Items.Remove(panel_name_sel);
                            RaisePropertyChanged("Panel_Name_Items");
                            return;
                        }

                        new_pan = PanelInfo.ProccessPanel(
                            Info, panel.Id, entries_to_add, Is_Sequential, true);
                    }

                    Panels.Add(new_pan);
                    RefreshDataGrids(new_pan.Breakers, new_pan.Is_Sequential);
                    UpdatePanelVoltageTxt(new_pan.Panel_Voltage);
                }
            }
            catch(Exception ex)
            {
                debugger.show(err:ex.ToString());
            }



        }
    }
}
