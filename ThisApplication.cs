using System;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using JPMorrow.Revit.Documents;
using System.Reflection;
using System.IO;
using System.Linq;
using JPMorrow.Tools.Diagnostics;

namespace MainApp
{
	/// <summary>
	/// Main Execution
	/// </summary>
	[Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
	[Autodesk.Revit.DB.Macros.AddInId("9BBF529B-520A-4877-B63B-BEF1238B6A05")]
    public partial class ThisApplication : IExternalCommand
    {

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
			//set revit model info
			string[] dataDirectories = new string[] { "data", };
			bool debugApp = false;
			ModelInfo revit_info = ModelInfo.StoreDocuments(commandData, dataDirectories, debugApp);

			Result ret()
			{
				debugger.show(err:"Select a scope box in a 3D view.");
				return Result.Succeeded;
			}

			//get selected scope box id
			var selection = revit_info.UIDOC.Selection.GetElementIds();
			if(!selection.Any()) return ret();
			var box_id = selection.First();
			if(box_id == null) return ret();

			//test element
			var el = revit_info.DOC.GetElement(box_id);
			if(el == null || el.Category.Name != "Scope Boxes") return ret();

			View3D view = null;
			try
			{
				view = revit_info.DOC.ActiveView as View3D;
				if(view == null || view.ViewType != ViewType.ThreeD)
				{
					debugger.show(err:"Select a scope box in a 3D view. Your current view is not a 3D view.");
					return Result.Succeeded;
				}
			}
			catch(Exception ex)
			{
				debugger.show(err:ex.ToString());
			}


			using(Transaction tx = new Transaction(revit_info.DOC, "Fit Scope Box"))
			{
				tx.Start();
					var scope_box = el.get_BoundingBox(view);
					view.SetSectionBox(scope_box);
				tx.Commit();
			}







			return Result.Succeeded;
        }
    }
}