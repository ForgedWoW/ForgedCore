﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks.Dataflow;

namespace Framework.Threading;

public class LimitedThreadTaskManager
{
	readonly AutoResetEvent _mapUpdateComplete = new(false);
	Exception _exc = null;
	ActionBlock<Action> _actionBlock;
	readonly ExecutionDataflowBlockOptions _blockOptions;
    readonly List<Action> _staged = new List<Action>();

	public LimitedThreadTaskManager(int maxDegreeOfParallelism) : this(new ExecutionDataflowBlockOptions()
	{
		MaxDegreeOfParallelism = maxDegreeOfParallelism
	}) { }

	public LimitedThreadTaskManager(ExecutionDataflowBlockOptions blockOptions = null)
	{
		if (blockOptions == null)
			blockOptions = new ExecutionDataflowBlockOptions()
			{
				MaxDegreeOfParallelism = 1
			};

		_blockOptions = blockOptions;
		_actionBlock = new ActionBlock<Action>(ProcessTask, _blockOptions);
	}

	public void Deactivate()
	{
		_actionBlock.Complete();
		_actionBlock.Completion.Wait();
	}

	public void Wait()
    {
        ExecuteStaged();

        _actionBlock.Complete();
        _actionBlock.Completion.Wait();
        CheckForExcpetion();
        _actionBlock = new ActionBlock<Action>(ProcessTask, _blockOptions);
    }

    public void Schedule(Action a)
	{
		CheckForExcpetion();
		_actionBlock.Post(a);
	}


	/// <summary>
	///		Staged actions will not execute until <see cref="Wait"/> or <see cref="ExecuteStaged"/> is called.
	/// </summary>
	/// <param name="a"></param>
	public void Stage(Action a)
	{
		lock (_staged)
			_staged.Add(a);
	}


    public void ExecuteStaged()
    {
        lock (_staged)
        {
            foreach (var a in _staged)
                Schedule(a);

            _staged.Clear();
        }
    }

    public void ProcessTask(Action a)
	{
		try
		{
			a();
		}
		catch (Exception ex)
		{
			Log.outException(ex);
			_exc = ex;
		}
	}

	public void Complete(bool success)
	{
		_mapUpdateComplete.Set();
	}

	private void CheckForExcpetion()
	{
		if (_exc != null)
			throw new Exception("Error while processing task!", _exc);
	}
}