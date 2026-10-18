using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StardewValley.ConsoleAsync;

public class AsyncOperationManager
{
	private static AsyncOperationManager _instance;

	private List<IAsyncOperation> _pendingOps;

	private List<IAsyncOperation> _tempOps;

	private List<IAsyncOperation> _doneOps;

	public static AsyncOperationManager Use => _instance;

	public static void Init()
	{
		_instance = new AsyncOperationManager();
	}

	private AsyncOperationManager()
	{
		_pendingOps = new List<IAsyncOperation>();
		_tempOps = new List<IAsyncOperation>();
		_doneOps = new List<IAsyncOperation>();
	}

	public void AddPending(Task task, Action<GenericResult> doneAction)
	{
		GenericOp op = new GenericOp();
		op.DoneCallback = OnDone;
		op.Task = task;
		if (task.Status > TaskStatus.Created)
		{
			op.TaskStarted = true;
		}
		AddPending(op);
		void OnDone()
		{
			GenericResult obj = new GenericResult
			{
				Ex = op.Task.Exception
			};
			if (obj.Ex != null)
			{
				obj.Ex = obj.Ex.GetBaseException();
			}
			obj.Failed = obj.Ex != null;
			obj.Success = obj.Ex == null;
			doneAction(obj);
		}
	}

	public void AddPending(Action workAction, Action<GenericResult> doneAction)
	{
		GenericOp op = new GenericOp();
		op.DoneCallback = OnDone;
		Task task = new Task(workAction);
		op.Task = task;
		AddPending(op);
		void OnDone()
		{
			GenericResult obj = new GenericResult
			{
				Ex = op.Task.Exception
			};
			if (obj.Ex != null)
			{
				obj.Ex = obj.Ex.GetBaseException();
			}
			obj.Failed = obj.Ex != null;
			obj.Success = obj.Ex == null;
			doneAction(obj);
		}
	}

	public void AddPending(IAsyncOperation op)
	{
		lock (_pendingOps)
		{
			_pendingOps.Add(op);
		}
	}

	public void Update()
	{
		lock (_pendingOps)
		{
			_doneOps.Clear();
			_tempOps.Clear();
			_tempOps.AddRange(_pendingOps);
			_pendingOps.Clear();
			bool flag = false;
			for (int i = 0; i < _tempOps.Count; i++)
			{
				IAsyncOperation asyncOperation = _tempOps[i];
				if (flag)
				{
					_pendingOps.Add(asyncOperation);
					continue;
				}
				flag = true;
				if (!asyncOperation.Started)
				{
					asyncOperation.Begin();
					_pendingOps.Add(asyncOperation);
				}
				else if (asyncOperation.Done)
				{
					_doneOps.Add(asyncOperation);
				}
				else
				{
					_pendingOps.Add(asyncOperation);
				}
			}
			_tempOps.Clear();
		}
		for (int j = 0; j < _doneOps.Count; j++)
		{
			_doneOps[j].Conclude();
		}
		_doneOps.Clear();
	}
}
