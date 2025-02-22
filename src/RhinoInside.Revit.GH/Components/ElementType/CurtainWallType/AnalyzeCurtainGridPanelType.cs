using System;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Components.Walls
{
  using External.DB.Extensions;

  public class AnalyzeCurtainGridPanelType : Component
  {
    public override Guid ComponentGuid => new Guid("6F11977F-7CF3-41F1-8A69-2F4CD7287DEF");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override string IconTag => "ACGPT";

    public AnalyzeCurtainGridPanelType() : base
    (
      name: "Analyze Panel Type",
      nickname: "A-PT",
      description: "Analyze given panel type",
      category: "Revit",
      subCategory: "Wall"
    )
    { }

    protected override void RegisterInputParams(GH_InputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.ElementType(),
        name: "Panel Type",
        nickname: "PT",
        description: "Panel Type",
        access: GH_ParamAccess.item
        );
    }

    protected override void RegisterOutputParams(GH_OutputParamManager manager)
    {
      manager.AddParameter(
        param: new Parameters.Param_Enum<Types.CurtainPanelSystemFamily>(),
        name: "Family",
        nickname: "PSF",
        description: "Panel system family",
        access: GH_ParamAccess.item
        );
      manager.AddNumberParameter(
        name: "Offset",
        nickname: "O",
        description: "Panel type offset",
        access: GH_ParamAccess.item
        );
      manager.AddNumberParameter(
        name: "Thickness",
        nickname: "T",
        description: "Panel type thickness",
        access: GH_ParamAccess.item
        );
    }

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      // get input
      // panel types can be DB.FamilySymbol or DB.PanelType
      ARDB.FamilySymbol famInst = default;
      if (!DA.GetData("Panel Type", ref famInst))
        return;

      var inputType = famInst.GetType();
      // make sure other derivatives of DB.FamilySymbol do not pass this filter
      // we are only interested in panel types
      if (inputType == typeof(ARDB.FamilySymbol) || inputType == typeof(ARDB.PanelType))
      {
        // TODO: find a way to determine whether panel type is an Empty type or not
        // maybe the Id/Unique is fixed? Compare across multiple example models of various languages
        DA.SetData("Family", new Types.CurtainPanelSystemFamily(External.DB.CurtainPanelSystemFamily.Unknown));

        switch (famInst)
        {
          case ARDB.PanelType panelType:
            DA.SetData("Offset", panelType.get_Parameter(ARDB.BuiltInParameter.CURTAIN_WALL_SYSPANEL_OFFSET).AsGoo());
            DA.SetData("Thickness", panelType.get_Parameter(ARDB.BuiltInParameter.CURTAIN_WALL_SYSPANEL_THICKNESS).AsGoo());
            break;

          case ARDB.FamilySymbol finst:
            // make sure family symbol belongs to a Panel Family
            // finst.Family.IsCurtainPanelFamily returns FALSE !!!
            var isCurtainPanelFamily = finst.Family.FamilyCategory.Id.ToBuiltInCategory() == ARDB.BuiltInCategory.OST_CurtainWallPanels;
            // can not extract Offset and Thickness since they are not builtin for curtain panel custom families
            // TODO: maybe extract other info for Panel Families?!
            break;
        }
      }
    }
  }
}
