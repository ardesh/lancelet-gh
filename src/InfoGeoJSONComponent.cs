using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Grasshopper.Kernel;
using Newtonsoft.Json.Linq;

namespace Lancelet
{
    public class InfoGeoJsonComponent : GH_Component
    {
        public InfoGeoJsonComponent()
          : base("GeoJSON Info", "Info",
              "Display information about a GeoJSON file without importing",
              "Lancelet", "Utilities")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("File Path", "F", "Path to GeoJSON file", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Summary", "S", "File summary information", GH_ParamAccess.list);
            pManager.AddTextParameter("Attributes", "A", "Available attribute keys", GH_ParamAccess.list);
            pManager.AddTextParameter("Names", "N", "Feature names preview (first 10)", GH_ParamAccess.list);
            pManager.AddTextParameter("Bounds", "B", "Coordinate bounds", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Read input
            string filePath = null;
            if (!DA.GetData(0, ref filePath)) return;

            // Validate file path
            if (!File.Exists(filePath))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"File not found: {filePath}");
                return;
            }

            try
            {
                // Read and parse GeoJSON
                string jsonContent = File.ReadAllText(filePath);
                JObject geoJson = JObject.Parse(jsonContent);

                JArray features = (JArray)geoJson["features"];
                if (features == null)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No features found in GeoJSON");
                    return;
                }

                // Count geometry types
                int pointCount = 0;
                int lineCount = 0;
                int polygonCount = 0;
                int otherCount = 0;

                var attributeKeys = new HashSet<string>();
                var featureNames = new List<string>();
                double minLon = double.MaxValue, maxLon = double.MinValue;
                double minLat = double.MaxValue, maxLat = double.MinValue;

                for (int i = 0; i < features.Count; i++)
                {
                    JObject feature = (JObject)features[i];
                    JObject geometry = (JObject)feature["geometry"];
                    JObject properties = (JObject)feature["properties"];

                    // Count geometry types
                    string geomType = geometry?["type"]?.ToString();
                    switch (geomType)
                    {
                        case "Point":
                            pointCount++;
                            break;
                        case "LineString":
                        case "MultiLineString":
                            lineCount++;
                            break;
                        case "Polygon":
                        case "MultiPolygon":
                            polygonCount++;
                            break;
                        default:
                            otherCount++;
                            break;
                    }

                    // Collect attribute keys
                    if (properties != null)
                    {
                        foreach (var prop in properties)
                        {
                            attributeKeys.Add(prop.Key);
                        }
                    }

                    // Collect feature names (first 10)
                    if (i < 10)
                    {
                        string name = properties?["NAME"]?.ToString() ??
                                    properties?["name"]?.ToString() ??
                                    $"Feature_{i}";
                        featureNames.Add($"{i}: {name}");
                    }

                    // Calculate bounds
                    JToken coordinates = geometry?["coordinates"];
                    if (coordinates != null)
                    {
                        ExtractBounds(coordinates, ref minLon, ref maxLon, ref minLat, ref maxLat);
                    }
                }

                // Generate summary
                var summary = new List<string>
                {
                    $"File: {Path.GetFileName(filePath)}",
                    $"Total Features: {features.Count}",
                    "",
                    "Geometry Types:",
                    $"  Points: {pointCount}",
                    $"  Lines: {lineCount}",
                    $"  Polygons: {polygonCount}",
                    (otherCount > 0 ? $"  Other: {otherCount}" : ""),
                    "",
                    $"Unique Attributes: {attributeKeys.Count}",
                    $"Coordinate System: {geoJson["crs"]?["properties"]?["name"] ?? "WGS84 (default)"}"
                };

                // Format bounds
                var bounds = new List<string>
                {
                    $"Longitude: {minLon:F6} to {maxLon:F6}",
                    $"Latitude: {minLat:F6} to {maxLat:F6}",
                    $"Width: ~{(maxLon - minLon) * 111000 * Math.Cos(minLat * Math.PI / 180):F0}m",
                    $"Height: ~{(maxLat - minLat) * 111000:F0}m"
                };

                // Set outputs
                DA.SetDataList(0, summary);
                DA.SetDataList(1, attributeKeys);
                DA.SetDataList(2, featureNames);
                DA.SetDataList(3, bounds);

                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                    $"Analyzed {features.Count} features");
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Error reading GeoJSON: {ex.Message}");
            }
        }

        private void ExtractBounds(JToken coordinates, ref double minLon, ref double maxLon, ref double minLat, ref double maxLat)
        {
            if (coordinates is JArray coordArray)
            {
                // Check if this is a coordinate pair [lon, lat] or nested array
                if (coordArray.Count >= 2 && coordArray[0].Type == JTokenType.Float || coordArray[0].Type == JTokenType.Integer)
                {
                    // This is a coordinate pair
                    double lon = (double)coordArray[0];
                    double lat = (double)coordArray[1];
                    minLon = Math.Min(minLon, lon);
                    maxLon = Math.Max(maxLon, lon);
                    minLat = Math.Min(minLat, lat);
                    maxLat = Math.Max(maxLat, lat);
                }
                else
                {
                    // This is nested, recurse
                    foreach (var item in coordArray)
                    {
                        ExtractBounds(item, ref minLon, ref maxLon, ref minLat, ref maxLat);
                    }
                }
            }
        }

        protected override Bitmap Icon
        {
            get
            {
                try
                {
                    var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                    var stream = assembly.GetManifestResourceStream("Lancelet.icon.bmp");
                    if (stream != null)
                        return new Bitmap(stream);
                }
                catch { }
                return null;
            }
        }

        public override Guid ComponentGuid => new Guid("C3D4E5F6-A7B8-9012-CDEF-123456789012");
    }
}
