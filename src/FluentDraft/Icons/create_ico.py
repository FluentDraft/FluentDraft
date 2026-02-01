from PIL import Image, ImageDraw

def make_transparent_and_create_ico(source_path, output_ico_path, output_png_path):
    img = Image.open(source_path)
    img = img.convert("RGBA")
    
    datas = img.getdata()
    
    # Simple strategy: Flood fill from corners if possible, or color replacement.
    # Since the user mentioned "white color which surrounds our icon", we assume the background is white.
    # We'll use a flood filling approach manually or using ImageDraw.
    
    # Alternative: Create a mask.
    # If the image is a rounded square on white, we can try to turn white to transparent.
    # However, the icon symbol is also white. So we must be careful.
    # We will assume the white to remove is connected to the corners.
    
    # Let's try Image.floodfill (available in newer Pillow) or a BFS approach if needed.
    # But first, let's see if we can just make "pure white" transparent if it's at the edges.
    
    # Robust approach:
    # 1. Create a mask initialized to 0.
    # 2. Flood fill the mask with 1 starting from (0,0) if (0,0) is white.
    # 3. Apply mask to alpha channel.
    
    width, height = img.size
    
    # check if corner is white-ish
    corner_pixel = img.getpixel((0, 0))
    # Threshold for "white"
    threshold = 240
    
    if all(x > threshold for x in corner_pixel[:3]):
        # The corner is white/near-white. We should make it transparent.
        # We will use a BFS flood fill to find all connected white pixels from the corners.
        
        # Create a new image for the mask
        mask = Image.new('L', (width, height), 0)
        
        # We'll do a custom flood fill on the alpha channel of the main image
        # logic: start at (0,0), (w,0), (0,h), (w,h). 
        # Stack based flood fill.
        
        stack = [(0, 0), (width-1, 0), (0, height-1), (width-1, height-1)]
        visited = set()
        
        # Get pixel access for speed
        pixels = img.load()
        
        while stack:
            x, y = stack.pop()
            
            if (x, y) in visited:
                continue
            
            if x < 0 or x >= width or y < 0 or y >= height:
                continue
                
            visited.add((x, y))
            
            p = pixels[x, y]
            # Check if pixel is white-ish
            if p[0] > threshold and p[1] > threshold and p[2] > threshold:
                # Make transparent
                pixels[x, y] = (255, 255, 255, 0)
                
                # Add neighbors
                stack.append((x+1, y))
                stack.append((x-1, y))
                stack.append((x, y+1))
                stack.append((x, y-1))
    
    # Resize and Save strategy
    sizes = [(256, 256), (128, 128), (64, 64), (48, 48), (32, 32), (16, 16)]
    
    # Save the modified PNG (with transparent background) back to source or a new file
    img.save(output_png_path, format="PNG")
    print(f"Saved transparent PNG to {output_png_path}")
    
    img.save(output_ico_path, format='ICO', sizes=sizes)
    print(f"Created {output_ico_path} with sizes: {sizes}")

if __name__ == "__main__":
    # We use the current png as source
    make_transparent_and_create_ico("app_icon.png", "app_icon.ico", "app_icon.png")
