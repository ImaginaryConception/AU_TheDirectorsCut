using System.Collections.Generic;
using UnityEngine;

namespace HydraMenu.ui
{
	internal class Styles
	{
		public enum UIColors
		{
			Azure,
			Carbon,
			Cardinal,
			Pesto,
			Pumpkin,
			White,
			Violet
		}

		public static Dictionary<UIColors, Color> ColorValues = new Dictionary<UIColors, Color>()
		{
			{ UIColors.Azure, new Color(0.0f, 0.50f, 1f) }, 
			{ UIColors.Carbon, new Color(0.07f, 0.07f, 0.07f) }, 
			{ UIColors.Cardinal, new Color(0.77f, 0.12f, 0.23f) }, 
			{ UIColors.Pesto, new Color(0.05f, 0.5f, 0.13f) }, 
			{ UIColors.Pumpkin, new Color(1.0f, 0.18f, 0.04f) }, 
			{ UIColors.White, new Color(0.95f, 0.95f, 0.97f) }, 
			{ UIColors.Violet, new Color(0.5f, 0f, 1f) } 
		};

		public static float menuOpacity = 0.85f;
		public static UIColors primaryColor = UIColors.Azure;

		private static Dictionary<string, Texture2D> CachedTextures = new Dictionary<string, Texture2D>();

		public static GUIStyle MainBox
		{
			get
			{
				GUIStyle style = new GUIStyle();

				Texture2D background = CreateColoredTexture("MainBox", ColorValues[UIColors.Carbon], menuOpacity);
				style.normal.background = background;

				style.normal.textColor = Color.white;
				style.alignment = TextAnchor.UpperCenter;
				style.padding.top = 5;
				
				
				
				style.fontSize = (int)(13 * MainUI.scale);

				return style;
			}
		}

		public static GUIStyle SectionBox
		{
			get
			{
				GUIStyle style = new GUIStyle();

				style.normal.textColor = ColorValues[UIColors.White];
				style.alignment = TextAnchor.MiddleLeft;
				style.padding.bottom = 1;
				style.padding.left = (int)(8 * MainUI.scale);
				style.fontSize = (int)(14 * MainUI.scale);

				return style;
			}
		}

		public static GUIStyle SectionBoxActive
		{
			get
			{
				GUIStyle style = new GUIStyle();

				Texture2D background = CreateColoredTexture("SectionBoxActive", ColorValues[primaryColor]);
				style.normal.background = background;

				style.normal.textColor = ColorValues[UIColors.White];
				style.alignment = TextAnchor.MiddleLeft;
				style.padding.bottom = 1;
				style.padding.left = (int)(13 * MainUI.scale);
				style.fontSize = (int)(MainUI.scale * 14);

				return style;
			}
		}

		public static GUIStyle PlayerBox
		{
			get
			{
				GUIStyle style = new GUIStyle();

				style.normal.textColor = ColorValues[UIColors.White];
				style.alignment = TextAnchor.MiddleLeft;
				style.clipping = TextClipping.Clip;
				style.padding.left = (int)(10 * MainUI.scale);
				style.richText = true;
				style.fontSize = (int)(13 * MainUI.scale);

				return style;
			}
		}

		public static GUIStyle PlayerBoxActive
		{
			get
			{
				GUIStyle style = new GUIStyle();

				Texture2D background = CreateColoredTexture("SectionBoxActive", ColorValues[primaryColor]);
				style.normal.background = background;

				style.normal.textColor = ColorValues[UIColors.White];
				style.alignment = TextAnchor.MiddleLeft;
				style.clipping = TextClipping.Clip;
				style.padding.left = (int)(10 * MainUI.scale);
				style.richText = true;
				style.fontSize = (int)(13 * MainUI.scale);

				return style;
			}
		}

		public static GUIStyle CreateCrewmateColorBox(string colorName, Color color)
		{
			GUIStyle style = new GUIStyle();

			Texture2D background = CreateColoredTexture(colorName, color);
			style.normal.background = background;

			return style;
		}

		private static Texture2D CreateColoredTexture(string textureName, Color color, float opacity = 1.0f)
		{
			CachedTextures.TryGetValue(textureName, out Texture2D background);
			if(background != null) return background;

			Hydra.Log.LogInfo($"Cache lookup for texture {textureName} returned a miss, creating the required texture...");

			background = new Texture2D(1, 1);
			background.SetPixel(0, 0, color.SetAlpha(opacity));
			background.Apply();

			CachedTextures[textureName] = background;
			return background;
		}

		public static void ClearCache()
		{
			foreach(Texture2D texture in CachedTextures.Values)
			{
				Texture2D.Destroy(texture);
			}
			CachedTextures.Clear();
		}
	}
}