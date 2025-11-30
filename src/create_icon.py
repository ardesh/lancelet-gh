"""
Create Lancelet component icon (24x24 bitmap)
Simple lancelet shape - elongated oval/fish-like
"""

from PIL import Image, ImageDraw

# Create 24x24 image with transparency
img = Image.new('RGBA', (24, 24), (255, 255, 255, 0))
draw = ImageDraw.Draw(img)

# Draw lancelet body (elongated oval, pointed at both ends)
# Color: Blue-green (ocean/water theme)
body_color = (70, 130, 180, 255)  # Steel blue
outline_color = (40, 80, 120, 255)  # Darker blue

# Draw elongated ellipse for body
draw.ellipse([4, 8, 20, 16], fill=body_color, outline=outline_color)

# Add pointed ends (triangles)
# Left point (head)
draw.polygon([(4, 12), (2, 12), (4, 10), (4, 14)], fill=body_color, outline=outline_color)
# Right point (tail)
draw.polygon([(20, 12), (22, 12), (20, 10), (20, 14)], fill=body_color, outline=outline_color)

# Add simple segmentation marks (lancelet characteristic)
segment_color = (40, 80, 120, 200)
for x in range(7, 18, 3):
    draw.line([(x, 9), (x, 15)], fill=segment_color, width=1)

# Save as PNG and BMP
img.save('C:/Users/ardeshir/Documents/github/lancelet-gh/src/icon.png')

# Convert to 24-bit BMP for Grasshopper
img_bmp = img.convert('RGB')
img_bmp.save('C:/Users/ardeshir/Documents/github/lancelet-gh/src/icon.bmp')

print("Icon created:")
print("  - icon.png (with transparency)")
print("  - icon.bmp (for Grasshopper)")
print("\nIcon is 24x24 pixels, lancelet-shaped in blue")
