using System.Windows;
using System.Windows.Forms;
using System;
using JPMorrow.Tools.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using JPMorrow.Tools.Data;
using JPMorrow.Panels;
using Autodesk.Revit.DB;

namespace JPMorrow.UI.ViewModels
{
	public partial class PanelImportHelpViewModel
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
    }
}
