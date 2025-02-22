using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino;
using Rhino.DocObjects;
using Rhino.DocObjects.Tables;
using Rhino.Geometry;
using ARDB = Autodesk.Revit.DB;

namespace RhinoInside.Revit.GH.Types
{
  using Convert.Geometry;
  using External.DB.Extensions;

  [Kernel.Attributes.Name("Base Point")]
  public interface IGH_BasePoint : IGH_GraphicalElement { }

  [Kernel.Attributes.Name("Base Point")]
  public class BasePoint : GraphicalElement, IGH_BasePoint, Bake.IGH_BakeAwareElement
  {
    protected override Type ValueType => typeof(ARDB.BasePoint);
    public new ARDB.BasePoint Value => base.Value as ARDB.BasePoint;

    protected override bool SetValue(ARDB.Element element) => IsValidElement(element) && base.SetValue(element);
    public static new bool IsValidElement(ARDB.Element element)
    {
      return element is ARDB.BasePoint &&
             element.Category.Id.ToBuiltInCategory() != ARDB.BuiltInCategory.OST_IOS_GeoSite;
    }

    public BasePoint() { }
    public BasePoint(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }
    public BasePoint(ARDB.BasePoint point) : base(point) { }

    public override string DisplayName => Value is ARDB.BasePoint point ? point.Category.Name : base.DisplayName;

    #region IGH_PreviewData
    protected override bool GetClippingBox(out BoundingBox clippingBox)
    {
      if (Value is ARDB.BasePoint point)
      {
        clippingBox = new BoundingBox
        (
          new Point3d[]
          {
            point.GetPosition().ToPoint3d(),
            (point.GetPosition() - point.GetSharedPosition()).ToPoint3d()
          }
        );
      }
      else clippingBox = NaN.BoundingBox;

      return true;
    }

    protected override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (Value is ARDB.BasePoint point)
      {
        var location = Location;
        point.Category.Id.TryGetBuiltInCategory(out var builtInCategory);
        var pointStyle = default(Rhino.Display.PointStyle);
        var angle = default(float);
        var radius = 6.0f;
        var secondarySize = 3.5f;
        switch (builtInCategory)
        {
          case ARDB.BuiltInCategory.OST_IOS_GeoSite:
            pointStyle = Rhino.Display.PointStyle.ActivePoint;
            break;
          case ARDB.BuiltInCategory.OST_ProjectBasePoint:
            pointStyle = Rhino.Display.PointStyle.RoundActivePoint;
            angle = (float) Rhino.RhinoMath.ToRadians(45);
            break;
          case ARDB.BuiltInCategory.OST_SharedBasePoint:
            pointStyle = Rhino.Display.PointStyle.Triangle;
            radius = 12.0f;
            secondarySize = 7.0f;
            break;
        }

        var strokeColor = (System.Drawing.Color) Rhino.Display.ColorRGBA.ApplyGamma(new Rhino.Display.ColorRGBA(args.Color), 2.0);
        args.Pipeline.DrawPoint(location.Origin, pointStyle, strokeColor, args.Color, radius, 2.0f, secondarySize, angle, true, true);
      }
    }
    #endregion

    #region IGH_BakeAwareElement
    bool IGH_BakeAwareData.BakeGeometry(RhinoDoc doc, ObjectAttributes att, out Guid guid) =>
      BakeElement(new Dictionary<ARDB.ElementId, Guid>(), true, doc, att, out guid);

    public bool BakeElement
    (
      IDictionary<ARDB.ElementId, Guid> idMap,
      bool overwrite,
      RhinoDoc doc,
      ObjectAttributes att,
      out Guid guid
    )
    {
      // 1. Check if is already cloned
      if (idMap.TryGetValue(Id, out guid))
        return true;

      if (Value is ARDB.BasePoint point)
      {
        var name = $"Revit::{point.Category.Name}";

        // 2. Check if already exist
        var index = doc.NamedConstructionPlanes.Find(name);

        // 3. Update if necessary
        if (index < 0 || overwrite)
        {
          if (point.IsEquivalent(BasePointExtension.GetProjectBasePoint(Document)))
            doc.ModelBasepoint = Location.Origin;

          var cplane = CreateConstructionPlane(name, Location, doc);

          if (index < 0) index = doc.NamedConstructionPlanes.Add(cplane);
          else if (overwrite) doc.NamedConstructionPlanes.Modify(cplane, index, true);
        }

        // TODO: Create a V5 Uuid out of the name
        //guid = new Guid(0, 0, 0, BitConverter.GetBytes((long) index));
        //idMap.Add(Id, guid);

        return true;
      }

      return false;
    }
    #endregion

    #region Properties
    public override Plane Location
    {
      get
      {
        if (Value is ARDB.BasePoint point)
        {
          var origin = point.GetPosition().ToPoint3d();
          var axisX = Vector3d.XAxis;
          var axisY = Vector3d.YAxis;

          if (point.IsShared)
          {
            point.Document.ActiveProjectLocation.GetLocation(out var _, out var basisX, out var basisY);
            axisX = basisX.ToVector3d();
            axisY = basisY.ToVector3d();
          }

          return new Plane(origin, axisX, axisY);
        }

        return NaN.Plane;
      }
    }
    #endregion
  }
}

namespace RhinoInside.Revit.GH.Types
{
  using External.DB.Extensions;

#if REVIT_2021
  using ARDB_InternalOrigin = ARDB.InternalOrigin;
#elif REVIT_2020
  using ARDB_InternalOrigin = ARDB.Element;
#else
  using ARDB_InternalOrigin = ARDB.BasePoint;
#endif

  [Kernel.Attributes.Name("Internal Origin")]
  public class InternalOrigin : GraphicalElement, IGH_BasePoint
  {
    protected override Type ValueType => typeof(ARDB_InternalOrigin);
    public new ARDB_InternalOrigin Value => base.Value as ARDB_InternalOrigin;

    protected override bool SetValue(ARDB.Element element) => IsValidElement(element) && base.SetValue(element);
    public static new bool IsValidElement(ARDB.Element element)
    {
      return element is ARDB_InternalOrigin &&
             element.Category?.Id.ToBuiltInCategory() == ARDB.BuiltInCategory.OST_IOS_GeoSite;
    }

    public InternalOrigin() { }
    public InternalOrigin(ARDB.Document doc, ARDB.ElementId id) : base(doc, id) { }

#if REVIT_2021
    public InternalOrigin(ARDB_InternalOrigin point) : base(point) { }
#else
    public InternalOrigin(ARDB.Element point) : base(point)
    {
      if (!IsValidElement(point))
        throw new ArgumentException("Invalid Element", nameof(point));
    }
#endif

    public override string DisplayName => Value is ARDB_InternalOrigin point ? point.Category.Name : base.DisplayName;

    #region IGH_PreviewData
    protected override bool GetClippingBox(out BoundingBox clippingBox)
    {
      clippingBox = Value is ARDB_InternalOrigin ?
        new BoundingBox(Point3d.Origin, Point3d.Origin) :
        NaN.BoundingBox;

      return true;
    }

    protected override void DrawViewportWires(GH_PreviewWireArgs args)
    {
      if (Value is ARDB_InternalOrigin)
      {
        var location = Location;
        var pointStyle = Rhino.Display.PointStyle.ActivePoint;
        var angle = default(float);
        var radius = 6.0f;
        var secondarySize = 3.5f;

        var strokeColor = (System.Drawing.Color) Rhino.Display.ColorRGBA.ApplyGamma(new Rhino.Display.ColorRGBA(args.Color), 2.0);
        args.Pipeline.DrawPoint(location.Origin, pointStyle, strokeColor, args.Color, radius, 2.0f, secondarySize, angle, true, true);
      }
    }
    #endregion

    #region Properties
    public override Plane Location => Value is ARDB_InternalOrigin ? Plane.WorldXY : NaN.Plane;
    #endregion
  }
}
