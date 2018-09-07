using UnityEngine;
using System.Collections;

public class TextureRotate {


	public static Texture2D rotateTextureLeft(Texture2D tex) {
		int width = tex.width;
		int height = tex.height;
		
		Texture2D rotated = new Texture2D (height, width, TextureFormat.RGB24, false);
		
		for (int r = 0; r < width; r++)
			for (int c = 0; c < height; c++)
				rotated.SetPixel (height - 1 -c, r, tex.GetPixel(r, c));
		
		rotated.Apply();
		return rotated;
	}

	public static Texture2D rotateTextureRight(Texture2D tex) {
		int width = tex.width;
		int height = tex.height;
		
		Texture2D rotated = new Texture2D (height, width, TextureFormat.RGB24, false);
		
		for (int r = 0; r < width; r++)
			for (int c = 0; c < height; c++)
				rotated.SetPixel (c, width - 1 - r, tex.GetPixel(r, c));
		
		rotated.Apply();
		return rotated;
	}

}
