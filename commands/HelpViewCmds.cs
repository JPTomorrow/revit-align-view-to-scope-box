using System.Windows;
using System;
using JPMorrow.Tools.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace JPMorrow.UI.ViewModels
{
	public partial class HelpViewModel
    {

        public void SaveAndClose(Window window)
        {
            try
            {
                window.Close();
            }
            catch(Exception ex)
            {
                debugger.show(err:ex.ToString());
            }
        }

        public void CollapseViews()
        {
            try
            {
                string  search_txt = Search_Txt.ToLower();

                var qs = new string[] {
                    "How do I get this program set up with panel schedules from my current Revit project",
                    "How do I save a new set of panel schedules for the current Revit project",
                    "How do I import a panel schedule using Blubeam OCR",
                    "How do I create a new panel schedule in the program and fill it with breaker information",
                    "How do I check for duplicate circuits on jboxes in the current view JUNCTION BOXES DUPLICATES CIRCUIT VIEWS",
                    "How do I change breaker entry information LEFT RIGHT GRID TABLE",
                    "How do I create a new project file and load it",

                };

                List<Tuple<int, int>> rankings = new List<Tuple<int, int>>();

                Tuple<int, int> eval_rank(string q, int corresponding_row_idx)
                {

                    string[] search_split = search_txt.Split(' ');
                    string[] q_split = q.ToLower().Split(' ');
                    int passed = 0;

                    foreach(var str in q_split)
                    {
                        //EQUALS NOT CONTAINS! VERY IMPORTANT
                        if(search_split.Any(x => x.Trim(' ').Equals(str.Trim(' '))))
                            passed++;
                    }

                    return new Tuple<int, int>(passed, corresponding_row_idx);
                }

                for(var i = 0; i < qs.Length; ++i)
                {
                    var t = eval_rank(qs[i], i);
                    rankings.Add(t);
                }

                var rank_queue = new Queue<Tuple<int, int>>(rankings);

                int row_order = 1;
                //string o = ""; // debug
                while(rank_queue.Any())
                {
                    var r = rank_queue.Dequeue();

                    if(!rank_queue.Any(x => x.Item1 > r.Item1))
                    {
                        rows[r.Item2] = row_order;
                        //o += row_order + "(" + r.Item1  + ") " +  " | ";

                        row_order++;
                    }
                    else
                    {
                        rank_queue.Enqueue(r);
                    }
                }

                //debugger.show(err:o);
                RefreshReorder();

            }
            catch(Exception ex)
            {
                debugger.show(err:ex.ToString());
            }
        }

        public void LaunchTutVideo(string filename)
        {
            try
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\Autodesk\Revit\Addins\MarathonScripts\PanelScheduleInfo\res\vids\" + filename;

                Process.Start(path);
            }
            catch(Exception ex)
            {
                debugger.show(err:ex.ToString());
            }

        }
    }
}
