using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using GeometryGym.Ifc;
using GeometryGym.Mvd;
using TfNSW_ExchangeRequirements;

namespace TfNSW_ExchangeRequirements
{ 
	class Program
	{
		[STAThread]
		static void Main(string[] args)
		{

			string ifcFile = "", propertiesFile = "";
			if (args.Length > 0)
			{
				ifcFile = args[0];
			}

			if (string.IsNullOrEmpty(ifcFile))
			{
				OpenFileDialog openDialog = new OpenFileDialog() { Filter = "IFC BIM Data (*.ifc,*.ifcxml)|*.ifc;*.ifcxml" };
				openDialog.Title = "Nominate IFC file to test";
				DialogResult dresult = openDialog.ShowDialog();
				if (dresult != DialogResult.OK)
					return;
				ifcFile = openDialog.FileName;
			}

			if (args.Length > 1)
				propertiesFile = args[1];
			else
			{
				OpenFileDialog openDialog = new OpenFileDialog() { Filter = "IFC BIM Data (*.ifc,*.ifcxml)|*.ifc;*.ifcxml" };
				openDialog.Title = "Nominate Exchange Requirement Property Templates";
				DialogResult dresult = openDialog.ShowDialog();
				if (dresult == DialogResult.OK)
					propertiesFile = openDialog.FileName;
			}


			List<IfcPropertySetTemplate> propertySetTemplates = new List<IfcPropertySetTemplate>();
			if (!string.IsNullOrEmpty(propertiesFile))
			{
				try
				{
					DatabaseIfc dbTemplates = new DatabaseIfc(propertiesFile);
					propertySetTemplates.AddRange(dbTemplates.Context.Declares.SelectMany(x => x.RelatedDefinitions).OfType<IfcPropertySetTemplate>());
				}
				catch (Exception x)
				{
					System.Windows.Forms.MessageBox.Show(x.Message, "Property Template Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}
			}

			DatabaseIfc db = new DatabaseIfc(ifcFile);

			// Future will inspect sender, receiver and purpose of file to select specific exchange requirements

			ExchangeRequirements_TfNSW_Drainage drainageExchangeRequirements = new ExchangeRequirements_TfNSW_Drainage(propertySetTemplates);
			Dictionary<IfcRoot, List<ValidationResult>> results = drainageExchangeRequirements.Validate(db.Context);
			if (results.Count > 0)
			{
				//TfNSW_Excel_Config config = TfNSW_Excel_Config.Load();
				string assetCodePropertyName = "TfNSW_Project_AssetCode"; //config.AssetSheet.AssetPropertySet.ProjectCode.Name;
				string locationCodePropertyName = "TfNSW_Project_LocationCode";//config.LocationSheet.LocationPropertySet.ProjectCode.Name;

				StringBuilder stringBuilder = new StringBuilder();
				foreach (KeyValuePair<IfcRoot, List<ValidationResult>> pair in results)
				{
					IfcRoot root = pair.Key;
					string id = root.ToString();

					IfcObjectDefinition obj = root as IfcObjectDefinition;
					if (obj != null)
					{
						IfcPropertySingleValue psv = obj.FindProperty(assetCodePropertyName) as IfcPropertySingleValue;
						if (psv != null && psv.NominalValue != null && !string.IsNullOrEmpty(psv.NominalValue.ValueString))
							id = psv.NominalValue.ValueString;
						else
						{
							psv = obj.FindProperty(locationCodePropertyName) as IfcPropertySingleValue;
							if (psv != null && psv.NominalValue != null && !string.IsNullOrEmpty(psv.NominalValue.ValueString))
								id = psv.NominalValue.ValueString;
						}
					}
					stringBuilder.Append("XXX " + id + "\r\n");
					foreach (ValidationResult result in pair.Value)
						stringBuilder.Append("   " + result + "\r\n");
				}
				string path = Path.Combine(Path.GetDirectoryName(ifcFile), Path.GetFileNameWithoutExtension(ifcFile) + " validation " + Path.Combine(DateTime.Now.ToString("yyMMdd hhmm") + ".txt"));
				File.WriteAllText(path, stringBuilder.ToString());
				System.Diagnostics.Process.Start(path);
			}
			else
				MessageBox.Show("Valid File", "Valid", MessageBoxButtons.OK, MessageBoxIcon.Information);

		}
	}
}
