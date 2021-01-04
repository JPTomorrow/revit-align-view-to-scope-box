using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;

namespace JPMorrow.Tools.Diagnostics
{
	public delegate void DEBUG_DELEGATE_1(string header = "Error", string sub = "Generic", string err = "", int max_itr = -1);
    public delegate DialogResult DEBUG_DELEGATE_2(string err = "", string header = "Warning!", string continue_txt = "Do you want to continue?");
	public static class debugger
	{
		public static DEBUG_DELEGATE_1 show = RevitCustom.RevitCustomDebugger.Show;
        public static DEBUG_DELEGATE_1 debug_show = RevitCustom.RevitCustomDebugger.DebugShow;
        public static DEBUG_DELEGATE_2 show_yesno = RevitCustom.RevitCustomDebugger.ShowYesNo;
	}
}

namespace RevitCustom
{
	/// <summary>
	/// Debugger designed to use taskdialogs in revit in order to provide error handling feedback
	/// </summary>
	public static class RevitCustomDebugger
	{
		private static List<string> masterOutput = new List<string>();

		private static int count = 0;
		private static bool spam_lock = false;
		public static int Debug_Spam_Timeout {get; set;} = 30;
		private static Stopwatch guard_timer = new Stopwatch();

		private static string ConcatOutput()
		{
			string retStr = "";
			foreach (string str in masterOutput)
			{
				retStr += str;

                if(!str.Equals(masterOutput.Last()))
                    retStr += Environment.NewLine;
			}
			return retStr;
		}

		public static void SortAlphabetical()
		{
			if(masterOutput.Any())
			{
				masterOutput.Sort();
			}
		}

		public static void Clear() //clear all the text out of the buffer
		{
			masterOutput.Clear();
		}

		public static void AddErr(string str) //add a single string to buffer
		{
			masterOutput.Add(str);
		}

		public static void AddErr(List<string> str) //add a List of string to buffer
		{
			foreach (string s in str)
			{
				masterOutput.Add(s);
			}
		}

		public static void AddErr(List<int> intList) //add a List of ints to buffer
		{
			foreach (int i in intList)
			{
				masterOutput.Add(i.ToString());
			}
		}

		public static void AddErr(List<double> doubleList) //add a list of doubles to buffer
		{
			foreach (int i in doubleList)
			{
				masterOutput.Add(i.ToString());
			}
		}

		public static void Show(string header = "Error", string sub = "Generic", string err = "", int max_itr = -1)
		{
			if(spam_lock == true && guard_timer.Elapsed.Seconds >= Debug_Spam_Timeout)
			{
				guard_timer.Stop();
				count = 0;
				spam_lock = false;
			}

			AddErr(err);
			string o = ConcatOutput();
			if(max_itr > 0 && !spam_lock)
			{
				if(count < max_itr)
				{
					MessageBox.Show(o, header + " - " + (count + 1).ToString(), MessageBoxButtons.OK);
					count++;
				}
				if(count == max_itr)
				{
					guard_timer.Reset();
					guard_timer.Start();
					spam_lock = true;
				}
			}
			else if(max_itr == -1)
			{
				DialogResult result = MessageBox.Show(o, header, MessageBoxButtons.OK);

                
			}
			Clear();
		}

        public static DialogResult ShowYesNo(
            string err = "", string header = "Warning!",
            string continue_txt = "Do you want to continue?")
		{
			AddErr(err);
			string o = ConcatOutput();
            DialogResult result = MessageBox.Show(o + "\n\n" + continue_txt, header, MessageBoxButtons.YesNo);
			Clear();
            return result;
		}

        public static void DebugShow(string header = "Error", string sub = "Generic", string err = "", int max_itr = -1)
		{
			if(spam_lock == true && guard_timer.Elapsed.Seconds >= Debug_Spam_Timeout)
			{
				guard_timer.Stop();
				count = 0;
				spam_lock = false;
			}

			AddErr(err);
			string o = ConcatOutput();
			if(max_itr > 0 && !spam_lock)
			{
				if(count < max_itr)
				{
					MessageBox.Show(o, header + " - " + (count + 1).ToString(), MessageBoxButtons.OK);
					count++;
				}
				if(count == max_itr)
				{
					guard_timer.Reset();
					guard_timer.Start();
					spam_lock = true;
				}
			}
			else if(max_itr == -1)
			{
				DialogResult result = MessageBox.Show(o + "\n\nContinue Execution?\nNOTE: This will throw an exception to break execution!", header, MessageBoxButtons.YesNo);

                if(result == DialogResult.No) {
                    throw new Exception("DEBUG BREAK!!!!!!!!");
                }
			}
			Clear();
		}

		public static void Show(List<string> errList, string header = "Error", string sub = "Generic" , int max_itr = -1)
		{
			if(spam_lock && guard_timer.Elapsed.Seconds >= Debug_Spam_Timeout)
			{
				guard_timer.Stop();
				count = 0;
				spam_lock = false;
			}

			AddErr(errList);
			string o = ConcatOutput();
			if(max_itr > 0 && !spam_lock)
			{
				if(count < max_itr)
				{
					MessageBox.Show(o, header + " - " + (count + 1).ToString(), MessageBoxButtons.OK);
					count++;
				}
				else if(count == max_itr)
				{
					guard_timer.Reset();
					guard_timer.Start();
					spam_lock = true;
				}
			}
			else if(max_itr == -1)
			{
				MessageBox.Show(o, header, MessageBoxButtons.OK);
			}
			//dw = new DebugWindow(header, sub, o);
			//dw.ShowDialog();
			Clear();
		}
	}


}
