using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.IO;
using Autodesk.Revit.UI.Selection;
using System.Reflection;
using JPMorrow.Tools.Diagnostics;

namespace JPMorrow.Revit.Documents
{
	/// <summary>
	/// Revit Model Information
	/// </summary>
	public class ModelInfo
	{
		public UIApplication UIAPP { get; private set; }
		public UIDocument UIDOC { get => UIAPP.ActiveUIDocument; }
		public Document DOC { get => UIAPP.ActiveUIDocument.Document; }
		public Selection SEL { get => UIDOC.Selection; }

		public static string AppBasePath { get; set; } = null;
		public static string SettingsBasePath { get; set; } = null;
		public static string[] DataDirs { get; set; } = null;

		public ModelInfo(ExternalCommandData cData) {
			UIAPP = cData.Application;
		}

		/// <summary> Store Revit document data </summary>
		/// <param name="cData">External Command Data from current Revit instance</param>
		public static ModelInfo StoreDocuments(ExternalCommandData cData, string[] dataDirs, bool debug = false)
		{
			if(DataDirs == null)
				DataDirs = dataDirs;

			//Set application and settings base path
			Assembly assem = Assembly.GetExecutingAssembly();
			UriBuilder uri = new UriBuilder(assem.CodeBase);
			string module_path = Path.GetDirectoryName(Uri.UnescapeDataString(uri.Path));

			if(AppBasePath == null)
			{
				AppBasePath = string.Join("\\", module_path.Split('\\').ToList().Where(x => !x.Contains("Ribbon")));
				var full_ass_name = assem.GetName().Name;

				// remove revit year from assembly name if it exists.
				if(full_ass_name.Contains("_"))
					full_ass_name = full_ass_name.Split('_').First();
					
				AppBasePath += debug ? "\\RevitTestBed\\" : "\\" + full_ass_name + "\\";
			}

			if(SettingsBasePath == null)
			{
				SettingsBasePath = AppBasePath + "settings\\";
			}

			// generate folder structure for app
			DataDirs.ToList().ForEach(dir_name => {
				if(!Directory.Exists(SettingsBasePath + dir_name))
					Directory.CreateDirectory(SettingsBasePath + dir_name);
					});

			ModelInfo ret_info = new ModelInfo(cData);
			return ret_info;
		}

		/// <summary>
		/// gets a data directory.
		/// </summary>
		/// <returns>data directory</returns>
		public static string GetDataDirectory(string dir_name, bool include_end_backslash = false)
		{
			if(!DataDirs.Any(x => x == dir_name))
				throw new Exception("No data directory is named: " + dir_name);

			string ret_path = SettingsBasePath + dir_name;
			if(include_end_backslash) ret_path += "\\";
			return ret_path;
		}
	}
}
