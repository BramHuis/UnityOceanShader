from PIL import Image
import os
print(os.listdir())
# Open the images (ensure they are all grayscale)
red = Image.open('Assets/Normal_2_5_0_point_4_5.png').convert('L')   # Red channel (grayscale)
green = Image.open('Assets/Normal_4_15_0_point_5_6.png').convert('L')  # Green channel
blue = Image.open('Assets/Normal_6_30_0_point_6_7.png').convert('L')  # Blue channel
alpha = Image.open('Assets/Normal_8_60_0_point_7_8.png').convert('L')  # Alpha channel

# Ensure all images have the same size
width, height = red.size
assert green.size == (width, height)
assert blue.size == (width, height)
assert alpha.size == (width, height)

# Create a new image with RGBA mode (4 channels)
rgba_image = Image.merge("RGBA", (red, green, blue, alpha))

# Save the result
rgba_image.save('result.png')