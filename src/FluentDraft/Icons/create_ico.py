from PIL import Image

def make_transparent_and_create_app_ico(source_path, output_ico_path):
    img = Image.open(source_path)
    img = img.convert("RGBA")
    
    datas = img.getdata()
    width, height = img.size
    
    # Check if corner is white-ish to decide on transparency
    try:
        corner_pixel = img.getpixel((0, 0))
        threshold = 240
        
        # Only attempt to make transparent if the corner is white and opaque
        if corner_pixel[3] > 0 and all(x > threshold for x in corner_pixel[:3]):
            print("Corner is white. Attempting to make background transparent...")
            
            # Create a new image for the mask logic (simplified for speed)
            # Actually, let's just use the flood fill logic from the original script if we want to be safe, 
            # or simpler: Replace all White pixels with Transparent? 
            # Original script used flood fill, let's replicate that to be safe.
            
            stack = [(0, 0), (width-1, 0), (0, height-1), (width-1, height-1)]
            visited = set()
            pixels = img.load()
            
            while stack:
                x, y = stack.pop()
                if (x, y) in visited: continue
                if x < 0 or x >= width or y < 0 or y >= height: continue
                
                visited.add((x, y))
                p = pixels[x, y]
                
                if p[0] > threshold and p[1] > threshold and p[2] > threshold:
                    pixels[x, y] = (255, 255, 255, 0) # Transparent
                    stack.extend([(x+1, y), (x-1, y), (x, y+1), (x, y-1)])
            
            # Save the modified PNG back? The original script did.
            # img.save(source_path, format="PNG") 
            # Let's not overwrite the source to avoid degradation if run repeatedly, 
            # unless desired. Original script overwrote `app_icon.png`.
            pass

    except Exception as e:
        print(f"Transparency processing skipped or failed: {e}")

    # Save App Icon
    sizes = [(256, 256), (128, 128), (64, 64), (48, 48), (32, 32), (16, 16)]
    img.save(output_ico_path, format='ICO', sizes=sizes)
    print(f"Created {output_ico_path}")


def create_recording_icon(source_path, output_ico_path):
    img = Image.open(source_path)
    img = img.convert("RGBA")
    
    datas = img.getdata()
    new_data = []
    threshold = 200 
    
    # Recolor White -> Red
    for item in datas:
        # item is (R, G, B, A)
        if item[0] > threshold and item[1] > threshold and item[2] > threshold:
            # Change to Red (Keeping Alpha of original pixel? Or forcing Opaque Red?)
            # If original was transparent, item[3] is 0.
            # If we keep item[3], transparent remains transparent red (invisible).
            # If original was White Opaque, it becomes Red Opaque.
            new_data.append((255, 0, 0, item[3]))
        else:
            new_data.append(item)
            
    img.putdata(new_data)
    
    sizes = [(256, 256), (128, 128), (64, 64), (48, 48), (32, 32), (16, 16)]
    img.save(output_ico_path, format='ICO', sizes=sizes)
    print(f"Created {output_ico_path}")

if __name__ == "__main__":
    source = "app_icon.png"
    print(f"Processing {source}...")
    make_transparent_and_create_app_ico(source, "app_icon.ico")
    create_recording_icon(source, "recording_icon.ico")
    print("Done.")
