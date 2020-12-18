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
		public static string[] Data_Dirs { get; } = new string[] {
			"data",
		};

		public static string App_Base_Path { get; set; } = null;
		public static string Settings_Base_Path { get; private set; }
		public static bool TestBed_Debug_Switch {get; set; } = false;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
			//set revit model info
			ModelInfo revit_info = ModelInfo.StoreDocuments(commandData);

			// set app path
			Assembly assem = Assembly.GetExecutingAssembly();
			UriBuilder uri = new UriBuilder(assem.CodeBase);
			string module_path = Path.GetDirectoryName(Uri.UnescapeDataString(uri.Path));
			App_Base_Path = RAP.GetApplicationBasePath(module_path, assem.GetName().Name, String.Join, TestBed_Debug_Switch);
			Settings_Base_Path = App_Base_Path + "settings\\";

			//create data directories
			RAP.GenAppStorageStruct(Settings_Base_Path, Data_Dirs, Directory.CreateDirectory, Directory.Exists);

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