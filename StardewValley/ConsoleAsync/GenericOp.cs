using System;
using System.Threading.Tasks;

namespace StardewValley.ConsoleAsync;

public sealed class GenericOp : AsyncTaskOperation
{
	public Action DoneCallback;

	public override bool Done => Task.Status >= TaskStatus.RanToCompletion;

	/// <summary>
	/// Returns true if successful
	///
	/// Otherwise will throw the tasks exception.
	/// This should be called from within the Action callback.
	/// </summary>        
	public bool Result
	{
		get
		{
			if (Task.Status >= TaskStatus.RanToCompletion)
			{
				if (Task.IsFaulted)
				{
					Exception baseException = Task.Exception.GetBaseException();
					Console.WriteLine(baseException);
					Console.WriteLine("Task failed with exception: {0}.", baseException.Message);
					throw baseException;
				}
				return true;
			}
			return false;
		}
	}

	public override void Conclude()
	{
		DoneCallback?.Invoke();
	}
}
