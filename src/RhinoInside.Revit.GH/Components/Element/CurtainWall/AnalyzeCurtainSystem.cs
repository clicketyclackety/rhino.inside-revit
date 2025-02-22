using System;
using System.Linq;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Walls
{
  [Obsolete("Since 2023-01-09")]
  public class AnalyzeCurtainSystem : Component
  {
    public override Guid ComponentGuid => new Guid("16DDB8A7-045E-4FED-B48F-93F3A7AE461A");
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.hidden;
    protected override string IconTag => "ACS";

    public AnalyzeCurtainSystem() : base(
      name: "Analyze Curtain System",
      nickname: "A-CS",
      description: "Analyze given Curtain System element",
      category: "Revit",
      subCategory: "Wall"
    )
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.CurtainSystem(),
        name: "Curtain System",
        nickname: "CS",
        description: "Curtain System element",
        access: GH_ParamAccess.item
        );
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.CurtainGrid(),
        name: "Curtain Grids",
        nickname: "CG",
        description: "Curtain Grid definition associated with each face of the input Curtain System",
        access: GH_ParamAccess.item
        );
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "Curtain System", out Types.CurtainSystem curtainSystem, x => x.IsValid)) return;

      DA.SetDataList("Curtain Grids", curtainSystem?.CurtainGrids);
    }
  }
}
