"""
Shapefile to GeoJSON Converter
126 Governors Drive, Leesburg, VA

Converts shapefiles to standard GeoJSON format for use in Grasshopper.
GeoJSON is a widely-supported format that can be read by GIS tools and custom importers.

Usage:
    python shp_to_geojson.py                          # Convert all lot_boundary*.shp
    python shp_to_geojson.py input.shp                # Convert single file
    python shp_to_geojson.py input.shp output.geojson # Specify output
"""

import shapefile
import json
from pathlib import Path
import sys

def shp_to_geojson(shp_path, geojson_path=None):
    """
    Convert shapefile to GeoJSON.

    Args:
        shp_path: Path to .shp file
        geojson_path: Optional output path (default: same name as .shp with .geojson)

    Returns:
        Path to created GeoJSON file
    """
    shp_path = Path(shp_path)

    if not shp_path.exists():
        print(f"[ERROR] Shapefile not found: {shp_path}")
        return None

    # Default output path
    if geojson_path is None:
        geojson_path = shp_path.with_suffix('.geojson')
    else:
        geojson_path = Path(geojson_path)

    print("=" * 80)
    print("SHAPEFILE TO GEOJSON CONVERTER")
    print("=" * 80)
    print()
    print(f"Input:  {shp_path.name}")
    print(f"Output: {geojson_path.name}")
    print()

    # Read shapefile
    sf = shapefile.Reader(str(shp_path))

    # Get field names
    field_names = [f[0] for f in sf.fields[1:]]

    # Build GeoJSON structure
    geojson = {
        "type": "FeatureCollection",
        "crs": {
            "type": "name",
            "properties": {
                "name": "EPSG:4326"  # WGS84 - standard for GeoJSON
            }
        },
        "features": []
    }

    # Convert each shape to a GeoJSON feature
    for shape_rec in sf.shapeRecords():
        shape = shape_rec.shape
        record = shape_rec.record

        # Extract attributes
        properties = {}
        for i, field_name in enumerate(field_names):
            value = record[i]
            # Convert datetime to string for JSON serialization
            if hasattr(value, 'isoformat'):
                value = value.isoformat()
            properties[field_name] = value

        # Convert geometry based on shape type
        geometry = None

        if shape.shapeType == 1:  # Point
            geometry = {
                "type": "Point",
                "coordinates": shape.points[0]
            }

        elif shape.shapeType == 3:  # Polyline
            if len(shape.parts) == 1:
                geometry = {
                    "type": "LineString",
                    "coordinates": shape.points
                }
            else:
                # MultiLineString
                lines = []
                for i in range(len(shape.parts)):
                    start = shape.parts[i]
                    end = shape.parts[i + 1] if i < len(shape.parts) - 1 else len(shape.points)
                    lines.append(shape.points[start:end])
                geometry = {
                    "type": "MultiLineString",
                    "coordinates": lines
                }

        elif shape.shapeType == 5:  # Polygon
            if len(shape.parts) == 1:
                # Simple polygon
                geometry = {
                    "type": "Polygon",
                    "coordinates": [shape.points]
                }
            else:
                # Polygon with holes or MultiPolygon
                rings = []
                for i in range(len(shape.parts)):
                    start = shape.parts[i]
                    end = shape.parts[i + 1] if i < len(shape.parts) - 1 else len(shape.points)
                    rings.append(shape.points[start:end])
                geometry = {
                    "type": "Polygon",
                    "coordinates": rings
                }

        # Create feature
        if geometry:
            feature = {
                "type": "Feature",
                "geometry": geometry,
                "properties": properties
            }
            geojson["features"].append(feature)

    # Write GeoJSON
    with open(geojson_path, 'w') as f:
        json.dump(geojson, f, indent=2)

    print(f"Converted {len(geojson['features'])} feature(s)")
    print(f"[OK] GeoJSON created: {geojson_path}")
    print()

    return geojson_path

def batch_convert():
    """Convert all lot boundary shapefiles in current directory."""
    print("=" * 80)
    print("BATCH CONVERT SHAPEFILES TO GEOJSON")
    print("=" * 80)
    print()

    current_dir = Path(__file__).parent
    shapefiles = list(current_dir.glob("lot_boundary*.shp"))

    if not shapefiles:
        print("No lot_boundary*.shp files found in current directory")
        return

    print(f"Found {len(shapefiles)} shapefile(s)")
    print()

    for shp_path in shapefiles:
        shp_to_geojson(shp_path)
        print()

    print("=" * 80)
    print("BATCH CONVERSION COMPLETE")
    print("=" * 80)
    print()
    print("GeoJSON files created:")
    for shp_path in shapefiles:
        geojson_path = shp_path.with_suffix('.geojson')
        print(f"  - {geojson_path.name}")
    print()
    print("USAGE IN GRASSHOPPER:")
    print("  1. Add GHPython component")
    print("  2. Copy code from: geojson_importer.py")
    print("  3. Set input 'filepath' to GeoJSON file path")
    print("  4. Outputs: curves, attributes")
    print()

if __name__ == "__main__":
    if len(sys.argv) > 1:
        # Single file conversion
        shp_path = sys.argv[1]
        geojson_path = sys.argv[2] if len(sys.argv) > 2 else None
        shp_to_geojson(shp_path, geojson_path)
    else:
        # Batch convert all lot boundaries
        batch_convert()
