using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace Lancelet
{
    public class LanceletInfo : GH_AssemblyInfo
    {
        public override string Name => "Lancelet";

        public override Bitmap Icon => null; // TODO: Add icon

        public override string Description =>
            "GeoJSON to Rhino importer with Earth Anchor Point transformation";

        public override Guid Id => new Guid("8F3A2D1E-5B9C-4E7A-8D2F-1C6B9A4E3F7D");

        public override string AuthorName => "Lancelet Contributors";

        public override string AuthorContact => "https://github.com/ardesh/lancelet-gh";

        public override string Version => "0.1.0";
    }
}
