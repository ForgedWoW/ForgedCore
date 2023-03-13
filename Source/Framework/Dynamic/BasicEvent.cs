﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Dynamic;

public class BasicEvent
{
	public ulong m_addTime;   // time when the event was added to queue, filled by event handler
	public double m_execTime; // planned time of next execution, filled by event handler

	AbortState m_abortState; // set by externals when the event is aborted, aborted events don't execute

	public BasicEvent()
	{
		m_abortState = AbortState.Running;
	}

	public void ScheduleAbort()
	{
		Cypher.Assert(IsRunning(), "Tried to scheduled the abortion of an event twice!");
		m_abortState = AbortState.Scheduled;
	}

	public void SetAborted()
	{
		Cypher.Assert(!IsAborted(), "Tried to abort an already aborted event!");
		m_abortState = AbortState.Aborted;
	}

	// this method executes when the event is triggered
	// return false if event does not want to be deleted
	// e_time is execution time, p_time is update interval
	public virtual bool Execute(ulong etime, uint pTime)
	{
		return true;
	}

	public virtual bool IsDeletable()
	{
		return true;
	} // this event can be safely deleted

	public virtual void Abort(ulong e_time) { } // this method executes when the event is aborted

	public bool IsRunning()
	{
		return m_abortState == AbortState.Running;
	}

	public bool IsAbortScheduled()
	{
		return m_abortState == AbortState.Scheduled;
	}

	public bool IsAborted()
	{
		return m_abortState == AbortState.Aborted;
	}
}