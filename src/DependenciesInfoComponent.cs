using System;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using Grasshopper.Kernel;

namespace Lancelet
{
    public class DependenciesInfoComponent : GH_Component
    {
        public DependenciesInfoComponent()
          : base("Dependencies Info", "Deps",
              "Show information about Python dependencies and GeoJSON data sources",
              "Lancelet", "Utilities")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            // No inputs - just informational
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Python Setup", "Py", "Python and pip installation info", GH_ParamAccess.list);
            pManager.AddTextParameter("Quick Method", "Quick", "No-QGIS method (address_to_contours.py)", GH_ParamAccess.list);
            pManager.AddTextParameter("Advanced Method", "Adv", "With-QGIS method (generate_contours_from_dem.py)", GH_ParamAccess.list);
            pManager.AddTextParameter("Data Sources", "Data", "Free DEM and GIS data sources", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Check if Python is available
            bool pythonFound = false;
            string pythonVersion = "Not found";

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                var process = Process.Start(psi);
                if (process != null)
                {
                    pythonVersion = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    pythonFound = true;
                }
            }
            catch
            {
                pythonVersion = "Python not found in PATH";
            }

            // Python setup info
            var pythonSetup = new List<string>
            {
                "=== PYTHON STATUS ===",
                pythonVersion.Trim(),
                "",
                "To install Python:",
                "1. Download from python.org (3.8+)",
                "2. Check 'Add Python to PATH' during install",
                "3. Restart terminal/cmd",
                "",
                "Verify installation:",
                "  python --version",
                "  pip --version",
                "",
                (pythonFound ? "✓ Python found" : "✗ Python not found - install Python first")
            };

            // Quick method (no QGIS)
            var quickMethod = new List<string>
            {
                "=== QUICK METHOD (No QGIS Required) ===",
                "",
                "File: address_to_contours.py",
                "Purpose: Address → Coordinates → Elevation → GeoJSON",
                "",
                "Required packages:",
                "  pip install numpy requests",
                "",
                "Usage:",
                "  python address_to_contours.py \"Your Address Here\"",
                "",
                "Example:",
                "  python address_to_contours.py \"126 Governors Dr, Leesburg, VA\"",
                "",
                "What it does:",
                "  1. Geocodes address (OpenStreetMap - free)",
                "  2. Gets elevation (USGS - free)",
                "  3. Generates contour GeoJSON",
                "  4. Outputs coordinates for Lancelet",
                "",
                "Limitations:",
                "  - Simplified contours (no full DEM)",
                "  - OpenTopography API key needed for full DEM",
                "  - Good for quick prototyping",
                "",
                "Optional for full DEM:",
                "  pip install rasterio",
                "  OR: conda install -c conda-forge gdal rasterio"
            };

            // Advanced method (with QGIS)
            var advancedMethod = new List<string>
            {
                "=== ADVANCED METHOD (With QGIS) ===",
                "",
                "File: generate_contours_from_dem.py",
                "Purpose: DEM Raster → Contour Lines → GeoJSON",
                "",
                "Requirements:",
                "  1. QGIS 3.x installed (includes GDAL)",
                "  2. Python package: pyshp",
                "",
                "Installation:",
                "  pip install pyshp",
                "",
                "Get DEM data:",
                "  1. USGS Earth Explorer (earthexplorer.usgs.gov)",
                "  2. Download 1/3 arc-second DEM (10m)",
                "  3. Place .tif file in data/QGIS/raster_data/",
                "",
                "Usage:",
                "  python generate_contours_from_dem.py",
                "",
                "What it does:",
                "  1. Clips DEM to site area",
                "  2. Generates contours at 2ft intervals",
                "  3. Converts to GeoJSON with 3D coordinates",
                "  4. Clean up temp files",
                "",
                "Advantages:",
                "  - Accurate contours from real DEM",
                "  - Customizable intervals",
                "  - Professional quality",
                "  - No API keys needed",
                "",
                "QGIS download:",
                "  qgis.org/download"
            };

            // Data sources
            var dataSources = new List<string>
            {
                "=== FREE DATA SOURCES ===",
                "",
                "ELEVATION DATA (DEM):",
                "  • USGS Earth Explorer",
                "    earthexplorer.usgs.gov",
                "    - Free, no account needed for download",
                "    - 10m resolution (1/3 arc-second)",
                "    - US coverage",
                "",
                "  • OpenTopography",
                "    opentopography.org",
                "    - Free API (registration required)",
                "    - Global SRTM coverage",
                "    - 30m resolution",
                "",
                "  • USGS Elevation Point Query",
                "    nationalmap.gov/epqs",
                "    - Free, no API key",
                "    - Point elevations only",
                "",
                "VECTOR DATA (Boundaries, Streets, etc.):",
                "  • OpenStreetMap",
                "    openstreetmap.org",
                "    - Free, open data",
                "    - Export as GeoJSON",
                "",
                "  • County GIS Portals",
                "    Search: '[County Name] GIS open data'",
                "    - Often free downloads",
                "    - Parcels, roads, utilities",
                "",
                "  • US Census TIGER",
                "    census.gov/geographies/mapping-files.html",
                "    - Free",
                "    - Roads, boundaries, water",
                "",
                "GEOCODING (Address → Coordinates):",
                "  • OpenStreetMap Nominatim",
                "    nominatim.openstreetmap.org",
                "    - Free, no API key",
                "    - Built into address_to_contours.py",
                "",
                "  • Google Maps",
                "    - Requires API key",
                "    - Most accurate",
                "",
                "GeoJSON TOOLS:",
                "  • geojson.io - View/edit GeoJSON",
                "  • QGIS - Convert formats",
                "  • ogr2ogr - Command-line conversion"
            };

            // Set outputs
            DA.SetDataList(0, pythonSetup);
            DA.SetDataList(1, quickMethod);
            DA.SetDataList(2, advancedMethod);
            DA.SetDataList(3, dataSources);

            if (!pythonFound)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    "Python not found. Install Python 3.8+ from python.org");
            }
            else
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                    "Python found. Check outputs for workflow info.");
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

        public override Guid ComponentGuid => new Guid("D4E5F6A7-B8C9-0123-DEFG-234567890123");
    }
}
