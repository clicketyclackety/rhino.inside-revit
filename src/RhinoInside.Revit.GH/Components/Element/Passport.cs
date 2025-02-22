using System;
using Grasshopper.Kernel;

namespace RhinoInside.Revit.GH.Components.Elements
{
  using External.DB.Extensions;

  public class ElementPassport : Component
  {
    public override Guid ComponentGuid => new Guid("BD534A54-B7ED-4D56-AA53-1F446D445DF6");
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.obscure;
    protected override string IconTag => "PASS";

    public ElementPassport()
    : base("Element Passport", "Passport", string.Empty, "Revit", "Element")
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(new Parameters.Element(), "Element", "E", string.Empty, GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(new Grasshopper.Kernel.Parameters.Param_Guid(), "DocumentGUID", "GUID", "A unique identifier for the document the Element resides", GH_ParamAccess.item);
      manager.AddParameter(new Parameters.Document(), "Document", "Document", "The document that contains this element", GH_ParamAccess.item);
      manager.AddTextParameter("UniqueID", "UID", "A stable across upgrades and workset operations unique identifier for the Element", GH_ParamAccess.item);
      manager.AddTextParameter("Id", "ID", "A unique identifier for an Element within the document that contains it", GH_ParamAccess.item);
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      var element = default(Types.Element);
      if (!DA.GetData("Element", ref element))
        return;

      DA.SetData("DocumentGUID", element?.Document.GetFingerprintGUID());
      DA.SetData("Document", element?.Document);
      DA.SetData("UniqueID", element?.UniqueId);
      DA.SetData("Id", element?.Id);
    }
  }
}
