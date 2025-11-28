# Lancelet

**GeoJSON to Grasshopper importer with Earth Anchor Point transformation**

Lancelet is a Grasshopper plugin that imports georeferenced GeoJSON files into Rhino/Grasshopper with proper coordinate transformation. Named after the primitive chordate (*amphioxus*), it bridges the gap between GIS and parametric design workflows.

## Features

- **Import GeoJSON** - Standard geographic format (Points, Lines, Polygons)
- **Coordinate Transformation** - WGS84 lat/lon → Rhino model space
- **Earth Anchor Point** - Proper georeferencing with EAP
- **True North Rotation** - Aligns geographic north with model coordinates
- **Unit Conversion** - Supports Inches, Feet, Meters
- **Preserve Attributes** - All GeoJSON properties available in DataTree

## Why Lancelet?

Existing GIS-to-Grasshopper workflows often require:
- Commercial software (ArcGIS, Speckle subscriptions)
- Multiple conversion steps
- Version mismatches between connectors
- Complex QGIS/Heron GDAL dependencies

**Lancelet provides:**
- Single component solution
- No external dependencies
- Direct GeoJSON import
- Proper coordinate transformation
- Open source

## Installation

### Option 1: Download Release

1. Download `Lancelet.gha` from [Releases](https://github.com/ardesh/lancelet-gh/releases)
2. Unblock the file (Right-click → Properties → Unblock)
3. Copy to Grasshopper libraries:
   - Windows: `%APPDATA%\Grasshopper\Libraries\`
   - Mac: `~/Library/Application Support/McNeel/Rhinoceros/Scripts/`
4. Restart Rhino/Grasshopper

### Option 2: Build from Source

**Requirements:**
- Visual Studio 2019+ or Rider
- .NET Framework 4.8
- Rhino 7 or Rhino 8

**Build steps:**
```bash
git clone https://github.com/ardesh/lancelet-gh.git
cd lancelet-gh/src
dotnet build Lancelet.csproj -c Release
```

Output: `bin/Lancelet.gha`

## Usage

### Quick Start

1. Convert shapefile to GeoJSON (if needed):
   ```bash
   python examples/shp_to_geojson.py your_file.shp
   ```

2. In Grasshopper:
   - Add "Import GeoJSON" component (Lancelet tab)
   - Connect File Path to your `.geojson` file
   - Set Earth Anchor Point coordinates
   - Connect outputs to your design workflow

### Component: Import GeoJSON

**Inputs:**
- `File Path` - Path to GeoJSON file
- `EAP Latitude` - Earth Anchor Point latitude (degrees)
- `EAP Longitude` - Earth Anchor Point longitude (degrees)
- `EAP Elevation` - Earth Anchor Point elevation (model units)
- `True North Angle` - Rotation angle (degrees CCW from +Y)
- `Model Units` - "Inches", "Feet", or "Meters"

**Outputs:**
- `Curves` - Imported polylines and polygons
- `Points` - Imported points
- `Names` - Feature names
- `Attributes` - Feature properties (DataTree)

## Earth Anchor Point (EAP)

The EAP connects Rhino model coordinates to real-world latitude/longitude.

**To find your EAP:**
1. In Rhino: `EarthAnchorPoint` command
2. Set location using lat/lon
3. Use these values in Lancelet component

**Important:** Model units must match:
- Rhino file units (File → Properties → Units)
- Lancelet "Model Units" input
- EAP elevation value

## Examples

### Import Site Boundary

```
[File Path] → "site_boundary.geojson"
[EAP Lat] → 40.7128
[EAP Lon] → -74.0060
[EAP Elev] → 0
[True North] → 0
[Units] → "Feet"
```

Result: Site boundary curve in correct location

### Overlay with Contours

Import both boundaries and contours with same EAP settings - they will align automatically.

See [examples/](examples/) folder for sample GeoJSON files and Grasshopper definitions.

## Coordinate Transformation

Lancelet performs this transformation chain:

1. **Geographic Offset**: Subtract EAP coordinates
2. **Degrees → Meters**: Earth's radius calculation at latitude
3. **Meters → Model Units**: Apply conversion factor
4. **Rotation**: Apply true north rotation matrix
5. **Output**: Point in Rhino model space

## What It Does NOT Do

- Read shapefiles directly (use converter script)
- Read raster data (DEM, satellite imagery)
- Write/export GIS data (import only)
- CRS transformation (assumes WGS84 input)
- Complex GIS analysis
- 3D terrain generation

## Workflow

```
┌─────────────────┐
│  GIS Source     │  (QGIS, ArcGIS, County GIS, OSM)
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  .shp file      │
└────────┬────────┘
         │
         │ shp_to_geojson.py
         ▼
┌─────────────────┐
│  .geojson file  │
└────────┬────────┘
         │
         │ Lancelet component
         ▼
┌─────────────────┐
│  Grasshopper    │  (Curves, Points, Attributes)
│  Geometry       │
└─────────────────┘
```

## Troubleshooting

**Curves appear far from origin**
- Check EAP coordinates are correct
- Verify model units match Rhino file

**Wrong scale**
- Ensure model units match: Rhino units = Component units
- Check EAP elevation units

**Doesn't align with existing geometry**
- Verify EAP matches existing georeferenced data
- Check true north angle
- Test with reference points

**Import fails**
- Validate GeoJSON (geojson.io or QGIS)
- Check file path
- Ensure coordinates are WGS84 (EPSG:4326)

## Contributing

Contributions welcome! Please:
1. Fork repository
2. Create feature branch
3. Submit pull request

## License

MIT License - see [LICENSE](LICENSE) file

## Credits

Lancelet Contributors

## Related Tools

- [shp_to_geojson.py](examples/shp_to_geojson.py) - Shapefile to GeoJSON converter
- [QGIS](https://qgis.org/) - Free GIS software
- [geojson.io](http://geojson.io/) - GeoJSON validator/viewer

## Version

0.1.0 (Initial Release)

## Support

GitHub Issues: Report bugs and request features
