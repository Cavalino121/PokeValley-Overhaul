using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.Network;

namespace StardewValley.BellsAndWhistles;

public class Train : INetObject<NetFields>
{
	public const int minCars = 8;

	public const int maxCars = 24;

	public const double chanceForLongTrain = 0.1;

	public const int randomTrain = 0;

	public const int jojaTrain = 1;

	public const int coalTrain = 2;

	public const int passengerTrain = 3;

	public const int uniformColorPlainTrain = 4;

	public const int prisonTrain = 5;

	public const int christmasTrain = 6;

	public readonly NetObjectList<TrainCar> cars = new NetObjectList<TrainCar>();

	public readonly NetInt type = new NetInt();

	public readonly NetPosition position = new NetPosition();

	public float speed;

	public float wheelRotation;

	public float smokeTimer;

	private TemporaryAnimatedSprite whistleSteam;

	public NetFields NetFields { get; } = new NetFields("Train");

	public Train()
	{
		initNetFields();
		Random random = Game1.random;
		if (random.NextDouble() < 0.1)
		{
			type.Value = 3;
		}
		else if (random.NextDouble() < 0.1)
		{
			type.Value = 1;
		}
		else if (random.NextDouble() < 0.1)
		{
			type.Value = 2;
		}
		else if (random.NextDouble() < 0.05)
		{
			type.Value = 5;
		}
		else if (Game1.IsWinter && random.NextDouble() < 0.2)
		{
			type.Value = 6;
		}
		else
		{
			type.Value = 0;
		}
		int num = random.Next(8, 25);
		if (random.NextDouble() < 0.1)
		{
			num *= 2;
		}
		speed = 0.2f;
		smokeTimer = speed * 2000f;
		Color color = Color.White;
		double num2 = 1.0;
		double num3 = 1.0;
		switch (type.Value)
		{
		case 0:
			num2 = 0.2;
			num3 = 0.2;
			break;
		case 1:
			num2 = 0.0;
			num3 = 0.0;
			color = Color.DimGray;
			break;
		case 3:
			num2 = 1.0;
			num3 = 0.0;
			speed = 0.4f;
			break;
		case 2:
			num2 = 0.0;
			num3 = 0.7;
			break;
		case 5:
			num3 = 0.0;
			num2 = 0.0;
			color = Color.MediumBlue;
			speed = 0.4f;
			break;
		case 6:
			num2 = 0.0;
			num3 = 1.0;
			color = Color.Red;
			break;
		}
		cars.Add(new TrainCar(random, 3, -1, Color.White));
		for (int i = 1; i < num; i++)
		{
			int num4 = 0;
			if (random.NextDouble() < num2)
			{
				num4 = 2;
			}
			else if (random.NextDouble() < num3)
			{
				num4 = 1;
			}
			Color color2 = color;
			if (color.Equals(Color.White))
			{
				bool flag = false;
				bool flag2 = false;
				bool flag3 = false;
				switch (random.Next(3))
				{
				case 0:
					flag = true;
					break;
				case 1:
					flag2 = true;
					break;
				case 2:
					flag3 = true;
					break;
				}
				color2 = new Color(random.Next((!flag) ? 100 : 0, 250), random.Next((!flag2) ? 100 : 0, 250), random.Next((!flag3) ? 100 : 0, 250));
			}
			int frontDecal = type.Value switch
			{
				1 => 2, 
				5 => 1, 
				6 => -1, 
				_ => (random.NextDouble() < 0.3) ? random.Next(36) : (-1), 
			};
			int resourceType = 0;
			if (num4 == 1)
			{
				resourceType = random.Next(9);
				if (type.Value == 6)
				{
					resourceType = 9;
				}
			}
			cars.Add(new TrainCar(random, num4, frontDecal, color2, resourceType, random.Next(4, 10)));
		}
	}

	private void initNetFields()
	{
		NetFields.SetOwner(this).AddField(cars, "cars").AddField(type, "type")
			.AddField(position.NetFields, "position.NetFields");
	}

	public Rectangle getBoundingBox()
	{
		return new Rectangle(-cars.Count * 128 * 4 + (int)position.X, 2720, cars.Count * 128 * 4, 128);
	}

	public bool Update(GameTime time, GameLocation location)
	{
		if (Game1.IsMasterGame)
		{
			position.X += (float)time.ElapsedGameTime.Milliseconds * speed;
		}
		wheelRotation += (float)time.ElapsedGameTime.Milliseconds * ((float)Math.PI / 256f);
		wheelRotation %= (float)Math.PI * 2f;
		if (!Game1.eventUp && location.Equals(Game1.currentLocation))
		{
			Farmer player = Game1.player;
			Rectangle boundingBox = player.GetBoundingBox();
			Rectangle boundingBox2 = getBoundingBox();
			if (boundingBox.Intersects(boundingBox2))
			{
				player.xVelocity = 8f;
				player.yVelocity = (float)(boundingBox2.Center.Y - boundingBox.Center.Y) / 4f;
				player.takeDamage(20, overrideParry: true, null);
				if (player.UsingTool)
				{
					Game1.playSound("clank");
				}
			}
		}
		if (Game1.random.NextDouble() < 0.001 && location.Equals(Game1.currentLocation))
		{
			Game1.playSound("trainWhistle");
			whistleSteam = new TemporaryAnimatedSprite(27, new Vector2(position.X - 250f, 2624f), Color.White, 8, flipped: false, 100f, 0, 64, 1f, 64);
		}
		if (whistleSteam != null)
		{
			whistleSteam.Position = new Vector2(position.X - 258f, 2592f);
			if (whistleSteam.update(time))
			{
				whistleSteam = null;
			}
		}
		smokeTimer -= time.ElapsedGameTime.Milliseconds;
		if (smokeTimer <= 0f)
		{
			location.temporarySprites.Add(new TemporaryAnimatedSprite(25, new Vector2(position.X - 170f, 2496f), Color.White, 8, flipped: false, 100f, 0, 64, 1f, 128));
			smokeTimer = speed * 2000f;
		}
		if (position.X > (float)(cars.Count * 128 * 4 + 4480))
		{
			return true;
		}
		return false;
	}

	public void draw(SpriteBatch b, GameLocation location)
	{
		for (int i = 0; i < cars.Count; i++)
		{
			cars[i].draw(b, new Vector2(position.X - (float)((i + 1) * 512), 2592f), wheelRotation, location);
		}
		whistleSteam?.draw(b);
	}
}
