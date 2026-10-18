using System;
using System.Collections.Generic;
using System.IO;

namespace StardewValley;

public class NetLogger
{
	private Dictionary<string, NetLogRecord> loggedWrites = new Dictionary<string, NetLogRecord>();

	private DateTime timeLastStarted;

	private double priorMillis;

	private bool isLogging;

	public bool IsLogging
	{
		get
		{
			return isLogging;
		}
		set
		{
			if (value != isLogging)
			{
				isLogging = value;
				if (isLogging)
				{
					timeLastStarted = DateTime.UtcNow;
				}
				else
				{
					priorMillis += (DateTime.UtcNow - timeLastStarted).TotalMilliseconds;
				}
			}
		}
	}

	public double LogDuration
	{
		get
		{
			if (isLogging)
			{
				return priorMillis + (DateTime.UtcNow - timeLastStarted).TotalMilliseconds;
			}
			return priorMillis;
		}
	}

	public void LogWrite(string path, long length)
	{
		if (IsLogging)
		{
			loggedWrites.TryGetValue(path, out var value);
			value.Path = path;
			value.Count++;
			value.Bytes += length;
			loggedWrites[path] = value;
		}
	}

	public void Clear()
	{
		loggedWrites.Clear();
		priorMillis = 0.0;
		timeLastStarted = DateTime.UtcNow;
	}

	public string Dump()
	{
		string text = Path.Combine(Program.GetLocalAppDataFolder("Profiling"), DateTime.UtcNow.Ticks + ".csv");
		using StreamWriter streamWriter = File.CreateText(text);
		double num = LogDuration / 1000.0;
		streamWriter.WriteLine("Profile Duration: {0:F2}", num);
		streamWriter.WriteLine("Stack,Deltas,Bytes,Deltas/s,Bytes/s,Bytes/Delta");
		foreach (NetLogRecord value in loggedWrites.Values)
		{
			streamWriter.WriteLine("{0:F2},{1:F2},{2:F2},{3:F2},{4:F2},{5:F2}", value.Path, value.Count, value.Bytes, (double)value.Count / num, (double)value.Bytes / num, (double)value.Bytes / (double)value.Count);
		}
		return text;
	}
}
