using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace Lancelet
{
    public class FilterGeoJsonComponent : GH_Component
    {
        public FilterGeoJsonComponent()
          : base("Filter GeoJSON", "Filter",
              "Filter curves by attribute values",
              "Lancelet", "Utilities")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curves", "C", "Input curves from Lancelet", GH_ParamAccess.list);
            pManager.AddTextParameter("Names", "N", "Feature names from Lancelet", GH_ParamAccess.list);
            pManager.AddTextParameter("Filter", "F", "Filter text (searches in Names)", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Invert", "!", "Invert filter (exclude matches)", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Filtered", "C", "Filtered curves", GH_ParamAccess.list);
            pManager.AddTextParameter("Names", "N", "Filtered names", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Indices", "i", "Original indices of matched curves", GH_ParamAccess.list);
            pManager.AddTextParameter("Info", "I", "Filter statistics", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Read inputs
            var curves = new List<Grasshopper.Kernel.Types.GH_Curve>();
            var names = new List<string>();
            string filter = "";
            bool invert = false;

            if (!DA.GetDataList(0, curves)) return;
            if (!DA.GetDataList(1, names)) return;
            if (!DA.GetData(2, ref filter)) return;
            DA.GetData(3, ref invert);

            // Validate input counts match
            if (curves.Count != names.Count)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    $"Curves count ({curves.Count}) must match Names count ({names.Count})");
                return;
            }

            // Filter curves
            var filteredCurves = new List<Grasshopper.Kernel.Types.GH_Curve>();
            var filteredNames = new List<string>();
            var matchedIndices = new List<int>();

            for (int i = 0; i < curves.Count; i++)
            {
                bool matches = string.IsNullOrEmpty(filter) ||
                               names[i].IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;

                // Apply invert if specified
                if (invert) matches = !matches;

                if (matches)
                {
                    filteredCurves.Add(curves[i]);
                    filteredNames.Add(names[i]);
                    matchedIndices.Add(i);
                }
            }

            // Generate info
            var info = new List<string>
            {
                $"Input: {curves.Count} curves",
                $"Filter: '{filter}' {(invert ? "(inverted)" : "")}",
                $"Matched: {filteredCurves.Count} curves",
                $"Filtered out: {curves.Count - filteredCurves.Count} curves"
            };

            // Set outputs
            DA.SetDataList(0, filteredCurves);
            DA.SetDataList(1, filteredNames);
            DA.SetDataList(2, matchedIndices);
            DA.SetDataList(3, info);

            if (filteredCurves.Count == 0 && !string.IsNullOrEmpty(filter))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    $"No curves matched filter: '{filter}'");
            }
        }

        protected override Bitmap Icon
        {
            get
            {
                // Reuse the same icon for now
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

        public override Guid ComponentGuid => new Guid("B2C3D4E5-F6A7-8901-BCDE-F12345678901");
    }
}
