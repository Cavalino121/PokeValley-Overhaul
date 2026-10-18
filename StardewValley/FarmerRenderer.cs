using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.Extensions;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Objects;
using StardewValley.Tools;

namespace StardewValley;

[InstanceStatics]
public class FarmerRenderer : INetObject<NetFields>
{
	public enum FarmerSpriteLayers
	{
		SlingshotUp,
		ToolUp,
		Base,
		Pants,
		FaceSkin,
		Eyes,
		Shirt,
		AccessoryUnderHair,
		ArmsUp,
		HatMaskUp,
		Hair,
		Accessory,
		Hat,
		Tool,
		Arms,
		ToolDown,
		Slingshot,
		PantsPassedOut,
		SwimWaterRing,
		MAX,
		TOOL_IN_USE_SIDE
	}

	public const int sleeveDarkestColorIndex = 256;

	public const int skinDarkestColorIndex = 260;

	public const int shoeDarkestColorIndex = 268;

	public const int eyeLightestColorIndex = 276;

	public const int accessoryDrawBelowHairThreshold = 8;

	public const int accessoryFacialHairThreshold = 6;

	protected bool _sickFrame;

	public static bool isDrawingForUI = false;

	public const int TransparentSkin = -12345;

	public const int pantsOffset = 288;

	public const int armOffset = 96;

	public const int shirtXOffset = 16;

	public const int shirtYOffset = 56;

	public static int[] featureYOffsetPerFrame = new int[126]
	{
		1, 2, 2, 0, 5, 6, 1, 2, 2, 1,
		0, 2, 0, 1, 1, 0, 2, 2, 3, 3,
		2, 2, 1, 1, 0, 0, 2, 2, 4, 4,
		0, 0, 1, 2, 1, 1, 1, 1, 0, 0,
		1, 1, 1, 0, 0, -2, -1, 1, 1, 0,
		-1, -2, -1, -1, 5, 4, 0, 0, 3, 2,
		-1, 0, 4, 2, 0, 0, 2, 1, 0, -1,
		1, -2, 0, 0, 1, 1, 1, 1, 1, 1,
		0, 0, 0, 0, 1, -1, -1, -1, -1, 1,
		1, 0, 0, 0, 0, 4, 1, 0, 1, 2,
		1, 0, 1, 0, 1, 2, -3, -4, -1, 0,
		0, 2, 1, -4, -1, 0, 0, -3, 0, 0,
		-1, 0, 0, 2, 1, 1
	};

	public static int[] featureXOffsetPerFrame = new int[126]
	{
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, -1, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, -1,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, -1, -1, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 4, 0, 0, 0, 0, -1, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, -1, 0, 0,
		0, 0, 0, 0, 0, 0
	};

	public static int[] hairstyleHatOffset = new int[16]
	{
		0, 0, 0, 4, 0, 0, 3, 0, 4, 0,
		0, 0, 0, 0, 0, 0
	};

	public static Texture2D hairStylesTexture;

	public static Texture2D shirtsTexture;

	public static Texture2D hatsTexture;

	public static Texture2D accessoriesTexture;

	public static Texture2D pantsTexture;

	public static Dictionary<string, Dictionary<int, List<int>>> recolorOffsets;

	[XmlElement("textureName")]
	public readonly NetString textureName = new NetString();

	[XmlIgnore]
	private LocalizedContentManager farmerTextureManager;

	[XmlIgnore]
	internal Texture2D baseTexture;

	[XmlElement("heightOffset")]
	public readonly NetInt heightOffset = new NetInt(0);

	[XmlIgnore]
	public readonly NetColor eyes = new NetColor();

	[XmlIgnore]
	public readonly NetInt skin = new NetInt();

	[XmlIgnore]
	public readonly NetString shoes = new NetString();

	[XmlIgnore]
	public readonly NetString shirt = new NetString();

	[XmlIgnore]
	public readonly NetString pants = new NetString();

	protected bool _spriteDirty;

	protected bool _baseTextureDirty;

	protected bool _eyesDirty;

	protected bool _skinDirty;

	protected bool _shoesDirty;

	protected bool _shirtDirty;

	protected bool _pantsDirty;

	public Rectangle shirtSourceRect;

	public Rectangle hairstyleSourceRect;

	public Rectangle hatSourceRect;

	public Rectangle accessorySourceRect;

	public Vector2 rotationAdjustment;

	public Vector2 positionOffset;

	[XmlIgnore]
	public NetFields NetFields { get; } = new NetFields("FarmerRenderer");

	public FarmerRenderer()
	{
		NetFields.SetOwner(this).AddField(textureName, "textureName").AddField(heightOffset, "heightOffset")
			.AddField(eyes, "eyes")
			.AddField(skin, "skin")
			.AddField(shoes, "shoes")
			.AddField(shirt, "shirt")
			.AddField(pants, "pants");
		farmerTextureManager = Game1.content.CreateTemporary();
		textureName.fieldChangeVisibleEvent += delegate
		{
			_spriteDirty = true;
			_baseTextureDirty = true;
		};
		eyes.fieldChangeVisibleEvent += delegate
		{
			_spriteDirty = true;
			_eyesDirty = true;
		};
		skin.fieldChangeVisibleEvent += delegate
		{
			_spriteDirty = true;
			_skinDirty = true;
			_shirtDirty = true;
		};
		shoes.fieldChangeVisibleEvent += delegate
		{
			_spriteDirty = true;
			_shoesDirty = true;
		};
		shirt.fieldChangeVisibleEvent += delegate
		{
			_spriteDirty = true;
			_shirtDirty = true;
		};
		pants.fieldChangeVisibleEvent += delegate
		{
			_spriteDirty = true;
			_pantsDirty = true;
		};
		_spriteDirty = true;
		_baseTextureDirty = true;
	}

	public FarmerRenderer(string textureName, Farmer farmer)
		: this()
	{
		eyes.Set(farmer.newEyeColor.Value);
		this.textureName.Set(textureName);
		_spriteDirty = true;
		_baseTextureDirty = true;
	}

	public bool isAccessoryFacialHair(int which)
	{
		if (which >= 6)
		{
			if (which >= 19)
			{
				return which <= 22;
			}
			return false;
		}
		return true;
	}

	public bool drawAccessoryBelowHair(int which)
	{
		if (which >= 8)
		{
			return isAccessoryFacialHair(which);
		}
		return true;
	}

	private void executeRecolorActions(Farmer farmer)
	{
		if (_spriteDirty)
		{
			_spriteDirty = false;
			if (_baseTextureDirty)
			{
				_baseTextureDirty = false;
				textureChanged();
				_eyesDirty = true;
				_shoesDirty = true;
				_pantsDirty = true;
				_skinDirty = true;
				_shirtDirty = true;
			}
			if (recolorOffsets == null)
			{
				recolorOffsets = new Dictionary<string, Dictionary<int, List<int>>>();
			}
			if (!recolorOffsets.ContainsKey(textureName.Value))
			{
				recolorOffsets[textureName.Value] = new Dictionary<int, List<int>>();
				Texture2D texture2D = farmerTextureManager.Load<Texture2D>(textureName.Value);
				Color[] array = new Color[texture2D.Width * texture2D.Height];
				texture2D.GetData(array);
				_GeneratePixelIndices(256, textureName.Value, array);
				_GeneratePixelIndices(257, textureName.Value, array);
				_GeneratePixelIndices(258, textureName.Value, array);
				_GeneratePixelIndices(268, textureName.Value, array);
				_GeneratePixelIndices(269, textureName.Value, array);
				_GeneratePixelIndices(270, textureName.Value, array);
				_GeneratePixelIndices(271, textureName.Value, array);
				_GeneratePixelIndices(260, textureName.Value, array);
				_GeneratePixelIndices(261, textureName.Value, array);
				_GeneratePixelIndices(262, textureName.Value, array);
				_GeneratePixelIndices(276, textureName.Value, array);
				_GeneratePixelIndices(277, textureName.Value, array);
			}
			Color[] array2 = new Color[baseTexture.Width * baseTexture.Height];
			baseTexture.GetData(array2);
			if (_eyesDirty)
			{
				_eyesDirty = false;
				ApplyEyeColor(textureName.Value, array2);
			}
			if (_skinDirty)
			{
				_skinDirty = false;
				ApplySkinColor(textureName.Value, array2);
			}
			if (_shoesDirty)
			{
				_shoesDirty = false;
				ApplyShoeColor(textureName.Value, array2);
			}
			if (_shirtDirty)
			{
				_shirtDirty = false;
				ApplySleeveColor(textureName.Value, array2, farmer);
			}
			if (_pantsDirty)
			{
				_pantsDirty = false;
			}
			baseTexture.SetData(array2);
		}
	}

	protected void _GeneratePixelIndices(int source_color_index, string texture_name, Color[] pixels)
	{
		Color color = pixels[source_color_index];
		List<int> list = new List<int>();
		for (int i = 0; i < pixels.Length; i++)
		{
			if (pixels[i].PackedValue == color.PackedValue)
			{
				list.Add(i);
			}
		}
		recolorOffsets[texture_name][source_color_index] = list;
	}

	public void unload()
	{
		farmerTextureManager.Unload();
		farmerTextureManager.Dispose();
	}

	public void textureChanged()
	{
		if (baseTexture != null)
		{
			baseTexture.Dispose();
			baseTexture = null;
		}
		Texture2D texture2D = farmerTextureManager.Load<Texture2D>(textureName.Value);
		baseTexture = new Texture2D(Game1.graphics.GraphicsDevice, texture2D.GetActualWidth(), texture2D.GetActualHeight())
		{
			Name = "@FarmerRenderer.baseTexture"
		};
		Color[] array = new Color[texture2D.GetElementCount()];
		texture2D.GetData(array, 0, array.Length);
		baseTexture.SetData(array);
	}

	public void recolorEyes(Color lightestColor)
	{
		eyes.Set(lightestColor);
	}

	public void ApplyEyeColor(string texture_name, Color[] pixels)
	{
		Color value = eyes.Value;
		Color color = changeBrightness(value, -75);
		if (value.Equals(color))
		{
			value.B += 10;
		}
		_SwapColor(texture_name, pixels, 276, value);
		_SwapColor(texture_name, pixels, 277, color);
	}

	private void _SwapColor(string texture_name, Color[] pixels, int color_index, Color color)
	{
		foreach (int item in recolorOffsets[texture_name][color_index])
		{
			pixels[item] = color;
		}
	}

	public void recolorShoes(string which)
	{
		shoes.Set(which);
	}

	private void ApplyShoeColor(string texture_name, Color[] pixels)
	{
		int result = 12;
		Texture2D texture2D = null;
		int num = shoes.Value.LastIndexOf(':');
		if (num > -1)
		{
			string assetName = shoes.Value.Substring(0, num);
			string s = shoes.Value.Substring(num + 1);
			try
			{
				texture2D = farmerTextureManager.Load<Texture2D>(assetName);
				if (!int.TryParse(s, out result))
				{
					result = 12;
				}
			}
			catch (Exception)
			{
				texture2D = farmerTextureManager.Load<Texture2D>("Characters\\Farmer\\shoeColors");
			}
		}
		else if (!int.TryParse(shoes.Value, out result))
		{
			result = 12;
		}
		if (texture2D == null)
		{
			texture2D = farmerTextureManager.Load<Texture2D>("Characters\\Farmer\\shoeColors");
		}
		Texture2D texture2D2 = texture2D;
		if (result >= texture2D2.Height)
		{
			result = texture2D2.Height - 1;
		}
		if (texture2D2.Width >= 4)
		{
			Color[] array = new Color[texture2D2.Width * texture2D2.Height];
			texture2D2.GetData(array);
			Color color = array[result * 4 % (texture2D2.Height * 4)];
			Color color2 = array[result * 4 % (texture2D2.Height * 4) + 1];
			Color color3 = array[result * 4 % (texture2D2.Height * 4) + 2];
			Color color4 = array[result * 4 % (texture2D2.Height * 4) + 3];
			_SwapColor(texture_name, pixels, 268, color);
			_SwapColor(texture_name, pixels, 269, color2);
			_SwapColor(texture_name, pixels, 270, color3);
			_SwapColor(texture_name, pixels, 271, color4);
		}
	}

	public int recolorSkin(int which, bool force = false)
	{
		if (force)
		{
			skin.Value = -1;
		}
		skin.Set(which);
		return which;
	}

	private void ApplySkinColor(string texture_name, Color[] pixels)
	{
		int num = skin.Value;
		Texture2D texture2D = farmerTextureManager.Load<Texture2D>("Characters\\Farmer\\skinColors");
		Color[] array = new Color[texture2D.Width * texture2D.Height];
		if (num < 0)
		{
			num = texture2D.Height - 1;
		}
		if (num > texture2D.Height - 1)
		{
			num = 0;
		}
		texture2D.GetData(array);
		Color color = array[num * 3 % (texture2D.Height * 3)];
		Color color2 = array[num * 3 % (texture2D.Height * 3) + 1];
		Color color3 = array[num * 3 % (texture2D.Height * 3) + 2];
		if (skin.Value == -12345)
		{
			color = (color2 = (color3 = Color.Transparent));
		}
		_SwapColor(texture_name, pixels, 260, color);
		_SwapColor(texture_name, pixels, 261, color2);
		_SwapColor(texture_name, pixels, 262, color3);
	}

	public void changeShirt(string whichShirt)
	{
		shirt.Set(whichShirt);
	}

	public void changePants(string whichPants)
	{
		pants.Set(whichPants);
	}

	public void MarkSpriteDirty()
	{
		_spriteDirty = true;
		_shirtDirty = true;
		_pantsDirty = true;
		_eyesDirty = true;
		_shoesDirty = true;
		_baseTextureDirty = true;
	}

	public void ApplySleeveColor(string texture_name, Color[] pixels, Farmer who)
	{
		who.GetDisplayShirt(out var texture, out var spriteIndex);
		Color[] array = new Color[texture.Bounds.Width * texture.Bounds.Height];
		texture.GetData(array);
		int num = spriteIndex * 8 / 128 * 32 * texture.Bounds.Width + spriteIndex * 8 % 128 + texture.Width * 4;
		int num2 = num + 128;
		if (!who.ShirtHasSleeves() || num >= array.Length || (skin.Value == -12345 && who.shirtItem.Value == null))
		{
			Texture2D texture2D = farmerTextureManager.Load<Texture2D>("Characters\\Farmer\\skinColors");
			Color[] array2 = new Color[texture2D.Width * texture2D.Height];
			int num3 = skin.Value;
			if (num3 < 0)
			{
				num3 = texture2D.Height - 1;
			}
			if (num3 > texture2D.Height - 1)
			{
				num3 = 0;
			}
			texture2D.GetData(array2);
			Color color = array2[num3 * 3 % (texture2D.Height * 3)];
			Color color2 = array2[num3 * 3 % (texture2D.Height * 3) + 1];
			Color color3 = array2[num3 * 3 % (texture2D.Height * 3) + 2];
			if (skin.Value == -12345)
			{
				color = pixels[260 + baseTexture.Width * 2];
				color2 = pixels[261 + baseTexture.Width * 2];
				color3 = pixels[262 + baseTexture.Width * 2];
			}
			if (_sickFrame)
			{
				color = pixels[260 + baseTexture.Width];
				color2 = pixels[261 + baseTexture.Width];
				color3 = pixels[262 + baseTexture.Width];
			}
			_SwapColor(texture_name, pixels, 256, color);
			_SwapColor(texture_name, pixels, 257, color2);
			_SwapColor(texture_name, pixels, 258, color3);
		}
		else
		{
			Color color4 = Utility.MakeCompletelyOpaque(who.GetShirtColor());
			Color a = array[num2];
			Color b = color4;
			if (a.A < byte.MaxValue)
			{
				a = array[num];
				b = Color.White;
			}
			a = Utility.MultiplyColor(a, b);
			_SwapColor(texture_name, pixels, 256, a);
			a = array[num2 - texture.Width];
			if (a.A < byte.MaxValue)
			{
				a = array[num - texture.Width];
				b = Color.White;
			}
			a = Utility.MultiplyColor(a, b);
			_SwapColor(texture_name, pixels, 257, a);
			a = array[num2 - texture.Width * 2];
			if (a.A < byte.MaxValue)
			{
				a = array[num - texture.Width * 2];
				b = Color.White;
			}
			a = Utility.MultiplyColor(a, b);
			_SwapColor(texture_name, pixels, 258, a);
		}
	}

	public static Color changeBrightness(Color c, int brightness)
	{
		c.R = (byte)Math.Min(255, Math.Max(0, c.R + brightness));
		c.G = (byte)Math.Min(255, Math.Max(0, c.G + brightness));
		c.B = (byte)Math.Min(255, Math.Max(0, c.B + ((brightness > 0) ? (brightness * 5 / 6) : (brightness * 8 / 7))));
		return c;
	}

	public void draw(SpriteBatch b, Farmer who, int whichFrame, Vector2 position, float layerDepth = 1f, bool flip = false)
	{
		who.FarmerSprite.setCurrentSingleFrame(whichFrame, 32000, secondaryArm: false, flip);
		draw(b, who.FarmerSprite, who.FarmerSprite.SourceRect, position, Vector2.Zero, layerDepth, Color.White, 0f, who);
	}

	public void draw(SpriteBatch b, FarmerSprite farmerSprite, Rectangle sourceRect, Vector2 position, Vector2 origin, float layerDepth, Color overrideColor, float rotation, Farmer who)
	{
		draw(b, farmerSprite.CurrentAnimationFrame, farmerSprite.CurrentFrame, sourceRect, position, origin, layerDepth, overrideColor, rotation, 1f, who);
	}

	public void drawMiniPortrat(SpriteBatch b, Vector2 position, float layerDepth, float scale, int facingDirection, Farmer who, float alpha = 1f)
	{
		int hair = who.getHair(ignore_hat: true);
		executeRecolorActions(who);
		facingDirection = 2;
		bool flag = false;
		int y = 0;
		int num = 0;
		HairStyleMetadata hairStyleMetadata = Farmer.GetHairStyleMetadata(who.hair.Value);
		Texture2D texture = hairStyleMetadata?.texture ?? hairStylesTexture;
		hairstyleSourceRect = ((hairStyleMetadata != null) ? new Rectangle(hairStyleMetadata.tileX * 16, hairStyleMetadata.tileY * 16, 16, 15) : new Rectangle(hair * 16 % hairStylesTexture.Width, hair * 16 / hairStylesTexture.Width * 96, 16, 15));
		if (facingDirection == 2)
		{
			y = 0;
			hairstyleSourceRect.Offset(0, 0);
			num = featureYOffsetPerFrame[0];
		}
		b.Draw(baseTexture, position, new Rectangle(0, y, 16, who.IsMale ? 15 : 16), Color.White * alpha, 0f, Vector2.Zero, scale, flag ? SpriteEffects.FlipHorizontally : SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Base));
		Color color = (who.prismaticHair.Value ? Utility.GetPrismaticColor() : who.hairstyleColor.Value);
		b.Draw(texture, position + new Vector2(0f, num * 4 + ((who.IsMale && who.hair.Value >= 16) ? (-4) : ((!who.IsMale && who.hair.Value < 16) ? 4 : 0))) * scale / 4f, hairstyleSourceRect, color * alpha, 0f, Vector2.Zero, scale, flag ? SpriteEffects.FlipHorizontally : SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Hair));
	}

	public void draw(SpriteBatch b, FarmerSprite.AnimationFrame animationFrame, int currentFrame, Rectangle sourceRect, Vector2 position, Vector2 origin, float layerDepth, Color overrideColor, float rotation, float scale, Farmer who)
	{
		draw(b, animationFrame, currentFrame, sourceRect, position, origin, layerDepth, who.FacingDirection, overrideColor, rotation, scale, who);
	}

	public void drawHairAndAccesories(SpriteBatch b, int facingDirection, Farmer who, Vector2 position, Vector2 origin, float scale, int currentFrame, float rotation, Color overrideColor, float layerDepth)
	{
		int num = who.getHair();
		float num2 = 4f * scale;
		int num3 = featureXOffsetPerFrame[currentFrame];
		int num4 = featureYOffsetPerFrame[currentFrame];
		HairStyleMetadata hairStyleMetadata = Farmer.GetHairStyleMetadata(num);
		Hat value = who.hat.Value;
		if (value != null && value.hairDrawType.Value == 1 && hairStyleMetadata != null && hairStyleMetadata.coveredIndex != -1)
		{
			num = hairStyleMetadata.coveredIndex;
			hairStyleMetadata = Farmer.GetHairStyleMetadata(num);
		}
		executeRecolorActions(who);
		who.GetDisplayShirt(out var texture, out var spriteIndex);
		Color color = (who.prismaticHair.Value ? Utility.GetPrismaticColor() : who.hairstyleColor.Value);
		shirtSourceRect = new Rectangle(spriteIndex * 8 % 128, spriteIndex * 8 / 128 * 32, 8, 8);
		Texture2D texture2 = hairStyleMetadata?.texture ?? hairStylesTexture;
		hairstyleSourceRect = ((hairStyleMetadata != null) ? new Rectangle(hairStyleMetadata.tileX * 16, hairStyleMetadata.tileY * 16, 16, 32) : new Rectangle(num * 16 % hairStylesTexture.Width, num * 16 / hairStylesTexture.Width * 96, 16, 32));
		if (who.accessory.Value >= 0)
		{
			accessorySourceRect = new Rectangle(who.accessory.Value * 16 % accessoriesTexture.Width, who.accessory.Value * 16 / accessoriesTexture.Width * 32, 16, 16);
		}
		Texture2D texture3 = hatsTexture;
		bool flag = false;
		if (who.hat.Value != null)
		{
			ParsedItemData dataOrErrorItem = ItemRegistry.GetDataOrErrorItem(who.hat.Value.QualifiedItemId);
			int spriteIndex2 = dataOrErrorItem.SpriteIndex;
			texture3 = dataOrErrorItem.GetTexture();
			hatSourceRect = new Rectangle(20 * spriteIndex2 % texture3.Width, 20 * spriteIndex2 / texture3.Width * 20 * 4, 20, 20);
			if (dataOrErrorItem.IsErrorItem)
			{
				hatSourceRect = dataOrErrorItem.GetSourceRect();
				flag = true;
			}
		}
		FarmerSpriteLayers layer = FarmerSpriteLayers.Accessory;
		if (who.accessory.Value >= 0 && drawAccessoryBelowHair(who.accessory.Value))
		{
			layer = FarmerSpriteLayers.AccessoryUnderHair;
		}
		switch (facingDirection)
		{
		case 0:
		{
			shirtSourceRect.Offset(0, 24);
			hairstyleSourceRect.Offset(0, 64);
			Rectangle value2 = shirtSourceRect;
			value2.Offset(128, 0);
			if (!flag && who.hat.Value != null)
			{
				hatSourceRect.Offset(0, 60);
			}
			if (!who.bathingClothes.Value && (skin.Value != -12345 || who.shirtItem.Value != null))
			{
				Vector2 position4 = position + origin + positionOffset + new Vector2(16f * scale + (float)(num3 * 4), (float)(56 + num4 * 4) + (float)heightOffset.Value * scale);
				b.Draw(texture, position4, shirtSourceRect, overrideColor.Equals(Color.White) ? Color.White : overrideColor, rotation, origin, num2, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Shirt));
				b.Draw(texture, position4, value2, overrideColor.Equals(Color.White) ? Utility.MakeCompletelyOpaque(who.GetShirtColor()) : overrideColor, rotation, origin, num2, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Shirt, dyeLayer: true));
			}
			b.Draw(texture2, position + origin + positionOffset + new Vector2(num3 * 4, num4 * 4 + 4 + ((who.IsMale && num >= 16) ? (-4) : ((!who.IsMale && num < 16) ? 4 : 0))), hairstyleSourceRect, overrideColor.Equals(Color.White) ? color : overrideColor, rotation, origin, num2, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Hair));
			break;
		}
		case 1:
		{
			shirtSourceRect.Offset(0, 8);
			hairstyleSourceRect.Offset(0, 32);
			Rectangle value2 = shirtSourceRect;
			value2.Offset(128, 0);
			if (!flag && who.hat.Value != null)
			{
				hatSourceRect.Offset(0, 20);
			}
			if (rotation != -(float)Math.PI / 32f)
			{
				if (rotation == (float)Math.PI / 32f)
				{
					rotationAdjustment.X = -6f;
					rotationAdjustment.Y = 1f;
				}
			}
			else
			{
				rotationAdjustment.X = 6f;
				rotationAdjustment.Y = -2f;
			}
			if (!who.bathingClothes.Value && (skin.Value != -12345 || who.shirtItem.Value != null))
			{
				Vector2 position5 = position + origin + positionOffset + rotationAdjustment + new Vector2(16f * scale + (float)(num3 * 4), 56f * scale + (float)(num4 * 4) + (float)heightOffset.Value * scale);
				b.Draw(texture, position5, shirtSourceRect, overrideColor.Equals(Color.White) ? Color.White : overrideColor, rotation, origin, num2, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Shirt));
				b.Draw(texture, position5, value2, overrideColor.Equals(Color.White) ? Utility.MakeCompletelyOpaque(who.GetShirtColor()) : overrideColor, rotation, origin, num2, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Shirt, dyeLayer: true));
			}
			if (who.accessory.Value >= 0)
			{
				accessorySourceRect.Offset(0, 16);
				b.Draw(accessoriesTexture, position + origin + positionOffset + rotationAdjustment + new Vector2(num3 * 4, 4 + num4 * 4 + heightOffset.Value), accessorySourceRect, (overrideColor.Equals(Color.White) && isAccessoryFacialHair(who.accessory.Value)) ? color : overrideColor, rotation, origin, num2, SpriteEffects.None, GetLayerDepth(layerDepth, layer));
			}
			b.Draw(texture2, position + origin + positionOffset + new Vector2(num3 * 4, num4 * 4 + ((who.IsMale && who.hair.Value >= 16) ? (-4) : ((!who.IsMale && who.hair.Value < 16) ? 4 : 0))), hairstyleSourceRect, overrideColor.Equals(Color.White) ? color : overrideColor, rotation, origin, num2, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Hair));
			break;
		}
		case 2:
		{
			Rectangle value2 = shirtSourceRect;
			value2.Offset(128, 0);
			if (!who.bathingClothes.Value && (skin.Value != -12345 || who.shirtItem.Value != null))
			{
				Vector2 position3 = position + origin + positionOffset + new Vector2(16 + num3 * 4, (float)(56 + num4 * 4) + (float)heightOffset.Value * scale);
				b.Draw(texture, position3, shirtSourceRect, overrideColor.Equals(Color.White) ? Color.White : overrideColor, rotation, origin, num2, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Shirt));
				b.Draw(texture, position3, value2, overrideColor.Equals(Color.White) ? Utility.MakeCompletelyOpaque(who.GetShirtColor()) : overrideColor, rotation, origin, num2, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Shirt, dyeLayer: true));
			}
			if (who.accessory.Value >= 0)
			{
				if (who.accessory.Value == 26)
				{
					switch (currentFrame)
					{
					case 24:
					case 25:
					case 26:
					case 70:
						positionOffset.Y += 4f;
						break;
					}
				}
				b.Draw(accessoriesTexture, position + origin + positionOffset + rotationAdjustment + new Vector2(num3 * 4, 8 + num4 * 4 + heightOffset.Value - 4), accessorySourceRect, (overrideColor.Equals(Color.White) && isAccessoryFacialHair(who.accessory.Value)) ? color : overrideColor, rotation, origin, num2, SpriteEffects.None, GetLayerDepth(layerDepth, layer));
			}
			b.Draw(texture2, position + origin + positionOffset + new Vector2(num3 * 4, num4 * 4 + ((who.IsMale && who.hair.Value >= 16) ? (-4) : ((!who.IsMale && who.hair.Value < 16) ? 4 : 0))), hairstyleSourceRect, overrideColor.Equals(Color.White) ? color : overrideColor, rotation, origin, num2, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Hair));
			break;
		}
		case 3:
		{
			bool flag2 = true;
			shirtSourceRect.Offset(0, 16);
			Rectangle value2 = shirtSourceRect;
			value2.Offset(128, 0);
			if (hairStyleMetadata != null && hairStyleMetadata.usesUniqueLeftSprite)
			{
				flag2 = false;
				hairstyleSourceRect.Offset(0, 96);
			}
			else
			{
				hairstyleSourceRect.Offset(0, 32);
			}
			if (!flag && who.hat.Value != null)
			{
				hatSourceRect.Offset(0, 40);
			}
			if (rotation != -(float)Math.PI / 32f)
			{
				if (rotation == (float)Math.PI / 32f)
				{
					rotationAdjustment.X = -5f;
					rotationAdjustment.Y = 1f;
				}
			}
			else
			{
				rotationAdjustment.X = 6f;
				rotationAdjustment.Y = -2f;
			}
			if (!who.bathingClothes.Value && (skin.Value != -12345 || who.shirtItem.Value != null))
			{
				Vector2 position2 = position + origin + positionOffset + rotationAdjustment + new Vector2(16f * scale - (float)(num3 * 4), 56f * scale + (float)(num4 * 4) + (float)heightOffset.Value * scale);
				b.Draw(texture, position2, shirtSourceRect, overrideColor.Equals(Color.White) ? Color.White : overrideColor, rotation, origin, num2, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Shirt));
				b.Draw(texture, position2, value2, overrideColor.Equals(Color.White) ? Utility.MakeCompletelyOpaque(who.GetShirtColor()) : overrideColor, rotation, origin, num2, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Shirt, dyeLayer: true));
			}
			if (who.accessory.Value >= 0)
			{
				accessorySourceRect.Offset(0, 16);
				b.Draw(accessoriesTexture, position + origin + positionOffset + rotationAdjustment + new Vector2(-num3 * 4, 4 + num4 * 4 + heightOffset.Value), accessorySourceRect, (overrideColor.Equals(Color.White) && isAccessoryFacialHair(who.accessory.Value)) ? color : overrideColor, rotation, origin, num2, SpriteEffects.FlipHorizontally, GetLayerDepth(layerDepth, layer));
			}
			b.Draw(texture2, position + origin + positionOffset + new Vector2(-num3 * 4, num4 * 4 + ((who.IsMale && who.hair.Value >= 16) ? (-4) : ((!who.IsMale && who.hair.Value < 16) ? 4 : 0))), hairstyleSourceRect, overrideColor.Equals(Color.White) ? color : overrideColor, rotation, origin, num2, flag2 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Hair));
			break;
		}
		}
		if (who.hat.Value != null && !who.bathingClothes.Value)
		{
			bool flip = who.FarmerSprite.CurrentAnimationFrame.flip;
			int num5 = ((!who.hat.Value.ignoreHairstyleOffset.Value) ? hairstyleHatOffset[who.hair.Value % 16] : 0);
			Vector2 position6 = position + origin + positionOffset + new Vector2((0f - num2) * 2f + (float)(((!flip) ? 1 : (-1)) * num3) * num2, (0f - num2) * 4f + (float)(num4 * 4) + (float)num5 + 4f + (float)heightOffset.Value);
			Color color2 = (who.hat.Value.isPrismatic.Value ? Utility.GetPrismaticColor() : overrideColor);
			if (!flag && who.hat.Value.isMask && facingDirection == 0)
			{
				Rectangle value3 = hatSourceRect;
				value3.Height -= 11;
				value3.Y += 11;
				b.Draw(texture3, position + origin + positionOffset + new Vector2(0f, 11f * num2) + new Vector2((0f - num2) * 2f + (float)(((!flip) ? 1 : (-1)) * num3 * 4), -16 + num4 * 4 + num5 + 4 + heightOffset.Value), value3, overrideColor, rotation, origin, num2, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Hat));
				value3 = hatSourceRect;
				value3.Height = 11;
				b.Draw(texture3, position6, value3, color2, rotation, origin, num2, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.HatMaskUp));
			}
			else
			{
				b.Draw(texture3, position6, hatSourceRect, color2, rotation, origin, num2, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Hat));
			}
		}
	}

	public static float GetLayerDepth(float baseLayerDepth, FarmerSpriteLayers layer, bool dyeLayer = false)
	{
		if (layer == FarmerSpriteLayers.TOOL_IN_USE_SIDE)
		{
			return baseLayerDepth + 0.0032f;
		}
		int num = ((!Game1.isUsingBackToFrontSorting) ? 1 : (-1));
		if (dyeLayer)
		{
			baseLayerDepth += 1E-07f * (float)num;
		}
		return baseLayerDepth + (float)layer * 1E-06f * (float)num;
	}

	public void draw(SpriteBatch b, FarmerSprite.AnimationFrame animationFrame, int currentFrame, Rectangle sourceRect, Vector2 position, Vector2 origin, float layerDepth, int facingDirection, Color overrideColor, float rotation, float scale, Farmer who)
	{
		float scale2 = 4f * scale;
		int num = featureXOffsetPerFrame[currentFrame];
		int num2 = featureYOffsetPerFrame[currentFrame];
		bool flag = currentFrame == 104 || currentFrame == 105;
		if (_sickFrame != flag)
		{
			_sickFrame = flag;
			_shirtDirty = true;
			_spriteDirty = true;
		}
		executeRecolorActions(who);
		position = new Vector2((float)Math.Floor(position.X), (float)Math.Floor(position.Y));
		rotationAdjustment = Vector2.Zero;
		positionOffset.Y = animationFrame.positionOffset * 4;
		positionOffset.X = animationFrame.xOffset * 4;
		if (!isDrawingForUI && who.swimming.Value)
		{
			sourceRect.Height /= 2;
			sourceRect.Height -= (int)who.yOffset / 4;
			position.Y += 64f;
		}
		if (facingDirection == 3 || facingDirection == 1)
		{
			facingDirection = ((!animationFrame.flip) ? 1 : 3);
		}
		b.Draw(baseTexture, position + origin + positionOffset, sourceRect, overrideColor, rotation, origin, scale2, animationFrame.flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Base));
		if (!isDrawingForUI && who.swimming.Value)
		{
			if (who.currentEyes != 0 && who.FacingDirection != 0 && (Game1.timeOfDay < 2600 || (who.isInBed.Value && who.timeWentToBed.Value != 0)) && ((!who.FarmerSprite.PauseForSingleAnimation && !who.UsingTool) || (who.UsingTool && who.CurrentTool is FishingRod)))
			{
				Vector2 position2 = position + origin + positionOffset + new Vector2(num * 4 + 20 + ((who.FacingDirection == 1) ? 12 : ((who.FacingDirection == 3) ? 4 : 0)), num2 * 4 + 40);
				b.Draw(baseTexture, position2, new Rectangle(5, 16, (who.FacingDirection == 2) ? 6 : 2, 2), overrideColor, 0f, origin, scale2, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.FaceSkin));
				b.Draw(baseTexture, position2, new Rectangle(264 + ((who.FacingDirection == 3) ? 4 : 0), 2 + (who.currentEyes - 1) * 2, (who.FacingDirection == 2) ? 6 : 2, 2), overrideColor, 0f, origin, scale2, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Eyes));
			}
			drawHairAndAccesories(b, facingDirection, who, position, origin, scale, currentFrame, rotation, overrideColor, layerDepth);
			b.Draw(Game1.staminaRect, new Rectangle((int)position.X + (int)who.yOffset + 8, (int)position.Y - 128 + sourceRect.Height * 4 + (int)origin.Y - (int)who.yOffset, sourceRect.Width * 4 - (int)who.yOffset * 2 - 16, 4), Game1.staminaRect.Bounds, Color.White * 0.75f, 0f, Vector2.Zero, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.SwimWaterRing));
			return;
		}
		who.GetDisplayPants(out var texture, out var spriteIndex);
		Rectangle value = new Rectangle(sourceRect.X, sourceRect.Y, sourceRect.Width, sourceRect.Height);
		value.X += spriteIndex % 10 * 192;
		value.Y += spriteIndex / 10 * 688;
		if (!who.IsMale)
		{
			value.X += 96;
		}
		if (skin.Value != -12345 || who.pantsItem.Value != null)
		{
			b.Draw(texture, position + origin + positionOffset, value, (overrideColor == Color.White) ? Utility.MakeCompletelyOpaque(who.GetPantsColor()) : overrideColor, rotation, origin, scale2, animationFrame.flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, GetLayerDepth(layerDepth, (who.FarmerSprite.CurrentAnimationFrame.frame == 5) ? FarmerSpriteLayers.PantsPassedOut : FarmerSpriteLayers.Pants));
		}
		sourceRect.Offset(288, 0);
		if (who.currentEyes != 0 && facingDirection != 0 && (Game1.timeOfDay < 2600 || (who.isInBed.Value && who.timeWentToBed.Value != 0)) && ((!who.FarmerSprite.PauseForSingleAnimation && !who.UsingTool) || (who.UsingTool && who.CurrentTool is FishingRod)) && (!who.UsingTool || !(who.CurrentTool is FishingRod { isFishing: false })))
		{
			int num3 = 5;
			num3 = (animationFrame.flip ? (num3 - num) : (num3 + num));
			switch (facingDirection)
			{
			case 1:
				num3 += 3;
				break;
			case 3:
				num3++;
				break;
			}
			num3 *= 4;
			b.Draw(baseTexture, position + origin + positionOffset + new Vector2(num3, num2 * 4 + ((who.IsMale && who.FacingDirection != 2) ? 36 : 40)), new Rectangle(5, 16, (facingDirection == 2) ? 6 : 2, 2), overrideColor, 0f, origin, scale2, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.FaceSkin));
			b.Draw(baseTexture, position + origin + positionOffset + new Vector2(num3, num2 * 4 + ((who.FacingDirection == 1 || who.FacingDirection == 3) ? 40 : 44)), new Rectangle(264 + ((facingDirection == 3) ? 4 : 0), 2 + (who.currentEyes - 1) * 2, (facingDirection == 2) ? 6 : 2, 2), overrideColor, 0f, origin, scale2, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Eyes));
		}
		drawHairAndAccesories(b, facingDirection, who, position, origin, scale, currentFrame, rotation, overrideColor, layerDepth);
		FarmerSpriteLayers layer = FarmerSpriteLayers.Arms;
		if (facingDirection == 0)
		{
			layer = FarmerSpriteLayers.ArmsUp;
		}
		if (animationFrame.armOffset > 0)
		{
			sourceRect.Offset(-288 + animationFrame.armOffset * 16, 0);
			b.Draw(baseTexture, position + origin + positionOffset + who.armOffset, sourceRect, overrideColor, rotation, origin, scale2, animationFrame.flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, GetLayerDepth(layerDepth, layer));
		}
		if (!who.usingSlingshot || !(who.CurrentTool is Slingshot slingshot))
		{
			return;
		}
		Point point = Utility.Vector2ToPoint(slingshot.AdjustForHeight(Utility.PointToVector2(slingshot.aimPos.Value)));
		int x = point.X;
		int y = point.Y;
		int backArmDistance = slingshot.GetBackArmDistance(who);
		Vector2 shootOrigin = slingshot.GetShootOrigin(who);
		float num4 = (float)Math.Atan2((float)y - shootOrigin.Y, (float)x - shootOrigin.X) + (float)Math.PI;
		if (!Game1.options.useLegacySlingshotFiring)
		{
			num4 -= (float)Math.PI;
			if (num4 < 0f)
			{
				num4 += (float)Math.PI * 2f;
			}
		}
		switch (facingDirection)
		{
		case 0:
			b.Draw(baseTexture, position + new Vector2(4f + num4 * 8f, -44f), new Rectangle(173, 238, 9, 14), Color.White, 0f, new Vector2(4f, 11f), scale2, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.SlingshotUp));
			break;
		case 1:
		{
			b.Draw(baseTexture, position + new Vector2(52 - backArmDistance, -32f), new Rectangle(147, 237, 10, 4), Color.White, 0f, new Vector2(8f, 3f), scale2, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Slingshot));
			b.Draw(baseTexture, position + new Vector2(36f, -44f), new Rectangle(156, 244, 9, 10), Color.White, num4, new Vector2(0f, 3f), scale2, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.SlingshotUp));
			int num5 = (int)(Math.Cos(num4 + (float)Math.PI / 2f) * (double)(20 - backArmDistance - 8) - Math.Sin(num4 + (float)Math.PI / 2f) * -68.0);
			int num6 = (int)(Math.Sin(num4 + (float)Math.PI / 2f) * (double)(20 - backArmDistance - 8) + Math.Cos(num4 + (float)Math.PI / 2f) * -68.0);
			Utility.drawLineWithScreenCoordinates((int)(position.X + 52f - (float)backArmDistance), (int)(position.Y - 32f - 4f), (int)(position.X + 32f + (float)(num5 / 2)), (int)(position.Y - 32f - 12f + (float)(num6 / 2)), b, Color.White);
			break;
		}
		case 3:
		{
			b.Draw(baseTexture, position + new Vector2(40 + backArmDistance, -32f), new Rectangle(147, 237, 10, 4), Color.White, 0f, new Vector2(9f, 4f), scale2, SpriteEffects.FlipHorizontally, GetLayerDepth(layerDepth, FarmerSpriteLayers.Slingshot));
			b.Draw(baseTexture, position + new Vector2(24f, -40f), new Rectangle(156, 244, 9, 10), Color.White, num4 + (float)Math.PI, new Vector2(8f, 3f), scale2, SpriteEffects.FlipHorizontally, GetLayerDepth(layerDepth, FarmerSpriteLayers.SlingshotUp));
			int num5 = (int)(Math.Cos(num4 + (float)Math.PI * 2f / 5f) * (double)(20 + backArmDistance - 8) - Math.Sin(num4 + (float)Math.PI * 2f / 5f) * -68.0);
			int num6 = (int)(Math.Sin(num4 + (float)Math.PI * 2f / 5f) * (double)(20 + backArmDistance - 8) + Math.Cos(num4 + (float)Math.PI * 2f / 5f) * -68.0);
			Utility.drawLineWithScreenCoordinates((int)(position.X + 4f + (float)backArmDistance), (int)(position.Y - 32f - 8f), (int)(position.X + 26f + (float)num5 * 4f / 10f), (int)(position.Y - 32f - 8f + (float)num6 * 4f / 10f), b, Color.White);
			break;
		}
		case 2:
			b.Draw(baseTexture, position + new Vector2(4f, -32 - backArmDistance / 2), new Rectangle(148, 244, 4, 4), Color.White, 0f, Vector2.Zero, scale2, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Arms));
			Utility.drawLineWithScreenCoordinates((int)(position.X + 16f), (int)(position.Y - 28f - (float)(backArmDistance / 2)), (int)(position.X + 44f - num4 * 10f), (int)(position.Y - 16f - 8f), b, Color.White);
			Utility.drawLineWithScreenCoordinates((int)(position.X + 16f), (int)(position.Y - 28f - (float)(backArmDistance / 2)), (int)(position.X + 56f - num4 * 10f), (int)(position.Y - 16f - 8f), b, Color.White);
			b.Draw(baseTexture, position + new Vector2(44f - num4 * 10f, -16f), new Rectangle(167, 235, 7, 9), Color.White, 0f, new Vector2(3f, 5f), scale2, SpriteEffects.None, GetLayerDepth(layerDepth, FarmerSpriteLayers.Slingshot, dyeLayer: true));
			break;
		}
	}
}
