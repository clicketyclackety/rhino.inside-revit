using System;
using Grasshopper.Kernel.Types;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using External.DB;
  using External.DB.Extensions;
  using Grasshopper.Special;

  [Kernel.Attributes.Name("View Filter")]
  public interface IGH_FilterElement : IGH_Element
  {
    ElementFilter GetElementFilter();
  }

  [Kernel.Attributes.Name("Filter")]
  public class FilterElement : Element, IGH_FilterElement, IGH_ItemDescription
  {
    protected override Type ValueType => typeof(ARDB.FilterElement);
    public new ARDB.FilterElement Value => base.Value as ARDB.FilterElement;

    #region IGH_ItemDescription
    System.Drawing.Bitmap IGH_ItemDescription.GetTypeIcon(System.Drawing.Size size) => Properties.Resources.FilterElement;
    string IGH_ItemDescription.Name => DisplayName;
    string IGH_ItemDescription.Identity => IsLinked ? $"{{{ReferenceId?.ToString("D")}:{Id?.ToString("D")}}}" : $"{{{Id?.ToString("D")}}}";
    string IGH_ItemDescription.Description => Document?.GetTitle();
    #endregion

    public FilterElement() { }
    protected FilterElement(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    protected FilterElement(ARDB.FilterElement value) : base(value) { }

    public override bool CastTo<Q>(out Q target)
    {
      if (base.CastTo(out target))
        return true;

      if (IsValid)
      {
        if (typeof(Q).IsAssignableFrom(typeof(ElementFilter)))
        {
          target = (Q) (object) GetElementFilter();
          return true;
        }
      }

      return false;
    }

    public virtual ElementFilter GetElementFilter() => new ElementFilter(CompoundElementFilter.Empty);
  }

  [Kernel.Attributes.Name("Selection Filter")]
  public class SelectionFilterElement : FilterElement
  {
    protected override Type ValueType => typeof(ARDB.SelectionFilterElement);
    public new ARDB.SelectionFilterElement Value => base.Value as ARDB.SelectionFilterElement;

    public SelectionFilterElement() { }
    public SelectionFilterElement(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public SelectionFilterElement(ARDB.SelectionFilterElement value) : base(value) { }

    public override ElementFilter GetElementFilter() => Value is ARDB.SelectionFilterElement filter ?
      new ElementFilter(CompoundElementFilter.ExclusionFilter(filter.GetElementIds(), inverted: true)) : null;
  }

  [Kernel.Attributes.Name("Rule-based Filter")]
  public class ParameterFilterElement : FilterElement
  {
    protected override Type ValueType => typeof(ARDB.ParameterFilterElement);
    public new ARDB.ParameterFilterElement Value => base.Value as ARDB.ParameterFilterElement;

    public ParameterFilterElement() { }
    public ParameterFilterElement(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public ParameterFilterElement(ARDB.ParameterFilterElement value) : base(value) { }

    public override ElementFilter GetElementFilter() => Value is ARDB.ParameterFilterElement filter ?
      new ElementFilter(filter.GetElementFilter()) : null;
  }

  public class ElementFilter : GH_Goo<ARDB.ElementFilter>
  {
    public override string TypeName => "Revit Element Filter";
    public override string TypeDescription => "Represents a Revit element filter";
    public override bool IsValid => Value?.IsValidObject ?? false;
    public sealed override IGH_Goo Duplicate() => (IGH_Goo) MemberwiseClone();

    public ElementFilter() { }
    public ElementFilter(ARDB.ElementFilter filter) : base(filter) { }

    public override bool CastFrom(object source)
    {
      if (source is ARDB.ElementFilter filter)
      {
        Value = filter;
        return true;
      }

      return base.CastFrom(source);
    }

    public override bool CastTo<Q>(ref Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(ARDB.ElementFilter)))
      {
        target = (Q) (object) Value;
        return true;
      }

      return base.CastTo(ref target);
    }

    public override string ToString()
    {
      if (!IsValid)           return $"Invalid {TypeName}";
      if (Value.IsEmpty())    return "<nothing>";
      if (Value.IsUniverse()) return "<everything>";

      return Value.GetType().Name;
    }
  }

  public class FilterRule : GH_Goo<ARDB.FilterRule>
  {
    public override string TypeName => "Revit Filter Rule";
    public override string TypeDescription => "Represents a Revit filter rule";
    public override bool IsValid => Value?.IsValidObject ?? false;
    public sealed override IGH_Goo Duplicate() => (IGH_Goo) MemberwiseClone();

    public FilterRule() { }
    public FilterRule(ARDB.FilterRule filter) : base(filter) { }

    public override bool CastFrom(object source)
    {
      if (source is ARDB.FilterRule rule)
      {
        Value = rule;
        return true;
      }

      return base.CastFrom(source);
    }

    public override bool CastTo<Q>(ref Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(ARDB.FilterRule)))
      {
        target = (Q) (object) Value;
        return true;
      }

      return base.CastTo(ref target);
    }

    public override string ToString()
    {
      if (!IsValid)
        return $"Invalid {TypeName}";

      return $"{Value.GetType().Name}";
    }
  }
}
