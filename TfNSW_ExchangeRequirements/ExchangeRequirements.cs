using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

using GeometryGym.Ifc;
using GeometryGym.Mvd;

namespace TfNSW_ExchangeRequirements
{
	public class ExchangeRequirements_TfNSW_Drainage : ExchangeRequirements
	{
		public ExchangeRequirements_TfNSW_Drainage(List<IfcPropertySetTemplate> propertyTemplates) : base()
		{
			PropertyTemplates.AddRange(propertyTemplates);

			IfcClassification classification = new IfcClassification(new DatabaseIfc(ModelView.Ifc4NotAssigned), "Uniclass2015");

			//Indicitive/candidate sender, receiver, activity, process...
			ApplicableProcess.Add(new IfcClassificationReference(classification) { Identification = "PM_40_30", Name = "Design information" });
			Senders.Add(new IfcClassificationReference(classification) { Identification = "Ro_50_20_28", Name = "Drainage engineer (D)" });
			Receivers.Add(new IfcClassificationReference(classification) { Identification = "Ro_10_30", Name = "Asset management roles" });
			Activities.Add(new IfcClassificationReference(classification) { Identification = "Ac_05_40", Name = "Design stage activities" });
			Activities.Add(new IfcClassificationReference(classification) { Identification = "Ac_05_60", Name = "Handover and close-out stage activities" });
			
			//Exchange Requirements
			Concepts.Add(new Concept_TfNSW_AllDistributionElements()); // No building element proxies, all elements distribution element
			Concepts.Add(new Concept_TfNSW_StormWaterPipeSingleSweep()); // Future might force pipe axis definition with attributes
			// Validate all Systems are drainage
			// Validate Distribution System is assigned to all elements
			// To be advanced further
		}
	}

	/// <summary>
	/// Concept rule to restrict Alignment Transition Curve Types
	/// </summary>
	public class Concept_TfNSW_AcceptedAlignmentTransitions : ConceptRoot<IfcProduct>
	{
		public Concept_TfNSW_AcceptedAlignmentTransitions() : base(new Guid("{B7BFBB19-E154-4A58-8F7A-D15637659B05}"))
		{
			Owner = "TfNSW";
			Status = StatusEnum.Draft;
		}

		protected override List<ValidationResult> ValidateWorker(IfcProduct obj)
		{
			var representation = obj.Representation;
			if (representation == null)
				return new List<ValidationResult>();
			List<IfcTransitionCurveSegment2D> transitionSegments = representation.Extract<IfcTransitionCurveSegment2D>();
			if (transitionSegments == null || transitionSegments.Count == 0)
				return new List<ValidationResult>();
			List<ValidationResult> results = new List<ValidationResult>();
			foreach (IfcTransitionCurveSegment2D segment in transitionSegments)
			{
				if (segment.TransitionCurveType == IfcTransitionCurveType.CUBICPARABOLA) // Is this only transition used for TfNSW???
					continue;
				results.Add(new ValidationResult("Invalid transition curve type " + segment.TransitionCurveType + ". " + segment.ToString()));
			}
			return results;
		}
	}

	/// <summary>
	/// Concept rule to prevent building element proxies, currently validates all elements to be distribution elements
	/// </summary>
	public class Concept_TfNSW_AllDistributionElements : ConceptRoot<IfcElement>
	{
		public Concept_TfNSW_AllDistributionElements() : base(new Guid("{918F3392-4655-4BEF-8A83-AA92580B7043}"))
		{
			Owner = "TfNSW";
			Status = StatusEnum.Draft;
		}

		protected override List<ValidationResult> ValidateWorker(IfcElement obj)
		{
			return new List<ValidationResult>() { obj.ValidateIsObject<IfcDistributionElement>(true) };
		}
	}

	/// <summary>
	/// Concept rule to validate a single body representation that is a sweep
	/// </summary>
	public class Concept_TfNSW_StormWaterPipeSingleSweep : ConceptRoot<IfcFlowSegment>
	{
		public Concept_TfNSW_StormWaterPipeSingleSweep() : base(new Guid("{CE4891CF-F642-4875-B261-BFC35BF81F39}"))
		{
			Owner = "TfNSW";
			Status = StatusEnum.Draft;
			Applicability = new ApplicabilityRules<IfcFlowSegment>() { TemplateRule = isApplicable };
		}
		protected override List<ValidationResult> ValidateWorker(IfcFlowSegment obj)
		{
			ValidationResult result = obj.Validate_SingleBodyRepresentation<IfcExtrudedAreaSolid, IfcRevolvedAreaSolid, IfcSweptDiskSolid>(false);
			return new List<ValidationResult>() { result };
		}
		public bool isApplicable(IfcFlowSegment flowSegment)
		{
			// Doubled up Ifc Classification and Uniclass to be confirmed....
			IfcPropertySingleValue propertySingleValue = null;
			IfcSystem system = flowSegment.HasAssignments.OfType<IfcRelAssignsToGroup>().Select(x => x.RelatingGroup).OfType<IfcSystem>().FirstOrDefault();
			if (system != null)
			{
				propertySingleValue = system.FindProperty("TfNSW_Uniclass_AssetCode") as IfcPropertySingleValue;
				if (propertySingleValue != null)
				{
					if (!propertySingleValue.ValueStringStartsWith("Ss_50_30")) //Drainage collection and distribution systems
						return false;
				}
				IfcDistributionSystem distributionSystem = system as IfcDistributionSystem;
				if (distributionSystem != null) //IFC4 concept
				{
					if (distributionSystem.PredefinedType != IfcDistributionSystemEnum.DRAINAGE)
						return false;
				}
			}

			propertySingleValue = flowSegment.FindProperty("TfNSW_Uniclass_AssetCode") as IfcPropertySingleValue;
			if(propertySingleValue != null)
			{
				if (propertySingleValue.ValueStringStartsWith("Pr_65_52_63")) // Pipes and fittings
				  	return true;
			}

			if (flowSegment is IfcDuctSegment)
				return false;
			if (flowSegment is IfcCableCarrierSegment)
				return false;

			IfcTypeObject typeObject = flowSegment.RelatingType();
			if(typeObject is IfcDuctSegmentType)
				return false;
			if (typeObject is IfcCableCarrierSegmentType)
				return false;
			
			return true;
		}
	}
}
