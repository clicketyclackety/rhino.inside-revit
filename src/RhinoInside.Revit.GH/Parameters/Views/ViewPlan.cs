using System;
using System.Windows.Forms;
using Grasshopper.Kernel;
using ARDB = Autodesk.Revit.DB;
using ARUI = Autodesk.Revit.UI;

namespace RhinoInside.Revit.GH.Parameters
{
  [ComponentVersion(introduced: "1.7")]
  public class FloorPlan : View<Types.FloorPlan, ARDB.ViewPlan>
  {
    public override GH_Exposure Exposure => GH_Exposure.septenary | GH_Exposure.obscure;
    public override Guid ComponentGuid => new Guid("1BDE7F9F-2769-416B-AAA7-85FBACA3BC73");

    public FloorPlan() : base("Floor Plan", "Floor Plan", "Contains a collection of Revit floor plan views", "Params", "Revit") { }

    #region UI
    protected override ARDB.ViewFamily ViewFamily => ARDB.ViewFamily.FloorPlan;

    protected override void Menu_AppendPromptNew(ToolStripDropDown menu)
    {
      var FloorPlanId = ARUI.RevitCommandId.LookupPostableCommandId(ARUI.PostableCommand.FloorPlan);
      Menu_AppendItem
      (
        menu, $"Set new {TypeName}",
        Menu_PromptNew(FloorPlanId),
        Revit.ActiveUIApplication.CanPostCommand(FloorPlanId)
      );
    }
    #endregion
  }

  [ComponentVersion(introduced: "1.7")]
  public class CeilingPlan : View<Types.CeilingPlan, ARDB.ViewPlan>
  {
    public override GH_Exposure Exposure => GH_Exposure.septenary | GH_Exposure.obscure;
    public override Guid ComponentGuid => new Guid("33E34BD8-4E56-4227-9B32-C076346D8FC8");

    public CeilingPlan() : base("Ceiling Plan", "Ceiling Plan", "Contains a collection of Revit ceiling plan views", "Params", "Revit") { }

    #region UI
    protected override ARDB.ViewFamily ViewFamily => ARDB.ViewFamily.CeilingPlan;

    protected override void Menu_AppendPromptNew(ToolStripDropDown menu)
    {
      var CeilingPlanId = ARUI.RevitCommandId.LookupPostableCommandId(ARUI.PostableCommand.ReflectedCeilingPlan);
      Menu_AppendItem
      (
        menu, $"Set new {TypeName}",
        Menu_PromptNew(CeilingPlanId),
        Revit.ActiveUIApplication.CanPostCommand(CeilingPlanId)
      );
    }
    #endregion
  }

  [ComponentVersion(introduced: "1.7")]
  public class AreaPlan : View<Types.AreaPlan, ARDB.ViewPlan>
  {
    public override GH_Exposure Exposure => GH_Exposure.septenary | GH_Exposure.obscure;
    public override Guid ComponentGuid => new Guid("D0D3D169-9CAF-48E4-982A-E2AF58B362D4");

    public AreaPlan() : base("Area Plan", "Area Plan", "Contains a collection of Revit area plan views", "Params", "Revit") { }

    #region UI
    protected override ARDB.ViewFamily ViewFamily => ARDB.ViewFamily.AreaPlan;

    protected override void Menu_AppendPromptNew(ToolStripDropDown menu)
    {
      var AreaPlanId = ARUI.RevitCommandId.LookupPostableCommandId(ARUI.PostableCommand.AreaPlan);
      Menu_AppendItem
      (
        menu, $"Set new {TypeName}",
        Menu_PromptNew(AreaPlanId),
        Revit.ActiveUIApplication.CanPostCommand(AreaPlanId)
      );
    }
    #endregion
  }

  [ComponentVersion(introduced: "1.7")]
  public class StructuralPlan : View<Types.StructuralPlan, ARDB.ViewPlan>
  {
    public override GH_Exposure Exposure => GH_Exposure.septenary | GH_Exposure.obscure;
    public override Guid ComponentGuid => new Guid("BF2EFFD6-DF83-4921-A318-DB1E72A1D1FB");

    public StructuralPlan() : base("Structural Plan", "Structural Plan", "Contains a collection of Revit structural plan views", "Params", "Revit") { }

    #region UI
    protected override ARDB.ViewFamily ViewFamily => ARDB.ViewFamily.StructuralPlan;

    protected override void Menu_AppendPromptNew(ToolStripDropDown menu)
    {
      var StructuralPlanId = ARUI.RevitCommandId.LookupPostableCommandId(ARUI.PostableCommand.StructuralPlan);
      Menu_AppendItem
      (
        menu, $"Set new {TypeName}",
        Menu_PromptNew(StructuralPlanId),
        Revit.ActiveUIApplication.CanPostCommand(StructuralPlanId)
      );
    }
    #endregion
  }
}
