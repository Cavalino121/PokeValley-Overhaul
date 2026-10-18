using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using xTile.Dimensions;
using xTile.Layers;
using xTile.Tiles;
using StardewValley.TerrainFeatures;
using StardewValley.Buildings;
using StardewValley.Objects;
using StardewValley.Locations;
using StardewValley.ItemTypeDefinitions;

namespace StardewValley.FirstPerson;

/// <summary>
/// 3D / 2.5D Renderer for First-Person perspective in Stardew Valley.
/// Features true Alpha-Tested depth testing (eliminates see-through glitches),
/// solid 3D building/furniture geometry, dynamic sky and mountain/forest horizon from Minigames/Clouds,
/// tilled dirt, mailbox, bus, trees, bushes, animated chests, and NPCs.
/// </summary>
public class FPSRenderer : IDisposable
{
	private readonly GraphicsDevice _graphicsDevice;
	private AlphaTestEffect _alphaEffect;

	// Batches by texture
	private readonly Dictionary<Texture2D, List<VertexPositionColorTexture>> _batches = new();

	// Skybox & Horizon textures
	private Texture2D _cloudsTexture;
	private Texture2D _whitePixel;
	private Texture2D _crosshairTexture;

	// Viewmodel animations
	private float _toolBobTimer = 0f;
	private float _toolSwingProgress = 0f;

	public FPSRenderer(GraphicsDevice device)
	{
		_graphicsDevice = device;
		InitializeEffect();
		CreateWhitePixel();
		CreateCrosshair();
		LoadAssets();
	}

	private void InitializeEffect()
	{
		_alphaEffect = new AlphaTestEffect(_graphicsDevice)
		{
			VertexColorEnabled = true,
			AlphaFunction = CompareFunction.Greater,
			ReferenceAlpha = 32,
			FogEnabled = true,
			FogStart = 500f,
			FogEnd = 4000f
		};
	}

	private void LoadAssets()
	{
		try
		{
			_cloudsTexture = Game1.content.Load<Texture2D>("Minigames\\Clouds");
		}
		catch
		{
			_cloudsTexture = null;
		}
	}

	private void CreateWhitePixel()
	{
		_whitePixel = new Texture2D(_graphicsDevice, 1, 1);
		_whitePixel.SetData(new Color[] { Color.White });
	}

	private void CreateCrosshair()
	{
		_crosshairTexture = new Texture2D(_graphicsDevice, 16, 16);
		Color[] pixels = new Color[16 * 16];
		Color crossColor = new Color(255, 255, 255, 230);
		Color shadowColor = new Color(0, 0, 0, 180);

		for (int y = 0; y < 16; y++)
		{
			for (int x = 0; x < 16; x++)
			{
				if ((x == 7 || x == 8) && (y >= 4 && y <= 11))
					pixels[y * 16 + x] = crossColor;
				else if ((y == 7 || y == 8) && (x >= 4 && x <= 11))
					pixels[y * 16 + x] = crossColor;
				else if ((x == 6 || x == 9) && (y >= 4 && y <= 11))
					pixels[y * 16 + x] = shadowColor;
				else if ((y == 6 || y == 9) && (x >= 4 && x <= 11))
					pixels[y * 16 + x] = shadowColor;
				else
					pixels[y * 16 + x] = Color.Transparent;
			}
		}
		_crosshairTexture.SetData(pixels);
	}

	public void RenderWorld(FPSCamera camera, GameLocation location, GameTime gameTime)
	{
		if (location?.Map == null || Game1.player == null)
			return;

		// 1. Clear Depth Buffer and Framebuffer with ambient sky color (Never pure black void!)
		Color skyClearColor = GetSkyClearColor(location);
		_graphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, skyClearColor, 1.0f, 0);

		// 2. Configure 3D Render States
		_graphicsDevice.BlendState = BlendState.AlphaBlend;
		_graphicsDevice.DepthStencilState = DepthStencilState.Default;
		_graphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
		_graphicsDevice.RasterizerState = RasterizerState.CullNone;

		UpdateFog(location);

		_alphaEffect.View = camera.ViewMatrix;
		_alphaEffect.Projection = camera.ProjectionMatrix;
		_alphaEffect.World = Matrix.Identity;

		_batches.Clear();

		// 3. Draw Horizon Backdrops (Mountains, Forest, Clouds) or Indoor Ceiling
		RenderBackdropAndCeiling(camera, location);

		// 4. Render Ground Floor & Solid Walls
		RenderTerrainAndWalls(camera, location);

		// 5. Render All World Entities & 3D Objects
		RenderAllEntities(camera, location);

		// Flush all world batches
		FlushBatches();

		// 6. Highlight targeted tile on ground
		if (camera.TryGetTargetGroundTile(350f, out Microsoft.Xna.Framework.Point targetTile))
		{
			RenderTileHighlight(targetTile);
		}
	}

	private Color GetSkyClearColor(GameLocation location)
	{
		if (!location.IsOutdoors)
		{
			return new Color(25, 20, 18); // Cozy warm indoor backdrop
		}

		if (location.IsRainingHere())
		{
			return new Color(110, 125, 135); // Overcast rain
		}

		if (Game1.timeOfDay >= 2000)
		{
			return new Color(8, 12, 30); // Night sky
		}

		if (Game1.timeOfDay >= 1800)
		{
			return new Color(210, 110, 65); // Warm sunset
		}

		return new Color(125, 195, 255); // Crisp daytime sky
	}

	private void UpdateFog(GameLocation location)
	{
		Color skyColor = GetSkyClearColor(location);
		_alphaEffect.FogColor = skyColor.ToVector3();

		if (location.IsOutdoors)
		{
			if (location.IsRainingHere())
			{
				_alphaEffect.FogStart = 400f;
				_alphaEffect.FogEnd = 2200f;
			}
			else if (Game1.timeOfDay >= 2000)
			{
				_alphaEffect.FogStart = 500f;
				_alphaEffect.FogEnd = 2600f;
			}
			else
			{
				_alphaEffect.FogStart = 900f;
				_alphaEffect.FogEnd = 4200f;
			}
		}
		else
		{
			_alphaEffect.FogStart = 800f;
			_alphaEffect.FogEnd = 3000f;
		}
	}

	private void RenderBackdropAndCeiling(FPSCamera camera, GameLocation location)
	{
		Vector3 camPos = camera.Position;

		if (location.IsOutdoors)
		{
			float radius = 3200f;
			float wallH = 1400f;

			Texture2D bgTex = _cloudsTexture ?? _whitePixel;

			// Mountain silhouette source rect from Minigames/Clouds
			Microsoft.Xna.Framework.Rectangle mountainRect = (_cloudsTexture != null) ? new Microsoft.Xna.Framework.Rectangle(0, 554, 165, 142) : Microsoft.Xna.Framework.Rectangle.Empty;
			// Forest silhouette source rect from Minigames/Clouds
			Microsoft.Xna.Framework.Rectangle forestRect = (_cloudsTexture != null) ? new Microsoft.Xna.Framework.Rectangle(390, 543, 122, 153) : Microsoft.Xna.Framework.Rectangle.Empty;
			// Cloud backdrop
			Microsoft.Xna.Framework.Rectangle cloudRect = (_cloudsTexture != null) ? new Microsoft.Xna.Framework.Rectangle(0, 0, 512, 337) : Microsoft.Xna.Framework.Rectangle.Empty;

			Color tint = Color.White;
			if (Game1.timeOfDay >= 2000) tint = new Color(30, 40, 70);
			else if (Game1.timeOfDay >= 1800) tint = new Color(240, 140, 100);

			// North Horizon: Mountain Peaks (facing South)
			AddTexturedWallQuad(
				bgTex, mountainRect,
				new Vector3(camPos.X - radius, -100f, camPos.Z - radius),
				new Vector3(camPos.X + radius, -100f, camPos.Z - radius),
				wallH, tint
			);

			// South Horizon: Forest Line (facing North)
			AddTexturedWallQuad(
				bgTex, forestRect,
				new Vector3(camPos.X + radius, -100f, camPos.Z + radius),
				new Vector3(camPos.X - radius, -100f, camPos.Z + radius),
				wallH * 0.8f, tint
			);

			// East Horizon: Hills
			AddTexturedWallQuad(
				bgTex, mountainRect,
				new Vector3(camPos.X + radius, -100f, camPos.Z - radius),
				new Vector3(camPos.X + radius, -100f, camPos.Z + radius),
				wallH * 0.9f, tint
			);

			// West Horizon: Ridge Forest
			AddTexturedWallQuad(
				bgTex, forestRect,
				new Vector3(camPos.X - radius, -100f, camPos.Z + radius),
				new Vector3(camPos.X - radius, -100f, camPos.Z - radius),
				wallH * 0.9f, tint
			);

			// Overhead Sky Cloud Dome
			if (_cloudsTexture != null)
			{
				AddHorizontalPlane(
					bgTex, cloudRect,
					new Vector3(camPos.X - radius, wallH * 0.95f, camPos.Z - radius),
					radius * 2f, radius * 2f, tint * 0.85f
				);
			}
		}
		else
		{
			// Indoor Ceiling at height Y = 112
			float ceilingY = 112f;
			int mapW = location.Map.Layers[0].LayerWidth * 64;
			int mapH = location.Map.Layers[0].LayerHeight * 64;

			Color ceilingTint = new Color(145, 120, 95); // Warm wooden ceiling

			AddHorizontalPlane(
				_whitePixel, Microsoft.Xna.Framework.Rectangle.Empty,
				new Vector3(0, ceilingY, 0),
				mapW, mapH, ceilingTint
			);
		}
	}

	private void RenderTerrainAndWalls(FPSCamera camera, GameLocation location)
	{
		int playerTileX = (int)Math.Floor(camera.Position.X / 64f);
		int playerTileY = (int)Math.Floor(camera.Position.Z / 64f);
		int viewRadius = 26;

		xTile.Map map = location.Map;
		Layer backLayer = map.GetLayer("Back");
		Layer buildingsLayer = map.GetLayer("Buildings");
		Layer frontLayer = map.GetLayer("Front");

		int startX = Math.Max(0, playerTileX - viewRadius);
		int endX = Math.Min(backLayer.LayerWidth, playerTileX + viewRadius);
		int startY = Math.Max(0, playerTileY - viewRadius);
		int endY = Math.Min(backLayer.LayerHeight, playerTileY + viewRadius);

		// 1. Render Ground Floor (Back Layer only, NEVER Paths layer!)
		for (int y = startY; y < endY; y++)
		{
			for (int x = startX; x < endX; x++)
			{
				Tile backTile = backLayer?.Tiles[x, y];
				if (backTile?.TileSheet != null)
				{
					AddGroundQuad(backTile, x, y, 0f);
				}
			}
		}

		// 2. Render Solid Walls and Obstacles (Buildings Layer)
		for (int y = startY; y < endY; y++)
		{
			for (int x = startX; x < endX; x++)
			{
				Tile bTile = buildingsLayer?.Tiles[x, y];
				if (bTile?.TileSheet == null) continue;

				if (!location.IsOutdoors)
				{
					// Check if this tile is an open doorway / warp
					bool isDoorway = location.doesTileHaveProperty(x, y, "Warp", "Buildings") != null
						|| location.doesTileHaveProperty(x, y, "Action", "Buildings")?.Contains("Warp") == true
						|| location.isTilePassable(new Location(x, y), Game1.viewport);

					if (!isDoorway)
					{
						// Solid Indoor Wall
						AddIndoorWallBlock(bTile, x, y, buildingsLayer);
					}
					else
					{
						// Doorway floor threshold
						AddGroundQuad(bTile, x, y, 0.05f);
					}
				}
				else
				{
					// Outdoor: Water tiles are ALWAYS flat ground, never walls!
					if (location.isWaterTile(x, y))
					{
						AddGroundQuad(bTile, x, y, 0.02f);
						continue;
					}

					bool isPassable = location.isTilePassable(new Location(x, y), Game1.viewport);
					if (isPassable)
					{
						AddGroundQuad(bTile, x, y, 0.05f);
					}
					else
					{
						// Cliff or fence obstacle
						AddOutdoorCliffBlock(bTile, x, y);
					}
				}
			}
		}
	}

	private void AddIndoorWallBlock(Tile tile, int x, int y, Layer buildingsLayer)
	{
		Texture2D tex = GetTileSheetTexture(tile.TileSheet);
		if (tex == null || tex.IsDisposed) return;

		xTile.Dimensions.Rectangle bounds = tile.TileSheet.GetTileImageBounds(tile.TileIndex);
		Microsoft.Xna.Framework.Rectangle srcRect = new Microsoft.Xna.Framework.Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);

		float x0 = x * 64f;
		float x1 = (x + 1) * 64f;
		float z0 = y * 64f;
		float z1 = (y + 1) * 64f;
		float wallTop = 112f;

		// South Face (facing player looking North)
		AddTexturedWallQuad(tex, srcRect, new Vector3(x0, 0f, z1), new Vector3(x1, 0f, z1), wallTop, Color.White);

		// East Face if neighbor is open
		if (x + 1 < buildingsLayer.LayerWidth && buildingsLayer.Tiles[x + 1, y] == null)
		{
			AddTexturedWallQuad(tex, srcRect, new Vector3(x1, 0f, z1), new Vector3(x1, 0f, z0), wallTop, Color.White * 0.9f);
		}

		// West Face if neighbor is open
		if (x - 1 >= 0 && buildingsLayer.Tiles[x - 1, y] == null)
		{
			AddTexturedWallQuad(tex, srcRect, new Vector3(x0, 0f, z0), new Vector3(x0, 0f, z1), wallTop, Color.White * 0.9f);
		}
	}

	private void AddOutdoorCliffBlock(Tile tile, int x, int y)
	{
		Texture2D tex = GetTileSheetTexture(tile.TileSheet);
		if (tex == null || tex.IsDisposed) return;

		xTile.Dimensions.Rectangle bounds = tile.TileSheet.GetTileImageBounds(tile.TileIndex);
		Microsoft.Xna.Framework.Rectangle srcRect = new Microsoft.Xna.Framework.Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);

		float x0 = x * 64f;
		float x1 = (x + 1) * 64f;
		float z0 = y * 64f;
		float z1 = (y + 1) * 64f;
		float h = 64f;

		// 3D block for cliffs / fences
		AddTexturedWallQuad(tex, srcRect, new Vector3(x0, 0f, z1), new Vector3(x1, 0f, z1), h, Color.White);
		AddHorizontalPlane(tex, srcRect, new Vector3(x0, h, z0), 64f, 64f, Color.White);
	}

	private void RenderAllEntities(FPSCamera camera, GameLocation location)
	{
		Vector2 camPos2D = new Vector2(camera.Position.X, camera.Position.Z);

		// 1. Farm Buildings (Exterior Farmhouse, Barn, Coop, Greenhouse, Silo)
		if (location.buildings.Count > 0)
		{
			foreach (Building building in location.buildings)
			{
				if (building?.texture?.Value == null) continue;

				Texture2D bTex = building.texture.Value;
				Microsoft.Xna.Framework.Rectangle bRect = building.getSourceRect();
				float bWidth = building.tilesWide.Value * 64f;
				float bDepth = building.tilesHigh.Value * 64f;
				float bHeight = bRect.Height * 4f;

				Vector3 baseCenter = new Vector3(
					building.tileX.Value * 64f + bWidth * 0.5f,
					0f,
					building.tileY.Value * 64f + bDepth * 0.5f
				);

				DrawSolid3DBox(baseCenter, bWidth, bDepth, bHeight, bTex, bRect);
			}
		}

		// 2. Bus at BusStop
		if (location is BusStop busStop)
		{
			Vector3 busCenter = new Vector3(busStop.busPosition.X + 256f, 0f, busStop.busPosition.Y + 128f);
			DrawSolid3DBox(busCenter, 512f, 256f, 200f, Game1.mouseCursors, new Microsoft.Xna.Framework.Rectangle(288, 1247, 128, 64));
		}

		// 3. Mailbox on Farm
		if (location is Farm farm)
		{
			Point mbPoint = farm.GetMainMailboxPosition();
			Vector2 mbPos = new Vector2(mbPoint.X * 64f + 32f, mbPoint.Y * 64f + 32f);
			DrawCylindricalBillboard(camera, mbPos, Game1.mouseCursors, new Microsoft.Xna.Framework.Rectangle(141, 465, 20, 24), 80f, 96f);
		}

		// 4. Indoor Furniture (TV, Bed, Tables, Chairs, Rugs)
		if (location.furniture.Count > 0)
		{
			foreach (Furniture item in location.furniture)
			{
				if (item == null || item.isTemporarilyInvisible) continue;

				ParsedItemData data = ItemRegistry.GetDataOrErrorItem(item.QualifiedItemId);
				if (data == null) continue;

				Texture2D fTex = data.GetTexture();
				Microsoft.Xna.Framework.Rectangle fRect = item.sourceRect.Value;

				float fw = item.getTilesWide() * 64f;
				float fd = item.getTilesHigh() * 64f;
				float fh = fRect.Height * 4f;

				Vector2 fPos = item.TileLocation * 64f + new Vector2(fw * 0.5f, fd * 0.5f);

				// Clip bed when player is sleeping/sitting so it does not obstruct vision
				if (item is BedFurniture || item.QualifiedItemId.Contains("Bed"))
				{
					if (Vector2.Distance(fPos, camPos2D) < 70f && (Game1.player.isInBed.Value || Game1.player.IsSitting()))
					{
						continue; // Player is inside bed, don't clip through eyes
					}
				}

				if (item.furniture_type.Value == 12)
				{
					// Rug: flat on floor
					AddHorizontalPlane(fTex, fRect, new Vector3(item.TileLocation.X * 64f, 0.08f, item.TileLocation.Y * 64f), fw, fd, Color.White);
				}
				else
				{
					// Solid 3D Furniture Box (TV, Tables, Fireplace, Bed)
					DrawSolid3DBox(new Vector3(fPos.X, 0f, fPos.Y), fw, fd, fh, fTex, fRect);
				}
			}
		}

		// 5. Resource Clumps (Hollow logs, large stumps, boulders)
		if (location.resourceClumps.Count > 0)
		{
			foreach (ResourceClump clump in location.resourceClumps)
			{
				if (clump == null) continue;
				Texture2D cTex = clump.textureName.Value != null ? Game1.content.Load<Texture2D>(clump.textureName.Value) : Game1.objectSpriteSheet;
				if (cTex == null) continue;

				Microsoft.Xna.Framework.Rectangle cRect = Game1.getSourceRectForStandardTileSheet(cTex, clump.parentSheetIndex.Value, 16, 16);
				cRect.Width = clump.width.Value * 16;
				cRect.Height = clump.height.Value * 16;

				float cw = clump.width.Value * 64f;
				float ch = clump.height.Value * 64f;
				Vector2 cPos = clump.Tile * 64f + new Vector2(cw * 0.5f, ch * 0.5f);

				DrawCylindricalBillboard(camera, cPos, cTex, cRect, cw, ch);
			}
		}

		// 6. Large Terrain Features (Bushes)
		if (location.largeTerrainFeatures.Count > 0)
		{
			foreach (LargeTerrainFeature ltf in location.largeTerrainFeatures)
			{
				if (ltf is Bush bush)
				{
					Texture2D bushTex = Bush.texture.Value;
					Microsoft.Xna.Framework.Rectangle bRect = bush.sourceRect.Value;
					float bw = (bush.size.Value + 1) * 64f;
					float bh = 128f;
					Vector2 bPos = bush.Tile * 64f + new Vector2(bw * 0.5f, 32f);

					DrawCylindricalBillboard(camera, bPos, bushTex, bRect, bw, bh);
				}
			}
		}

		// 7. Terrain Features (Trees, Tilled Dirt, Crops, Grass)
		foreach (var kvp in location.terrainFeatures.Pairs)
		{
			Vector2 tilePos = kvp.Key;
			TerrainFeature tf = kvp.Value;

			// Trees
			if (tf is Tree tree && tree.texture?.Value != null)
			{
				// If chopped down and health <= 0, do not draw
				if (tree.health.Value <= 0 && !tree.stump.Value) continue;

				Texture2D tTex = tree.texture.Value;
				if (tree.stump.Value)
				{
					// Stump only
					DrawCylindricalBillboard(camera, tilePos * 64f + new Vector2(32f, 32f), tTex, new Microsoft.Xna.Framework.Rectangle(32, 96, 16, 32), 64f, 64f);
				}
				else if (tree.growthStage.Value >= 5)
				{
					// Adult Tree
					Microsoft.Xna.Framework.Rectangle topRect = Tree.treeTopSourceRect;
					DrawCylindricalBillboard(camera, tilePos * 64f + new Vector2(32f, 32f), tTex, topRect, 192f, 256f);
				}
				else
				{
					// Sprout
					Microsoft.Xna.Framework.Rectangle saplingRect = tree.growthStage.Value switch
					{
						0 => new Microsoft.Xna.Framework.Rectangle(32, 128, 16, 16),
						1 => new Microsoft.Xna.Framework.Rectangle(0, 128, 16, 16),
						2 => new Microsoft.Xna.Framework.Rectangle(16, 128, 16, 16),
						_ => new Microsoft.Xna.Framework.Rectangle(0, 96, 16, 32)
					};
					DrawCylindricalBillboard(camera, tilePos * 64f + new Vector2(32f, 32f), tTex, saplingRect, 64f, saplingRect.Height * 4f);
				}
			}
			// Fruit Trees
			else if (tf is FruitTree fTree && fTree.texture != null)
			{
				DrawCylindricalBillboard(camera, tilePos * 64f + new Vector2(32f, 32f), fTree.texture, new Microsoft.Xna.Framework.Rectangle(0, 0, 48, 80), 192f, 256f);
			}
			// Grass
			else if (tf is Grass grass && grass.texture?.Value != null)
			{
				DrawCylindricalBillboard(camera, tilePos * 64f + new Vector2(32f, 32f), grass.texture.Value, new Microsoft.Xna.Framework.Rectangle(0, 0, 15, 20), 60f, 80f);
			}
			// HoeDirt (Tilled Soil and Crops)
			else if (tf is HoeDirt dirt)
			{
				Texture2D dirtTex = HoeDirt.lightTexture ?? Game1.content.Load<Texture2D>("TerrainFeatures/hoeDirt");
				if (dirtTex != null)
				{
					Microsoft.Xna.Framework.Rectangle dRect = new Microsoft.Xna.Framework.Rectangle(
						(dirt.sourceRectPosition % 4) * 16,
						(dirt.sourceRectPosition / 4) * 16,
						16, 16
					);
					AddHorizontalPlane(dirtTex, dRect, new Vector3(tilePos.X * 64f, 0.05f, tilePos.Y * 64f), 64f, 64f, Color.White);

					if (dirt.state.Value == 1)
					{
						Microsoft.Xna.Framework.Rectangle wRect = new Microsoft.Xna.Framework.Rectangle(
							(dirt.wateredRectPosition % 4) * 16 + 64,
							(dirt.wateredRectPosition / 4) * 16,
							16, 16
						);
						AddHorizontalPlane(dirtTex, wRect, new Vector3(tilePos.X * 64f, 0.06f, tilePos.Y * 64f), 64f, 64f, Color.White);
					}
				}

				if (dirt.crop != null && Game1.cropSpriteSheet != null)
				{
					int row = dirt.crop.rowInSpriteSheet.Value;
					int phase = dirt.crop.currentPhase.Value;
					Microsoft.Xna.Framework.Rectangle cropRect = new Microsoft.Xna.Framework.Rectangle(phase * 16, row * 32, 16, 32);
					DrawCylindricalBillboard(camera, tilePos * 64f + new Vector2(32f, 32f), Game1.cropSpriteSheet, cropRect, 64f, 128f);
				}
			}
		}

		// 8. Placed Objects (Chests with animated lid, Furnaces, Sprinklers, Stones, Weeds)
		foreach (var kvp in location.objects.Pairs)
		{
			Vector2 tilePos = kvp.Key;
			StardewValley.Object obj = kvp.Value;
			if (obj == null) continue;

			ParsedItemData itemData = ItemRegistry.GetData(obj.QualifiedItemId);
			if (itemData == null) continue;

			Texture2D tex = itemData.GetTexture();
			Microsoft.Xna.Framework.Rectangle srcRect;

			if (obj is Chest chest)
			{
				srcRect = itemData.GetSourceRect(0, chest.currentLidFrame);
				// Solid 3D Box for Chest
				DrawSolid3DBox(new Vector3(tilePos.X * 64f + 32f, 0f, tilePos.Y * 64f + 32f), 52f, 52f, 52f, tex, srcRect);
			}
			else
			{
				srcRect = itemData.GetSourceRect();
				DrawCylindricalBillboard(camera, tilePos * 64f + new Vector2(32f, 32f), tex, srcRect, 64f, 64f);
			}
		}

		// 9. NPCs and Characters
		foreach (NPC npc in location.characters)
		{
			if (npc == null || npc.isInvisible.Value) continue;
			if (npc.Sprite?.Texture == null) continue;

			DrawCylindricalBillboard(camera, npc.Position + new Vector2(32f, 60f), npc.Sprite.Texture, npc.Sprite.SourceRect, npc.Sprite.SourceRect.Width * 4f, npc.Sprite.SourceRect.Height * 4f);
		}

		// 10. Farm Animals
		foreach (FarmAnimal animal in location.animals.Values)
		{
			if (animal?.Sprite?.Texture == null) continue;
			DrawCylindricalBillboard(camera, animal.Position + new Vector2(32f, 60f), animal.Sprite.Texture, animal.Sprite.SourceRect, animal.Sprite.SourceRect.Width * 4f, animal.Sprite.SourceRect.Height * 4f);
		}
	}

	/// <summary>
	/// Draws a solid 3D box (front, back, left, right, top faces) to eliminate flat paper cutout look.
	/// </summary>
	private void DrawSolid3DBox(Vector3 baseCenter, float width, float depth, float height, Texture2D texture, Microsoft.Xna.Framework.Rectangle srcRect)
	{
		if (texture == null || texture.IsDisposed) return;

		float halfW = width * 0.5f;
		float halfD = depth * 0.5f;

		Vector3 southLeft = new Vector3(baseCenter.X - halfW, 0f, baseCenter.Z + halfD);
		Vector3 southRight = new Vector3(baseCenter.X + halfW, 0f, baseCenter.Z + halfD);
		Vector3 northLeft = new Vector3(baseCenter.X - halfW, 0f, baseCenter.Z - halfD);
		Vector3 northRight = new Vector3(baseCenter.X + halfW, 0f, baseCenter.Z - halfD);

		// South Face (Front)
		AddTexturedWallQuad(texture, srcRect, southLeft, southRight, height, Color.White);

		// North Face (Back)
		AddTexturedWallQuad(texture, srcRect, northRight, northLeft, height, Color.White * 0.85f);

		// West Face (Left)
		AddTexturedWallQuad(texture, srcRect, northLeft, southLeft, height, Color.White * 0.9f);

		// East Face (Right)
		AddTexturedWallQuad(texture, srcRect, southRight, northRight, height, Color.White * 0.9f);

		// Top Face (Roof / Table top)
		AddHorizontalPlane(texture, srcRect, new Vector3(baseCenter.X - halfW, height, baseCenter.Z - halfD), width, depth, Color.White);
	}

	/// <summary>
	/// Cylindrical billboard rotates ONLY on the horizontal Y-axis towards the camera.
	/// Never tilts or flies when looking up/down!
	/// </summary>
	private void DrawCylindricalBillboard(FPSCamera camera, Vector2 groundCenter2D, Texture2D texture, Microsoft.Xna.Framework.Rectangle srcRect, float width, float height)
	{
		if (texture == null || texture.IsDisposed) return;

		// Calculate horizontal right vector perpendicular to line-of-sight on XZ plane
		Vector2 toCam = new Vector2(camera.Position.X - groundCenter2D.X, camera.Position.Z - groundCenter2D.Y);
		if (toCam.LengthSquared() < 0.001f) toCam = Vector2.UnitY;
		toCam.Normalize();

		Vector3 rightVec = new Vector3(toCam.Y, 0f, -toCam.X) * (width * 0.5f);
		Vector3 centerBottom = new Vector3(groundCenter2D.X, 0f, groundCenter2D.Y);

		Vector3 pBL = centerBottom - rightVec;
		Vector3 pBR = centerBottom + rightVec;

		AddTexturedWallQuad(texture, srcRect, pBL, pBR, height, Color.White);
	}

	private void AddTexturedWallQuad(Texture2D texture, Microsoft.Xna.Framework.Rectangle srcRect, Vector3 bl, Vector3 br, float height, Color color)
	{
		if (texture == null || texture.IsDisposed) return;

		if (!_batches.TryGetValue(texture, out var list))
		{
			list = new List<VertexPositionColorTexture>();
			_batches[texture] = list;
		}

		Vector3 tl = bl + Vector3.Up * height;
		Vector3 tr = br + Vector3.Up * height;

		float u0 = (srcRect != Microsoft.Xna.Framework.Rectangle.Empty) ? (float)srcRect.X / texture.Width : 0f;
		float v0 = (srcRect != Microsoft.Xna.Framework.Rectangle.Empty) ? (float)srcRect.Y / texture.Height : 0f;
		float u1 = (srcRect != Microsoft.Xna.Framework.Rectangle.Empty) ? (float)(srcRect.X + srcRect.Width) / texture.Width : 1f;
		float v1 = (srcRect != Microsoft.Xna.Framework.Rectangle.Empty) ? (float)(srcRect.Y + srcRect.Height) / texture.Height : 1f;

		list.Add(new VertexPositionColorTexture(tl, color, new Vector2(u0, v0)));
		list.Add(new VertexPositionColorTexture(tr, color, new Vector2(u1, v0)));
		list.Add(new VertexPositionColorTexture(bl, color, new Vector2(u0, v1)));

		list.Add(new VertexPositionColorTexture(bl, color, new Vector2(u0, v1)));
		list.Add(new VertexPositionColorTexture(tr, color, new Vector2(u1, v0)));
		list.Add(new VertexPositionColorTexture(br, color, new Vector2(u1, v1)));
	}

	private void AddHorizontalPlane(Texture2D texture, Microsoft.Xna.Framework.Rectangle srcRect, Vector3 topLeft, float width, float depth, Color color)
	{
		if (texture == null || texture.IsDisposed) return;

		if (!_batches.TryGetValue(texture, out var list))
		{
			list = new List<VertexPositionColorTexture>();
			_batches[texture] = list;
		}

		float x0 = topLeft.X;
		float x1 = topLeft.X + width;
		float z0 = topLeft.Z;
		float z1 = topLeft.Z + depth;
		float y = topLeft.Y;

		float u0 = (srcRect != Microsoft.Xna.Framework.Rectangle.Empty) ? (float)srcRect.X / texture.Width : 0f;
		float v0 = (srcRect != Microsoft.Xna.Framework.Rectangle.Empty) ? (float)srcRect.Y / texture.Height : 0f;
		float u1 = (srcRect != Microsoft.Xna.Framework.Rectangle.Empty) ? (float)(srcRect.X + srcRect.Width) / texture.Width : 1f;
		float v1 = (srcRect != Microsoft.Xna.Framework.Rectangle.Empty) ? (float)(srcRect.Y + srcRect.Height) / texture.Height : 1f;

		list.Add(new VertexPositionColorTexture(new Vector3(x0, y, z0), color, new Vector2(u0, v0)));
		list.Add(new VertexPositionColorTexture(new Vector3(x1, y, z0), color, new Vector2(u1, v0)));
		list.Add(new VertexPositionColorTexture(new Vector3(x0, y, z1), color, new Vector2(u0, v1)));

		list.Add(new VertexPositionColorTexture(new Vector3(x0, y, z1), color, new Vector2(u0, v1)));
		list.Add(new VertexPositionColorTexture(new Vector3(x1, y, z0), color, new Vector2(u1, v0)));
		list.Add(new VertexPositionColorTexture(new Vector3(x1, y, z1), color, new Vector2(u1, v1)));
	}

	private void AddGroundQuad(Tile tile, int x, int y, float altitude)
	{
		Texture2D tex = GetTileSheetTexture(tile.TileSheet);
		if (tex == null || tex.IsDisposed) return;

		xTile.Dimensions.Rectangle bounds = tile.TileSheet.GetTileImageBounds(tile.TileIndex);
		Microsoft.Xna.Framework.Rectangle srcRect = new Microsoft.Xna.Framework.Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);

		AddHorizontalPlane(tex, srcRect, new Vector3(x * 64f, altitude, y * 64f), 64f, 64f, Color.White);
	}

	private void FlushBatches()
	{
		foreach (var kvp in _batches)
		{
			Texture2D tex = kvp.Key;
			List<VertexPositionColorTexture> verts = kvp.Value;
			if (verts.Count == 0) continue;

			_alphaEffect.Texture = tex;
			foreach (EffectPass pass in _alphaEffect.CurrentTechnique.Passes)
			{
				pass.Apply();
				_graphicsDevice.DrawUserPrimitives(
					PrimitiveType.TriangleList,
					verts.ToArray(),
					0,
					verts.Count / 3
				);
			}
		}
	}

	private Texture2D GetTileSheetTexture(TileSheet tileSheet)
	{
		try
		{
			return Game1.content.Load<Texture2D>(tileSheet.ImageSource);
		}
		catch
		{
			return null;
		}
	}

	private void RenderTileHighlight(Microsoft.Xna.Framework.Point tile)
	{
		float x0 = tile.X * 64f;
		float x1 = (tile.X + 1) * 64f;
		float z0 = tile.Y * 64f;
		float z1 = (tile.Y + 1) * 64f;
		float alt = 1.2f;

		Color highlightColor = new Color(255, 255, 100, 140);

		VertexPositionColorTexture[] verts = new VertexPositionColorTexture[6]
		{
			new VertexPositionColorTexture(new Vector3(x0, alt, z0), highlightColor, Vector2.Zero),
			new VertexPositionColorTexture(new Vector3(x1, alt, z0), highlightColor, Vector2.UnitX),
			new VertexPositionColorTexture(new Vector3(x0, alt, z1), highlightColor, Vector2.UnitY),

			new VertexPositionColorTexture(new Vector3(x0, alt, z1), highlightColor, Vector2.UnitY),
			new VertexPositionColorTexture(new Vector3(x1, alt, z0), highlightColor, Vector2.UnitX),
			new VertexPositionColorTexture(new Vector3(x1, alt, z1), highlightColor, Vector2.One)
		};

		_alphaEffect.Texture = _crosshairTexture;
		foreach (EffectPass pass in _alphaEffect.CurrentTechnique.Passes)
		{
			pass.Apply();
			_graphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, verts, 0, 2);
		}
	}

	public void RenderHUD(SpriteBatch spriteBatch, GameTime gameTime, int screenWidth, int screenHeight)
	{
		// Draw Crosshair in exact center of UI viewport with (8, 8) origin
		if (_crosshairTexture != null)
		{
			Vector2 center = new Vector2(screenWidth * 0.5f, screenHeight * 0.5f);
			spriteBatch.Draw(_crosshairTexture, center, null, Color.White, 0f, new Vector2(8f, 8f), 2f, SpriteEffects.None, 0.99f);
		}

		// Draw Currently Held Tool / Item in Lower Right Corner (Minecraft-style viewmodel)
		Item heldItem = Game1.player?.CurrentItem;
		if (heldItem != null)
		{
			ParsedItemData itemData = ItemRegistry.GetData(heldItem.QualifiedItemId);
			if (itemData != null)
			{
				Texture2D tex = itemData.GetTexture();
				Microsoft.Xna.Framework.Rectangle srcRect = itemData.GetSourceRect();

				float baseScale = 5f;
				float swingOffset = 0f;

				if (Game1.player.UsingTool)
				{
					_toolSwingProgress += (float)gameTime.ElapsedGameTime.TotalSeconds * 8f;
					swingOffset = (float)Math.Sin(Math.Min(Math.PI, _toolSwingProgress)) * 50f;
				}
				else
				{
					_toolSwingProgress = 0f;
					_toolBobTimer += (float)gameTime.ElapsedGameTime.TotalSeconds * 4f;
					swingOffset = (float)Math.Sin(_toolBobTimer) * 4f;
				}

				Vector2 toolPos = new Vector2(screenWidth - 140f - swingOffset, screenHeight - 140f + swingOffset);
				spriteBatch.Draw(tex, toolPos, srcRect, Color.White, -0.2f + (swingOffset * 0.01f), new Vector2(8f, 8f), baseScale, SpriteEffects.None, 0.999f);
			}
		}
	}

	public void Dispose()
	{
		_alphaEffect?.Dispose();
		_whitePixel?.Dispose();
		_crosshairTexture?.Dispose();
	}
}
