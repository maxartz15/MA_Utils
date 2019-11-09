//https://github.com/maxartz15/Toolbox

//References:
//http://www.gamasutra.com/blogs/JoshSutphin/20131007/201829/Adding_to_Unitys_BuiltIn_Classes_Using_Extension_Methods.php
//https://forum.unity3d.com/threads/contribution-texture2d-blur-in-c.185694/
//http://orbcreation.com/orbcreation/page.orb?1180
//https://support.unity3d.com/hc/en-us/articles/206486626-How-can-I-get-pixels-from-unreadable-textures-
//https://github.com/maxartz15/TextureAtlasser/commit/9f5240967a51692fa2a17a6b3c8d124dd5dc60f9

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace Toolbox.Utils.Editor
{
    public static class TextureUtils
    {
        #region BaseFunctions
        public static Texture ConvertToReadableTexture(Texture texture)
        {
            if (texture == null)
                return texture;
            // Create a temporary RenderTexture of the same size as the texture
            RenderTexture tmp = RenderTexture.GetTemporary(
                                texture.width,
                                texture.height,
                                0,
                                RenderTextureFormat.Default,
                                RenderTextureReadWrite.Linear);

            // Blit the pixels on texture to the RenderTexture
            Graphics.Blit(texture, tmp);

            // Backup the currently set RenderTexture
            RenderTexture previous = RenderTexture.active;

            // Set the current RenderTexture to the temporary one we created
            RenderTexture.active = tmp;

            // Create a new readable Texture2D to copy the pixels to it
            Texture2D myTexture2D = new Texture2D(texture.width, texture.width);

            // Copy the pixels from the RenderTexture to the new Texture
            myTexture2D.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            myTexture2D.Apply();
            myTexture2D.name = texture.name;

            // Reset the active RenderTexture
            RenderTexture.active = previous;

            // Release the temporary RenderTexture
            RenderTexture.ReleaseTemporary(tmp);
            // "myTexture2D" now has the same pixels from "texture" and it's readable.

            return myTexture2D;
        }

        #region Save
        public static Texture2D Save2D(this Texture2D texture, string textureName, string savePath)
        {
            if (!Directory.Exists(savePath))
                Directory.CreateDirectory(savePath);

            FileStream fs = new FileStream(savePath + "/" + textureName + ".png", FileMode.Create);
            BinaryWriter bw = new BinaryWriter(fs);

            bw.Write(texture.EncodeToPNG());
            bw.Close();
            fs.Close();

            AssetDatabase.Refresh();

            return texture;
        }

        public static Texture Save(this Texture texture, string name, string savePath)
        {
            Texture2D texture2D = (Texture2D)TextureUtils.ConvertToReadableTexture(texture);

            texture2D.Save2D(name, savePath);

            texture = texture2D;

            return texture;
        }
        #endregion

        #region CopyProperties
        public static Texture2D CopyProperties(this Texture2D texture, Texture2D textureToCopy, bool name, bool alpha, bool filter, bool wrap)
        {
            if (name)
                texture.name = textureToCopy.name;
            if (alpha)
                texture.alphaIsTransparency = textureToCopy.alphaIsTransparency;
            if (filter)
                texture.filterMode = textureToCopy.filterMode;
            if (wrap)
                texture.wrapMode = textureToCopy.wrapMode;

            texture.Apply();
            return texture;
        }
        #endregion

        #region Scale
        public enum TextureScaleMode
		{
			Bilinear,
			Point
		}

        public static Texture Scale(this Texture texture, int width, int height, TextureScaleMode scaleMode)
        {
			Texture2D texture2D = (Texture2D)TextureUtils.ConvertToReadableTexture(texture);

			texture2D.Scale2D(width, height, scaleMode);

			texture = texture2D;

            return texture;
        }

		public static Texture2D Scale2D(this Texture2D texture, int newWidth, int newHeight, TextureScaleMode scaleMode)
		{
			Color[] curColors = texture.GetPixels();
			Color[] newColors = new Color[newWidth * newHeight];

			switch (scaleMode)
			{
				case TextureScaleMode.Bilinear:
					newColors = BilinearScale(curColors, texture.width, texture.height, newWidth, newHeight);
					break;
				case TextureScaleMode.Point:
					newColors = PointScale(curColors, texture.width, texture.height, newWidth, newHeight);
					break;

			}

			texture.Resize(newWidth, newHeight);
			texture.SetPixels(newColors);
			texture.Apply();

			return texture;
		}

		private static Color[] BilinearScale(Color[] curColors, int curWidth, int curHeight, int newWidth, int newHeight)
		{
			Color[] newColors = new Color[newWidth * newHeight];

			float ratioX = 1.0f / ((float)newWidth / (curWidth - 1));
			float ratioY = 1.0f / ((float)newHeight / (curHeight - 1));

			for (int y = 0; y < newHeight; y++)
			{
				int yFloor = Mathf.FloorToInt(y * ratioY);
				var y1 = yFloor * curWidth;
				var y2 = (yFloor + 1) * curWidth;
				var yw = y * newWidth;

				for (int x = 0; x < newWidth; x++)
				{
					int xFloor = Mathf.FloorToInt(x * ratioX);
					var xLerp = x * ratioX - xFloor;

					newColors[yw + x] = ColorLerpUnclamped(ColorLerpUnclamped(curColors[y1 + xFloor], curColors[y1 + xFloor + 1], xLerp),
														ColorLerpUnclamped(curColors[y2 + xFloor], curColors[y2 + xFloor + 1], xLerp),
														y * ratioY - yFloor);
				}
			}

			return newColors;
		}

		private static Color[] PointScale(Color[] curColors, int curWidth, int curHeight, int newWidth, int newHeight)
		{
			Color[] newColors = new Color[newWidth * newHeight];

			float ratioX = ((float)curWidth) / newWidth;
			float ratioY = ((float)curHeight) / newHeight;

			for (int y = 0; y < newHeight; y++)
			{
				var thisY = Mathf.RoundToInt((ratioY * y) * curWidth);
				var yw = y * newWidth;

				for (int x = 0; x < newWidth; x++)
				{
					newColors[yw + x] = curColors[Mathf.RoundToInt(thisY + ratioX * x)];
				}
			}

			return newColors;
		}

		private static Color ColorLerpUnclamped(Color c1, Color c2, float value)
		{
			return new Color(c1.r + (c2.r - c1.r) * value,
							  c1.g + (c2.g - c1.g) * value,
							  c1.b + (c2.b - c1.b) * value,
							  c1.a + (c2.a - c1.a) * value);
		}

		#endregion

		#region combine
		public static Texture2D Combine2D(this Texture2D texture, Texture2D combineTexture, int offsetX, int offsetY, bool flipY = true)
        {
            for (int x = 0; x < combineTexture.width; x++)
            {
                if(flipY)             
                {
                    //Y is 'flipped' because textures are made from left to right, bottom to top. We want to draw from left to right and top to bottom.
                    for (int y = combineTexture.height; y > 0; y--)
                    {
                        texture.SetPixel(x + offsetX, y + (texture.height - offsetY - combineTexture.height), combineTexture.GetPixel(x, y));
                    }
                }
                else
                {
                    for (int y = 0; y < combineTexture.height; y++)
                    {
                        texture.SetPixel(x + offsetX, y + offsetY, combineTexture.GetPixel(x, y));
                    }
                }
            }

            texture.Apply();

            return texture;
        }

        public static Texture Combine(this Texture texture, Texture combineTexture, int offsetX, int offsetY)
        {
            Texture2D texture2D = (Texture2D)TextureUtils.ConvertToReadableTexture(texture);
            Texture2D combineTexture2D = (Texture2D)TextureUtils.ConvertToReadableTexture(combineTexture);

            texture = texture2D.Combine2D(combineTexture2D, offsetX, offsetY);

            return texture;
        }
        #endregion

        #region Tile
        public static Texture2D Tile2D(this Texture2D texture, float tileValue)
        {
            Texture2D texture2D = texture;

            for (int x = 0; x < texture.width; x++)
            {
                for (int y = 0; y < texture.height; y++)
                {
                    Color pixel = texture.GetPixel(x, y);
                    int posX = Mathf.RoundToInt(x / tileValue);
                    int posY = Mathf.RoundToInt(y / tileValue);
                    texture.SetPixel(posX, posY, new Color(pixel.r, pixel.g, pixel.b, pixel.a));
                }
            }
            for (int i = 0; i < tileValue; i++)
            {
                for (int y = 0; y < tileValue; y++)
                {
                    Color[] pixels = texture.GetPixels(0, 0, Mathf.RoundToInt(texture2D.width / tileValue), Mathf.RoundToInt(texture2D.height / tileValue));
                    texture.SetPixels((texture2D.width / (int)tileValue) * y, (texture2D.height / (int)tileValue) * i, Mathf.RoundToInt(texture2D.width / tileValue), Mathf.RoundToInt(texture2D.height / tileValue), pixels);
                }
            }
            texture.Apply();

            return texture;
        }

        public static Texture Tile(this Texture texture, float tileValue)
        {
            Texture2D texture2D = (Texture2D)TextureUtils.ConvertToReadableTexture(texture);

            texture2D.Tile2D(tileValue);

            texture = texture2D;

            return texture;
        }
        #endregion

        #endregion

        #region Effects/Adjustments

        #region GrayScale
        public static Texture2D Grayscale2D(this Texture2D texture)
        {
            for (int m = 0; m < texture.mipmapCount; m++)
            {
                Color[] c = texture.GetPixels(m);
                for (int i = 0; i < c.Length; i++)
                {
                    var grayValue = c[i].grayscale;
                    c[i] = new Color(grayValue, grayValue, grayValue);

                    Mathf.Clamp(c[i].r, -1, 1);
                    Mathf.Clamp(c[i].g, -1, 1);
                    Mathf.Clamp(c[i].b, -1, 1);
                }
                texture.SetPixels(c, m);
            }
            texture.Apply();
            return texture;
        }

        public static Texture Grayscale(this Texture texture)
        {
            Texture2D texture2D = (Texture2D)TextureUtils.ConvertToReadableTexture(texture);

            texture2D.Grayscale2D();

            texture = texture2D;

            return texture;
        }
        #endregion

        #region Invert
        public static Texture2D Invert2D(this Texture2D texture, bool invertAlpha = false, bool invertAlphaOnly = false)
        {
            for (int m = 0; m < texture.mipmapCount; m++)
            {
                Color[] c = texture.GetPixels(m);
                for (int i = 0; i < c.Length; i++)
                {
                    if (!invertAlpha)
                    {
                        c[i].r = 1 - c[i].r;
                        c[i].g = 1 - c[i].g;
                        c[i].b = 1 - c[i].b;
                    }
                    else
                    {
                        if (!invertAlphaOnly)
                        {
                            c[i].r = 1 - c[i].r;
                            c[i].g = 1 - c[i].g;
                            c[i].b = 1 - c[i].b;
                            c[i].a = 1 - c[i].a;
                        }
                        else
                        {
                            c[i].a = 1 - c[i].a;
                        }
                    }
                }
                texture.SetPixels(c, m);
            }
            texture.Apply();
            return texture;
        }

        public static Texture Invert(this Texture texture, bool invertAlpha = false, bool invertAlphaOnly = false)
        {
            Texture2D texture2D = (Texture2D)TextureUtils.ConvertToReadableTexture(texture);

            texture2D.Invert2D(invertAlpha, invertAlphaOnly);

            texture = texture2D;

            return texture;
        }
        #endregion

        #region AlphaTexture
        public static Texture2D Alpha2D(this Texture2D texture)
        {
            for (int m = 0; m < texture.mipmapCount; m++)
            {
                Color[] c = texture.GetPixels(m);
                for (int i = 0; i < c.Length; i++)
                {
                    c[i].r = c[i].a;
                    c[i].g = c[i].a;
                    c[i].b = c[i].a;
                }
                texture.SetPixels(c, m);
            }
            texture.Apply();
            return texture;
        }

        public static Texture Alpha(this Texture texture)
        {
            Texture2D texture2D = (Texture2D)TextureUtils.ConvertToReadableTexture(texture);

            texture2D.Alpha2D();

            texture = texture2D;

            return texture;
        }
        #endregion

        #region BlendMultiply
        public static Texture2D BlendMultiply2D(this Texture2D texture, Texture2D blendTexture, float blendValue)
        {
            Color[] c = blendTexture.GetPixels();
            Color[] c2 = texture.GetPixels();
            for (int i = 0; i < c2.Length; i++)
            {
                c2[i].r = c2[i].r * c[i].r * blendValue;
                c2[i].g = c2[i].g * c[i].g * blendValue;
                c2[i].b = c2[i].b * c[i].b * blendValue;
            }
            texture.SetPixels(c2);
            texture.Apply();

            return texture;
        }

        public static Texture BlendMultiply(this Texture texture, Texture blendTexture, float blendValue)
        {
            Texture2D texture2D = (Texture2D)TextureUtils.ConvertToReadableTexture(texture);
            Texture2D blendTexture2D = (Texture2D)TextureUtils.ConvertToReadableTexture(blendTexture);

            texture2D.BlendMultiply2D(blendTexture2D, blendValue);

            texture = texture2D;

            return texture;
        }
        #endregion

        #region BlendScreen
        public static Texture2D BlendScreen2D(this Texture2D texture, Texture2D blendTexture, float blendValue)
        {
            Color[] c = blendTexture.GetPixels();
            Color[] c2 = texture.GetPixels();
            for (int i = 0; i < c2.Length; i++)
            {
                c2[i].r = (1 - ((1 - c[i].r) * (1 - c2[i].r))) / blendValue;
                c2[i].g = (1 - ((1 - c[i].g) * (1 - c2[i].g))) / blendValue;
                c2[i].b = (1 - ((1 - c[i].b) * (1 - c2[i].b))) / blendValue;
            }
            texture.SetPixels(c2);
            texture.Apply();

            return texture;
        }

        public static Texture BlendScreen(this Texture texture, Texture blendTexture, float blendValue)
        {
            Texture2D texture2D = (Texture2D)TextureUtils.ConvertToReadableTexture(texture);
            Texture2D blendTexture2D = (Texture2D)TextureUtils.ConvertToReadableTexture(blendTexture);

            texture2D.BlendMultiply(blendTexture2D, blendValue);

            texture = texture2D;

            return texture;
        }
        #endregion

        #region BlendOverlay
        public static Texture2D BlendOverlay2D(this Texture2D texture, Texture2D blendTexture, float blendValue)
        {
            Color[] c = blendTexture.GetPixels();
            Color[] c2 = texture.GetPixels();
            for (int i = 0; i < c2.Length; i++)
            {
                if (c2[i].r < 0.5f)
                    c2[i].r = (2 * ((c[i].r * c2[i].r) / blendValue));
                else
                    c2[i].r = (1 - (2 * (1 - c[i].r) * (1 - c2[i].r) / blendValue));

                if (c2[i].g < 0.5f)
                    c2[i].g = (2 * ((c[i].g * c2[i].g) / blendValue));
                else
                    c2[i].g = (1 - (2 * (1 - c[i].g) * (1 - c2[i].g) / blendValue));

                if (c2[i].b < 0.5f)
                    c2[i].b = (2 * ((c[i].b * c2[i].b) / blendValue));
                else
                    c2[i].b = (1 - (2 * (1 - c[i].b) * (1 - c2[i].b) / blendValue));
            }
            texture.SetPixels(c2);
            texture.Apply();

            return texture;
        }

        public static Texture BlendOverlay(this Texture texture, Texture blendTexture, float blendValue)
        {
            Texture2D texture2D = (Texture2D)TextureUtils.ConvertToReadableTexture(texture);
            Texture2D blendTexture2D = (Texture2D)TextureUtils.ConvertToReadableTexture(blendTexture);

            texture2D.BlendOverlay2D(blendTexture2D, blendValue);

            texture = texture2D;

            return texture;
        }
        #endregion

        #region BlendOpacety
        public static Texture2D BlendOpacety2D(this Texture2D texture, Texture2D blendTexture, float blendValue)
        {
            Color[] c = blendTexture.GetPixels();
            Color[] c2 = texture.GetPixels();
            for (int i = 0; i < c2.Length; i++)
            {
                c2[i].r = (1 - blendValue) * c2[i].r + blendValue * c[i].r;
                c2[i].g = (1 - blendValue) * c2[i].g + blendValue * c[i].g;
                c2[i].b = (1 - blendValue) * c2[i].b + blendValue * c[i].b;
            }
            texture.SetPixels(c2);
            texture.Apply();

            return texture;
        }

        public static Texture BlendOpacety(this Texture texture, Texture blendTexture, float blendValue)
        {
            Texture2D texture2D = (Texture2D)TextureUtils.ConvertToReadableTexture(texture);
            Texture2D blendTexture2D = (Texture2D)TextureUtils.ConvertToReadableTexture(blendTexture);

            texture2D.BlendOpacety2D(blendTexture2D, blendValue);

            texture = texture2D;

            return texture;
        }
        #endregion

        #region Brightness
        public static Texture2D Brightness2D(this Texture2D texture, float brightnessValue)
        {
            for (int m = 0; m < texture.mipmapCount; m++)
            {
                Color[] c = texture.GetPixels(m);
                for (int i = 0; i < c.Length; i++)
                {
                    if (c[i].a != 0) // keep alpha
                    {
                        c[i].r += brightnessValue;
                        Mathf.Clamp(c[i].r, -1, 1);
                        c[i].g += brightnessValue;
                        Mathf.Clamp(c[i].g, -1, 1);
                        c[i].b += brightnessValue;
                        Mathf.Clamp(c[i].b, -1, 1);
                    }
                }
                texture.SetPixels(c, m);
            }
            texture.Apply();

            return texture;
        }

        public static Texture Brightness(this Texture texture, float brightnessValue)
        {
            Texture2D texture2D = (Texture2D)TextureUtils.ConvertToReadableTexture(texture);

            texture2D.Brightness2D(brightnessValue);

            texture = texture2D;

            return texture;
        }
        #endregion

        #region ColorReplace
        public static Texture2D ColorReplace2D(this Texture2D texture, Color selectColor, Color newColor, float colorRangeMin = 0, float colorRangeMax = 0, bool alpha = false, bool red = true, bool green = true, bool blue = true)
        {
            for (int m = 0; m < texture.mipmapCount; m++)
            {
                Color[] c = texture.GetPixels(m);
                for (int i = 0; i < c.Length; i++)
                {
                    if (red && c[i].r > selectColor.r + colorRangeMin && c[i].r < selectColor.r + colorRangeMax)
                    {
                        c[i].r = newColor.r;
                    }
                    if (green && c[i].g > selectColor.g + colorRangeMin && c[i].g < selectColor.g + colorRangeMax)
                    {
                        c[i].g = newColor.g;
                    }
                    if (blue && c[i].b > selectColor.b + colorRangeMin && c[i].b < selectColor.b + colorRangeMax)
                    {
                        c[i].b = newColor.b;
                    }
                    if (alpha && c[i].a > selectColor.a + colorRangeMin && c[i].a < selectColor.a + colorRangeMax)
                    {
                        c[i].a = newColor.a;
                    }
                }
                texture.SetPixels(c, m);
            }
            texture.Apply();

            return texture;
        }

        public static Texture ColorReplace(this Texture texture, Color selectColor, Color newColor, float colorRangeMin = 0, float colorRangeMax = 0, bool alpha = false, bool red = true, bool green = true, bool blue = true)
        {
            Texture2D texture2D = (Texture2D)TextureUtils.ConvertToReadableTexture(texture);

            texture2D.ColorReplace2D(selectColor, newColor, colorRangeMin, colorRangeMax, alpha, red, green, blue);

            texture = texture2D;

            return texture;
        }

        public static Texture2D ColorReplaceHSL2D(this Texture2D texture, Color selectColor, Color newColor, bool useHue = false, float hueMin = 0, float hueMax = 0, bool useSat = false, float satMin = 0, float satMax = 0, bool useVal = false, float valMin = 0, float valMax = 0)
        {
            float nh;
            float ns;
            float nv;

            Color.RGBToHSV(newColor, out nh, out ns, out nv);

            float sh;
            float ss;
            float sv;

            Color.RGBToHSV(selectColor, out sh, out ss, out sv);

            for (int m = 0; m < texture.mipmapCount; m++)
            {
                Color[] c = texture.GetPixels(m);
                for (int i = 0; i < c.Length; i++)
                {
                    //HSV
                    float h;
                    float s;
                    float v;

                    Color.RGBToHSV(c[i], out h, out s, out v);

                    //HUE
                    if (useHue)
                    {
                        if (h <= sh + hueMin)
                        {
                            float dh = h - (sh + hueMin);
                            c[i] = Color.HSVToRGB(nh + dh, ns, nv);
                        }
                        else if (h >= sh + hueMin)
                        {
                            float dh = (sh + hueMax) - h;
                            c[i] = Color.HSVToRGB(nh + dh, ns, nv);
                        }
                    }

                    //SAT
                    if (useSat)
                    {
                        if (s <= ss + satMin)
                        {
                            float ds = s - (ss + satMin);
                            c[i] = Color.HSVToRGB(nh, ns + ds, nv);
                        }
                        else if (s >= ss + satMax)
                        {
                            float ds = (ss + satMax) - s;
                            c[i] = Color.HSVToRGB(nh, ns + ds, nv);
                        }
                    }

                    //VAL
                    if (useVal)
                    {
                        if (v <= sv + valMin)
                        {
                            float dv = v - (sv + valMin);
                            c[i] = Color.HSVToRGB(nh, ns, nv + dv);
                        }
                        else if (v >= sv + valMax)
                        {
                            float dv = (sv + valMax) - v;
                            c[i] = Color.HSVToRGB(nh, ns, nv + dv);
                        }
                    }
                }
                texture.SetPixels(c, m);
            }
            texture.Apply();

            return texture;
        }

        public static Texture ColorReplaceHSL(this Texture texture, Color selectColor, Color newColor, bool useHue = false, float hueMin = 0, float hueMax = 0, bool useSat = false, float satMin = 0, float satMax = 0, bool useVal = false, float valMin = 0, float valMax = 0)
        {
            Texture2D texture2D = (Texture2D)TextureUtils.ConvertToReadableTexture(texture);

            texture2D.ColorReplaceHSL2D(selectColor, newColor, useHue, hueMin, hueMax, useSat, satMin, satMax, useVal, valMin, valMax);

            texture = texture2D;

            return texture;
        }
        #endregion

        #region Contrast
        public static Texture2D Contrast2D(this Texture2D texture, float contrastValue)
        {
            for (int m = 0; m < texture.mipmapCount; m++)
            {
                Color[] c = texture.GetPixels(m);
                for (int i = 0; i < c.Length; i++)
                {
                    if (c[i].r > c[i].g && c[i].r > c[i].b)
                    {
                        c[i].r += contrastValue;
                    }
                    else if (c[i].g > c[i].r && c[i].g > c[i].b)
                    {
                        c[i].g += contrastValue;
                    }
                    else if (c[i].b > c[i].r && c[i].b > c[i].g)
                    {
                        c[i].b += contrastValue;
                    }
                    else
                    {
                        if (c[i].r <= 0.5f)
                            c[i].r += contrastValue;
                        else
                            c[i].r -= contrastValue;
                        if (c[i].g <= 0.5f)
                            c[i].g += contrastValue;
                        else
                            c[i].g -= contrastValue;
                        if (c[i].b <= 0.5f)
                            c[i].b += contrastValue;
                        else
                            c[i].b -= contrastValue;
                    }
                    Mathf.Clamp(c[i].r, -1, 1);
                    Mathf.Clamp(c[i].g, -1, 1);
                    Mathf.Clamp(c[i].b, -1, 1);
                }
                texture.SetPixels(c, m);
            }
            texture.Apply();

            return texture;
        }

        public static Texture Contrast(this Texture texture, float contrastValue)
        {
            Texture2D texture2D = (Texture2D)TextureUtils.ConvertToReadableTexture(texture);

            texture2D.Contrast2D(contrastValue);

            texture = texture2D;

            return texture;
        }
        #endregion

        #region Mask
        public static Texture2D Mask2D(this Texture2D texture, Texture2D maskTexture, bool grayscaleAplha = false)
        {
            Color[] c = maskTexture.GetPixels();
            Color[] c2 = texture.GetPixels();
            for (int i = 0; i < c2.Length; i++)
            {
                if (grayscaleAplha) //Grayscale Alpha
                {
                    c[i].a = c2[i].grayscale;
                }
                else //Alpha
                {
                    c[i].a = c2[i].a;
                }
            }
            texture.SetPixels(c);
            texture.alphaIsTransparency = true;
            texture.Apply();

            return texture;
        }

        public static Texture Mask(this Texture texture, Texture maskTexture, bool grayscaleAplha = false)
        {
            Texture2D texture2D = (Texture2D)TextureUtils.ConvertToReadableTexture(texture);
            Texture2D maskTexture2D = (Texture2D)TextureUtils.ConvertToReadableTexture(maskTexture);

            texture2D.Mask2D(maskTexture2D, grayscaleAplha);

            texture = texture2D;

            return texture;
        }
        #endregion

        #region MinMax
        public static Texture2D MinMax2D(this Texture2D texture, float min, float max, bool alpha = false, bool red = true, bool green = true, bool blue = true)
        {
            for (int m = 0; m < texture.mipmapCount; m++)
            {
                Color[] c = texture.GetPixels(m);
                for (int i = 0; i < c.Length; i++)
                {
                    if (c[i].r < min && red)
                    {
                        c[i].r = min;
                    }
                    if (c[i].g < min && green)
                    {
                        c[i].g = min;
                    }
                    if (c[i].b < min && blue)
                    {
                        c[i].b = min;
                    }
                    if (c[i].a < min && alpha)
                    {
                        c[i].a = min;
                    }

                    if (c[i].r > max && red)
                    {
                        c[i].r = max;
                    }
                    if (c[i].g > max && green)
                    {
                        c[i].g = max;
                    }
                    if (c[i].b > max && blue)
                    {
                        c[i].b = max;
                    }
                    if (c[i].a > max && alpha)
                    {
                        c[i].a = max;
                    }
                }
                texture.SetPixels(c, m);
            }
            texture.Apply();

            return texture;
        }

        public static Texture MinMax(this Texture texture, float min, float max, bool alpha = false, bool red = true, bool green = true, bool blue = true)
        {
            Texture2D texture2D = (Texture2D)TextureUtils.ConvertToReadableTexture(texture);

            texture2D.MinMax2D(min, max, alpha, red, green, blue);

            texture = texture2D;

            return texture;
        }
        #endregion

        #region Normal
        public static Texture2D Normal2D(this Texture2D texture, float intensety, bool up = true, bool unityNormalMap = true)
        {
            if (!up)
            {
                texture = texture.Grayscale2D();
                texture = texture.Invert2D();
            }

            Color[] pixels = texture.GetPixels();
            Color[] newPixels = new Color[pixels.Length];

            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    int x_1 = x - 1;
                    if (x_1 < 0) x_1 = texture.width - 1; // repeat the texture so use the opposit side
                    int x1 = x + 1;
                    if (x1 >= texture.width) x1 = 0; // repeat the texture so use the opposit side
                    int y_1 = y - 1;
                    if (y_1 < 0) y_1 = texture.height - 1; // repeat the texture so use the opposit side
                    int y1 = y + 1;
                    if (y1 >= texture.height) y1 = 0; // repeat the texture so use the opposit side
                    float grayX_1 = pixels[(y * texture.width) + x_1].grayscale;
                    float grayX1 = pixels[(y * texture.width) + x1].grayscale;
                    float grayY_1 = pixels[(y_1 * texture.width) + x].grayscale;
                    float grayY1 = pixels[(y1 * texture.width) + x].grayscale;
                    Vector3 vx = new Vector3(0, 1, (grayX_1 - grayX1) * intensety);
                    Vector3 vy = new Vector3(1, 0, (grayY_1 - grayY1) * intensety);
                    Vector3 n = Vector3.Cross(vy, vx).normalized;
                    newPixels[(y * texture.width) + x] = (Vector4)((n + Vector3.one) * 0.5f);
                }
            }

            texture.SetPixels(newPixels);
            texture.Apply();

            if (unityNormalMap)
                texture = texture.ToUnityNormal();

            return texture;
        }

        public static Texture Normal(this Texture texture, float intensety, bool up = true, bool unityNormalMap = true)
        {
            Texture2D texture2D = (Texture2D)TextureUtils.ConvertToReadableTexture(texture);

            texture = texture2D.Normal2D(intensety, up, unityNormalMap);

            return texture;
        }

        //Converting normal map to Unity format
        static Texture2D ToUnityNormal(this Texture2D texture)
        {
            Color[] pixels = texture.GetPixels();
            Color[] newPixels = new Color[pixels.Length];

            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    Color p = pixels[(y * texture.width) + x];
                    Color np = new Color(0, 0, 0, 1);
                    np.r = p.g;
                    np.g = p.r;
                    np.b = p.b;
                    newPixels[(y * texture.width) + x] = np;
                }
            }

            texture.SetPixels(newPixels);
            texture.Apply();

            return texture;
        }
        #endregion

        #region BoxBlur
        public static Texture2D BoxBlur2D(this Texture2D texture, int radius, int iterations = 1)
        {
            Texture2D texture2D = texture;
            List<Color> pixels = new List<Color>();
            List<Vector2> pos = new List<Vector2>();

            for (int i = 0; i < iterations; i++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    for (int y = 0; y < texture.height; y++)
                    {
                        pixels.Clear();
                        pos.Clear();

                        for (int rx = 0 - (radius / 2); rx < (radius / 2) + 1; rx++)
                        {
                            for (int ry = 0 - (radius / 2); ry < (radius / 2) + 1; ry++)
                            {
                                pixels.Add(texture2D.GetPixel(x + rx, y + ry));
                                pos.Add(new Vector2(rx, ry));
                            }
                        }

                        float averageRed = 0;
                        float averageGreen = 0;
                        float averageBlue = 0;

                        for (int r = 0; r < pixels.Count; r++)
                        {
                            averageRed += pixels[r].r;
                        }
                        for (int g = 0; g < pixels.Count; g++)
                        {
                            averageGreen += pixels[g].g;
                        }
                        for (int b = 0; b < pixels.Count; b++)
                        {
                            averageBlue += pixels[b].b;
                        }

                        averageRed = averageRed / pixels.Count;
                        averageGreen = averageGreen / pixels.Count;
                        averageBlue = averageBlue / pixels.Count;

                        Color newPixel = new Color(averageRed, averageGreen, averageBlue);

                        texture.SetPixel(x, y, newPixel);
                    }
                }
            }
            texture.Apply();

            return texture;
        }

        public static Texture BoxBlur(this Texture texture, int radius, int iterations = 1)
        {
            Texture2D texture2D = (Texture2D)TextureUtils.ConvertToReadableTexture(texture);

            texture2D.BoxBlur2D(radius, iterations);

            texture = texture2D;

            return texture;
        }
        #endregion

        #region Blur
        public static Texture2D Blur2D(this Texture2D texture, int radius, int iterations = 1)
        {
            List<Color> pixels = new List<Color>();
            List<Vector2> pos = new List<Vector2>();

            for (int i = 0; i < iterations; i++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    for (int y = 0; y < texture.height; y++)
                    {
                        pixels.Clear();
                        pos.Clear();

                        for (int rx = 0 - (radius / 2); rx < (radius / 2) + 1; rx++)
                        {
                            for (int ry = 0 - (radius / 2); ry < (radius / 2) + 1; ry++)
                            {
                                pixels.Add(texture.GetPixel(x + rx, y + ry));
                                pos.Add(new Vector2(rx, ry));
                            }
                        }

                        float averageRed = 0;
                        float averageGreen = 0;
                        float averageBlue = 0;

                        for (int r = 0; r < pixels.Count; r++)
                        {
                            averageRed += pixels[r].r;
                        }
                        for (int g = 0; g < pixels.Count; g++)
                        {
                            averageGreen += pixels[g].g;
                        }
                        for (int b = 0; b < pixels.Count; b++)
                        {
                            averageBlue += pixels[b].b;
                        }

                        averageRed = averageRed / pixels.Count;
                        averageGreen = averageGreen / pixels.Count;
                        averageBlue = averageBlue / pixels.Count;

                        Color newPixel = new Color(averageRed, averageGreen, averageBlue);

                        texture.SetPixel(x, y, newPixel);
                    }
                }
            }
            texture.Apply();

            return texture;
        }

        public static Texture Blur(this Texture texture, int radius, int iterations = 1)
        {
            Texture2D texture2D = (Texture2D)TextureUtils.ConvertToReadableTexture(texture);

            texture2D.Blur2D(radius, iterations);

            texture = texture2D;

            return texture;
        }
        #endregion

        #endregion

        #region Generate

        #region SolidColor
        public static Texture2D SolidColor2D(this Texture2D texture, Color color, int width, int height)
        {
            Texture2D texture2D = new Texture2D(width, height);

            Color[] p = new Color[texture2D.width * texture2D.height];

            for (int i = 0; i < p.Length; i++)
            {
                p[i] = color;
            }

            texture2D.SetPixels(p);

            texture2D.Apply();

            return texture2D;
        }

        public static Texture SolidColor(this Texture texture, Color color, int width, int height)
        {
            Texture2D texture2D = new Texture2D(width, height);

            texture = texture2D.SolidColor2D(color, width, height);

            return texture;
        }
        #endregion

        #region PerlinNoise
        public static Texture2D PerlinNoise2D(this Texture2D texture, int textureWidth, int textureHeight, float scale = 10, float xPos = 0, float yPos = 0)
        {
            //Create new texture
            texture = new Texture2D(textureWidth, textureHeight);
            Color[] p = texture.GetPixels();
            for (int i = 0; i < p.Length; i++)
                p[i] = Color.clear;
            texture.SetPixels(p);
            texture.Apply();

            //Calculate PerlinNoise.
            //Unity's build in PerlinNoise.
            Color[] c = p;
            float y = 0.0f;
            while (y < texture.height)
            {
                float x = 0.0f;
                while (x < texture.width)
                {
                    float xCoord = xPos + x / texture.width * scale;
                    float yCoord = yPos + y / texture.height * scale;
                    float sample = Mathf.PerlinNoise(xCoord, yCoord);
                    c[Mathf.RoundToInt(y * texture.width + x)] = new Color(sample, sample, sample);
                    x++;
                }
                y++;
            }
            texture.SetPixels(c);
            texture.Apply();
            return texture;
        }

        public static Texture PerlinNoise(this Texture texture, int textureWidth, int textureHeight, float scale = 10, float xPos = 0, float yPos = 0)
        {
            Texture2D texture2D = (Texture2D)TextureUtils.ConvertToReadableTexture(texture);

            texture = texture2D.PerlinNoise2D(textureWidth, textureHeight, scale, xPos, yPos);

            return texture;
        }
        #endregion

        #endregion
    }
}
#endif