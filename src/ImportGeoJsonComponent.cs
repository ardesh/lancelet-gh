using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Lancelet
{
    public class ImportGeoJsonComponent : GH_Component
    {
        public ImportGeoJsonComponent()
          : base("Import GeoJSON", "GeoJSON",
              "Import GeoJSON file with Earth Anchor Point transformation",
              "Lancelet", "Import")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("File Path", "F", "Path to GeoJSON file", GH_ParamAccess.item);
            pManager.AddNumberParameter("EAP Latitude", "Lat", "Earth Anchor Point Latitude (degrees)", GH_ParamAccess.item, 39.10165);
            pManager.AddNumberParameter("EAP Longitude", "Lon", "Earth Anchor Point Longitude (degrees)", GH_ParamAccess.item, -77.57778);
            pManager.AddNumberParameter("EAP Elevation", "Elev", "Earth Anchor Point Elevation (model units)", GH_ParamAccess.item, 3865.44);
            pManager.AddNumberParameter("True North Angle", "TN", "True North rotation angle (degrees CCW from +Y)", GH_ParamAccess.item, 48.08);
            pManager.AddTextParameter("Model Units", "Units", "Model coordinate units (Inches, Feet, Meters)", GH_ParamAccess.item, "Inches");
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Curves", "C", "Imported curves", GH_ParamAccess.list);
            pManager.AddPointParameter("Points", "P", "Imported points", GH_ParamAccess.list);
            pManager.AddTextParameter("Names", "N", "Feature names", GH_ParamAccess.list);
            pManager.AddGenericParameter("Attributes", "A", "Feature attributes (DataTree)", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Read inputs
            string filePath = null;
            double eapLat = 0;
            double eapLon = 0;
            double eapElev = 0;
            double trueNorthAngle = 0;
            string modelUnits = null;

            if (!DA.GetData(0, ref filePath)) return;
            if (!DA.GetData(1, ref eapLat)) return;
            if (!DA.GetData(2, ref eapLon)) return;
            if (!DA.GetData(3, ref eapElev)) return;
            if (!DA.GetData(4, ref trueNorthAngle)) return;
            if (!DA.GetData(5, ref modelUnits)) return;

            // Validate file path
            if (!File.Exists(filePath))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"File not found: {filePath}");
                return;
            }

            // Parse model units
            double metersToModelUnits;
            switch (modelUnits.ToLower())
            {
                case "inches":
                    metersToModelUnits = 39.3701; // meters to inches
                    break;
                case "feet":
                    metersToModelUnits = 3.28084; // meters to feet
                    break;
                case "meters":
                    metersToModelUnits = 1.0;
                    break;
                default:
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Unknown units: {modelUnits}. Use Inches, Feet, or Meters.");
                    return;
            }

            // Calculate coordinate transformation parameters
            var transformer = new GeoCoordinateTransformer(
                eapLat, eapLon, eapElev,
                trueNorthAngle,
                metersToModelUnits
            );

            try
            {
                // Read and parse GeoJSON
                string jsonContent = File.ReadAllText(filePath);
                JObject geoJson = JObject.Parse(jsonContent);

                var curves = new List<Curve>();
                var points = new List<Point3d>();
                var names = new List<string>();
                var attributes = new Grasshopper.DataTree<string>();

                JArray features = (JArray)geoJson["features"];
                if (features == null)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No features found in GeoJSON");
                    return;
                }

                for (int i = 0; i < features.Count; i++)
                {
                    JObject feature = (JObject)features[i];
                    JObject geometry = (JObject)feature["geometry"];
                    JObject properties = (JObject)feature["properties"];

                    string featureName = properties?["NAME"]?.ToString() ??
                                       properties?["name"]?.ToString() ??
                                       $"Feature_{i}";

                    string geomType = geometry["type"]?.ToString();
                    JToken coordinates = geometry["coordinates"];

                    // Convert geometry based on type
                    switch (geomType)
                    {
                        case "Point":
                            Point3d pt = transformer.GeoToModel(
                                (double)coordinates[0],
                                (double)coordinates[1]
                            );
                            points.Add(pt);
                            names.Add(featureName);
                            break;

                        case "LineString":
                            Curve lineCurve = CreatePolyline(coordinates, transformer);
                            if (lineCurve != null)
                            {
                                curves.Add(lineCurve);
                                names.Add(featureName);
                            }
                            break;

                        case "Polygon":
                            foreach (JArray ring in coordinates)
                            {
                                Curve polyCurve = CreatePolyline(ring, transformer);
                                if (polyCurve != null)
                                {
                                    curves.Add(polyCurve);
                                    names.Add(featureName);
                                }
                            }
                            break;

                        case "MultiLineString":
                            foreach (JArray line in coordinates)
                            {
                                Curve multiLineCurve = CreatePolyline(line, transformer);
                                if (multiLineCurve != null)
                                {
                                    curves.Add(multiLineCurve);
                                    names.Add(featureName);
                                }
                            }
                            break;

                        case "MultiPolygon":
                            foreach (JArray polygon in coordinates)
                            {
                                foreach (JArray ring in polygon)
                                {
                                    Curve multiPolyCurve = CreatePolyline(ring, transformer);
                                    if (multiPolyCurve != null)
                                    {
                                        curves.Add(multiPolyCurve);
                                        names.Add(featureName);
                                    }
                                }
                            }
                            break;
                    }

                    // Store attributes in DataTree
                    GH_Path path = new GH_Path(i);
                    if (properties != null)
                    {
                        foreach (var prop in properties)
                        {
                            attributes.Add($"{prop.Key}: {prop.Value}", path);
                        }
                    }
                }

                // Set outputs
                DA.SetDataList(0, curves);
                DA.SetDataList(1, points);
                DA.SetDataList(2, names);
                DA.SetDataTree(3, attributes);

                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                    $"Imported {curves.Count} curve(s) and {points.Count} point(s)");
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Error reading GeoJSON: {ex.Message}");
            }
        }

        private Curve CreatePolyline(JToken coordinates, GeoCoordinateTransformer transformer)
        {
            var pts = new List<Point3d>();

            foreach (JArray coord in coordinates)
            {
                double lon = (double)coord[0];
                double lat = (double)coord[1];
                pts.Add(transformer.GeoToModel(lon, lat));
            }

            if (pts.Count < 2) return null;

            var polyline = new Polyline(pts);
            return polyline.ToNurbsCurve();
        }

        protected override System.Drawing.Bitmap Icon => null; // TODO: Add icon

        public override Guid ComponentGuid => new Guid("A1B2C3D4-E5F6-7890-ABCD-EF1234567890");
    }

    /// <summary>
    /// Handles coordinate transformation from WGS84 to Rhino model space
    /// </summary>
    public class GeoCoordinateTransformer
    {
        private readonly double eapLat;
        private readonly double eapLon;
        private readonly double eapElev;
        private readonly double northVectorX;
        private readonly double northVectorY;
        private readonly double eastVectorX;
        private readonly double eastVectorY;
        private readonly double metersToModelUnits;

        // Conversion constants
        private const double MetersPerDegreeLat = 111111.0;

        public GeoCoordinateTransformer(
            double eapLatitude,
            double eapLongitude,
            double eapElevation,
            double trueNorthAngleDegrees,
            double metersToModelUnits)
        {
            this.eapLat = eapLatitude;
            this.eapLon = eapLongitude;
            this.eapElev = eapElevation;
            this.metersToModelUnits = metersToModelUnits;

            // Calculate longitude to meters conversion at this latitude
            double metersPerDegreeLon = MetersPerDegreeLat * Math.Cos(eapLatitude * Math.PI / 180.0);

            // Calculate true north and east vectors
            double angleRad = trueNorthAngleDegrees * Math.PI / 180.0;
            northVectorX = Math.Sin(angleRad);
            northVectorY = Math.Cos(angleRad);
            eastVectorX = Math.Cos(angleRad);
            eastVectorY = -Math.Sin(angleRad);
        }

        public Point3d GeoToModel(double lon, double lat)
        {
            // Step 1: Offset from EAP in degrees
            double lonOffset = lon - eapLon;
            double latOffset = lat - eapLat;

            // Step 2: Convert degrees to meters
            double metersPerDegreeLon = MetersPerDegreeLat * Math.Cos(eapLat * Math.PI / 180.0);
            double eastMeters = lonOffset * metersPerDegreeLon;
            double northMeters = latOffset * MetersPerDegreeLat;

            // Step 3: Convert meters to model units
            double eastModelUnits = eastMeters * metersToModelUnits;
            double northModelUnits = northMeters * metersToModelUnits;

            // Step 4: Apply inverse rotation (true north → model coordinates)
            double modelX = northModelUnits * northVectorX + eastModelUnits * eastVectorX;
            double modelY = northModelUnits * northVectorY + eastModelUnits * eastVectorY;

            // Step 5: Return point at EAP elevation
            return new Point3d(modelX, modelY, eapElev);
        }
    }
}
