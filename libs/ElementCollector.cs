using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using JPMorrow.Revit.Documents;
using JPMorrow.Tools.Diagnostics;

namespace JPMorrow.Revit.ElementCollection
{
	public struct ElementCollection
	{
		private readonly ElementId[] element_ids;

		public ElementId[] Element_Ids { get => element_ids; }
		public bool Has_Ids { get {
			if(Element_Ids == null) return false;
			if(!Element_Ids.Any()) return false;
			return true;
		}}

		public ElementCollection(ElementId[] el_ids)
		{
			element_ids = el_ids;
		}
	}

	public static class ElementCollector
	{
		public static ElementCollection CollectElements(ModelInfo info, bool contains, BuiltInCategory category = BuiltInCategory.INVALID, params string[] family_names)
		{
			//refresh element collector
			FilteredElementCollector rc() => new FilteredElementCollector(info.DOC, info.DOC.ActiveView.Id);

			//filtered element collector to use
			FilteredElementCollector el_coll = rc();
			List<ElementId> temp_list = new List<ElementId>();

			foreach(string family_name in family_names)
			{
				//get family master list
				el_coll = rc();
				Element[] fam_list = null;

				if(family_name == "BYPASS")
				{
					fam_list = new FilteredElementCollector(info.DOC)
					.OfClass(typeof(Family)).ToArray();
				}
				else
				{
					if(contains)
					{
						fam_list = new FilteredElementCollector(info.DOC)
						.OfClass(typeof(Family))
						.Where(x => x.Name.Contains(family_name)).ToArray();
					}
					else
					{
						fam_list = new FilteredElementCollector(info.DOC)
						.OfClass(typeof(Family))
						.Where(x => x.Name.Equals(family_name)).ToArray();
					}

				}

				//get type ids and dump all of them into list
				List<ElementId> type_ids = new List<ElementId>();
				foreach(var fam in fam_list)
				{
					Family fam_cvt = fam as Family;
					type_ids.AddRange(fam_cvt.GetFamilySymbolIds());
				}

				//collect the elements
				if(category == BuiltInCategory.OST_Conduit)
				{
					temp_list.AddRange(el_coll
					.OfCategory(category)
					.OfClass(typeof(Conduit))
					.Select(x => x.Id));
				}
				else if(category != BuiltInCategory.INVALID)
				{
					temp_list.AddRange(el_coll
					.OfCategory(category)
					.Where(x => type_ids.Any(y => x.GetTypeId().IntegerValue.Equals(y.IntegerValue)))
					.Select(x => x.Id));
				}
				else
				{
					temp_list.AddRange(el_coll
					.Where(x => type_ids.Any(y => x.GetTypeId().IntegerValue.Equals(y.IntegerValue)))
					.Select(x => x.Id));
				}


			}
			//return
			ElementCollection collection = new ElementCollection();
			if(temp_list.Any())
				collection = new ElementCollection(temp_list.ToArray());
			else
				collection = new ElementCollection(new ElementId[0]);

			return collection;
		}
	}
}