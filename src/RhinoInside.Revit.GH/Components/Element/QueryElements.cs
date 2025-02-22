using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using ARDB = Autodesk.Revit.DB;
using ERDB = RhinoInside.Revit.External.DB;

namespace RhinoInside.Revit.GH.Components.Elements
{
  using External.DB.Extensions;

  public class QueryElement : ZuiComponent
  {
    public override Guid ComponentGuid => new Guid("BBCF5D5C-9ABC-4E28-861A-584644EDFC3D");
    public override GH_Exposure Exposure => GH_Exposure.tertiary | GH_Exposure.obscure;

    public QueryElement() : base
    (
      name: "Query Element",
      nickname: "Element",
      description: "Get element by ID",
      category: "Revit",
      subCategory: "Element"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Occasional),
      ParamDefinition.Create<Param_GenericObject>("Id", "ID", "Element Id or UniqueId to look for", defaultValue: -1),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.Element>("Element", "E", string.Empty),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc)) return;
      if (!Params.GetData(DA, "Id", out Grasshopper.Kernel.Types.IGH_Goo goo)) return;

      switch (goo)
      {
        case Types.Reference id:
          if (doc.IsEquivalent(id.ReferenceDocument))
            DA.SetData("Element", Types.Element.FromReference(id.ReferenceDocument, id.GetReference()));

          else if (doc.IsEquivalent(id.Document))
            DA.SetData("Element", Types.Element.FromElementId(id.Document, id.Id));

          return;

        case Types.CategoryId c:
          DA.SetData("Element", Types.Category.FromElementId(doc, new ARDB.ElementId(c.Value)));
          return;

        case Types.ParameterId p:
          DA.SetData("Element", Types.ParameterKey.FromElementId(doc, new ARDB.ElementId(p.Value)));
          return;
      }

      DA.SetData("Element", Types.Element.FromValue(doc, goo.ScriptVariable()));
    }
  }

  public class QueryElements : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("0F7DA57E-6C05-4DD0-AABF-69E42DF38859");
    public override GH_Exposure Exposure => GH_Exposure.primary | GH_Exposure.obscure;
    protected override ARDB.ElementFilter ElementFilter => ERDB.CompoundElementFilter.ElementIsElementTypeFilter(inverted: true);

    static readonly string[] keywords = new string[] { "Count" };
    public override IEnumerable<string> Keywords => Enumerable.Concat(base.Keywords, keywords);

    public QueryElements() : base
    (
      name: "Query Elements",
      nickname: "Elements",
      description: "Get document model elements list",
      category: "Revit",
      subCategory: "Element"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      new ParamDefinition(new Parameters.Document(), ParamRelevance.Occasional),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", "Filter", GH_ParamAccess.item),
      ParamDefinition.Create<Param_Integer>("Limit", "L", $"Max number of Elements to query for.{Environment.NewLine}For an unlimited query remove this parameter.", defaultValue: 100, GH_ParamAccess.item, relevance: ParamRelevance.Primary),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.Element>("Elements", "E", "Elements list", GH_ParamAccess.list, relevance: ParamRelevance.Primary),
      ParamDefinition.Create<Param_Integer>("Count", "C", $"Elements count.{Environment.NewLine}For a more performant way of knowing how many elements this query returns remove the Elements output.", GH_ParamAccess.item, relevance: ParamRelevance.Primary),
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Parameters.Document.GetDataOrDefault(this, DA, "Document", out var doc)) return;
      if (!Params.GetData(DA, "Filter", out Types.ElementFilter filter, x => x.IsValid)) return;
      if (!Params.TryGetData(DA, "Limit", out int? limit, x => x >= 0)) return;

      using (var collector = new ARDB.FilteredElementCollector(doc))
      {
        var elementCollector = collector.WherePasses(ElementFilter).
          WherePasses(filter.Value);

        var _Elements_ = Params.IndexOfOutputParam("Elements");
        if
        (
          Params.TrySetDataList(DA, "Elements", () =>
          {
            var elements = limit.HasValue ?
              elementCollector.Take(limit.Value) :
              elementCollector;

            return elements.
              Select(Types.Element.FromElement).
              TakeWhileIsNotEscapeKeyDown(this);
          }) &&
          limit <= (Params.Output[_Elements_].VolatileData.get_Branch(DA.ParameterTargetPath(_Elements_))?.Count ?? 0)
        )
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"'{Params.Output[_Elements_].NickName}' output is limited to {limit.Value} elements.{Environment.NewLine}Increase or remove 'Limit' input parameter to retreive more elements.");
        }

        Params.TrySetData
        (
          DA,
          "Count",
          () =>
            _Elements_ < 0 || limit.HasValue ?
            elementCollector.GetElementCount() :
            Params.Output[_Elements_].VolatileData.get_Branch(DA.ParameterTargetPath(_Elements_)).Count
        );
      }
    }
  }

  public class QueryViewElements : ElementCollectorComponent
  {
    public override Guid ComponentGuid => new Guid("79DAEA3A-13A3-49BF-8BEB-AA28E3BE4515");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override ARDB.ElementFilter ElementFilter => new ARDB.ElementIsElementTypeFilter(inverted: true);

    public QueryViewElements() : base
    (
      name: "Query View Elements",
      nickname: "ViewEles",
      description: "Get elements visible in a view",
      category: "Revit",
      subCategory: "Element"
    )
    { }

    protected override ParamDefinition[] Inputs => inputs;
    static readonly ParamDefinition[] inputs =
    {
      ParamDefinition.Create<Parameters.View>("View", "V", "View", GH_ParamAccess.item),
      ParamDefinition.Create<Parameters.Category>("Categories", "C", "Category", GH_ParamAccess.list, optional: true),
      ParamDefinition.Create<Parameters.ElementFilter>("Filter", "F", "Filter", GH_ParamAccess.item, optional: true),
    };

    protected override ParamDefinition[] Outputs => outputs;
    static readonly ParamDefinition[] outputs =
    {
      ParamDefinition.Create<Parameters.GraphicalElement>("Elements", "E", "Elements list", GH_ParamAccess.list)
    };

    protected override void TrySolveInstance(IGH_DataAccess DA)
    {
      if (!Params.GetData(DA, "View", out Types.View view, x => x.IsValid)) return;
      if (!Params.TryGetDataList(DA, "Categories", out IList<Types.Category> categories)) return;
      if (!Params.TryGetData(DA, "Filter", out ARDB.ElementFilter filter, x => x.IsValidObject)) return;

      using (var collector = new ARDB.FilteredElementCollector(view.Document, view.Id))
      {
        var elementCollector = collector.WherePasses(ElementFilter);

        if (categories is object)
        {
          var ids = categories.
            Where(x => x?.IsValid == true && (x.Document is null || x.Document.Equals(view.Document))).
            Select(x => x.Id).ToArray();

          elementCollector = elementCollector.WherePasses
          (
            ERDB.CompoundElementFilter.ElementCategoryFilter(ids, inverted: false, view.Document.IsFamilyDocument)
          );
        }

        if (filter is object)
          elementCollector = elementCollector.WherePasses(filter);

        DA.SetDataList
        (
          "Elements",
          elementCollector.
          Where(x => Types.GraphicalElement.IsValidElement(x)).
          Select(Types.GraphicalElement.FromElement).
          TakeWhileIsNotEscapeKeyDown(this)
        );
      }
    }
  }
}
