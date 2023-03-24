﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Framework.Constants;
using Forged.RealmServer.Chat;
using Forged.RealmServer.DataStorage;
using Game.Entities;
using Forged.RealmServer.Maps;
using Forged.RealmServer.Misc;
using Forged.RealmServer.Movement;
using Forged.RealmServer.Scripting.Interfaces.IAreaTrigger;
using Forged.RealmServer.Spells;
using Game.Common.Entities;
using Game.Common.Entities.AreaTriggers;
using Game.Common.Entities.Creatures;
using Game.Common.Entities.GameObjects;
using Game.Common.Entities.Objects;
using Game.Common.Entities.Players;
using Game.Common.Entities.Units;

namespace Forged.RealmServer.AI;

public class SmartScript
{
	public ObjectGuid LastInvoker;

	// Max number of nested ProcessEventsFor() calls to avoid infinite loops
	const uint MaxNestedEvents = 10;
	readonly Dictionary<uint, uint> _counterList = new();
	readonly List<SmartScriptHolder> _events = new();
	readonly List<SmartScriptHolder> _installEvents = new();
	readonly List<SmartScriptHolder> _storedEvents = new();
	readonly List<uint> _remIDs = new();
	readonly Dictionary<uint, ObjectGuidList> _storedTargets = new();
	List<SmartScriptHolder> _timedActionList = new();
	ObjectGuid mTimedActionListInvoker;
	Creature _me;
	ObjectGuid _meOrigGUID;
	GameObject _go;
	ObjectGuid _goOrigGUID;
	Player _player;
	AreaTriggerRecord _trigger;
	AreaTrigger _areaTrigger;
	SceneTemplate _sceneTemplate;
	Quest _quest;
	SmartScriptType _scriptType;
	uint _eventPhase;

	uint _pathId;

	uint _textTimer;
	uint _lastTextID;
	ObjectGuid _textGUID;
	uint _talkerEntry;
	bool _useTextTimer;
	uint _currentPriority;
	bool _eventSortingRequired;
	uint _nestedEventsCounter;
	SmartEventFlags _allEventFlags;

	public SmartScript()
	{
		_go = null;
		_me = null;
		_trigger = null;
		_eventPhase = 0;
		_pathId = 0;
		_textTimer = 0;
		_lastTextID = 0;
		_textGUID = ObjectGuid.Empty;
		_useTextTimer = false;
		_talkerEntry = 0;
		_meOrigGUID = ObjectGuid.Empty;
		_goOrigGUID = ObjectGuid.Empty;
		LastInvoker = ObjectGuid.Empty;
		_scriptType = SmartScriptType.Creature;
	}

	public void OnReset()
	{
		ResetBaseObject();

		lock (_events)
		{
			foreach (var holder in _events)
			{
				if (!holder.Event.event_flags.HasAnyFlag(SmartEventFlags.DontReset))
				{
					InitTimer(holder);
					holder.RunOnce = false;
				}

				if (holder.Priority != SmartScriptHolder.DefaultPriority)
				{
					holder.Priority = SmartScriptHolder.DefaultPriority;
					_eventSortingRequired = true;
				}
			}
		}

		ProcessEventsFor(SmartEvents.Reset);
		LastInvoker.Clear();
	}

	public void ProcessEventsFor(SmartEvents e, Unit unit = null, uint var0 = 0, uint var1 = 0, bool bvar = false, SpellInfo spell = null, GameObject gob = null, string varString = "")
	{
		_nestedEventsCounter++;

		// Allow only a fixed number of nested ProcessEventsFor calls
		if (_nestedEventsCounter > MaxNestedEvents)
			Log.outWarn(LogFilter.ScriptsAi, $"SmartScript::ProcessEventsFor: reached the limit of max allowed nested ProcessEventsFor() calls with event {e}, skipping!\n{GetBaseObject().GetDebugInfo()}");
		else if (_nestedEventsCounter == 1)
			lock (_events) // only lock on the first event to prevent deadlock.
			{
				Process(e, unit, var0, var1, bvar, spell, gob, varString);
			}
		else
			Process(e, unit, var0, var1, bvar, spell, gob, varString);

		--_nestedEventsCounter;

		void Process(SmartEvents e, Unit unit, uint var0, uint var1, bool bvar, SpellInfo spell, GameObject gob, string varString)
		{
			foreach (var Event in _events)
			{
				var eventType = Event.GetEventType();

				if (eventType == SmartEvents.Link) //special handling
					continue;

				if (eventType == e)
					if (Global.ConditionMgr.IsObjectMeetingSmartEventConditions(Event.EntryOrGuid, Event.EventId, Event.SourceType, unit, GetBaseObject()))
						ProcessEvent(Event, unit, var0, var1, bvar, spell, gob, varString);
			}
		}
	}

	public bool CheckTimer(SmartScriptHolder e)
	{
		return e.Active;
	}

	public void OnUpdate(uint diff)
	{
		if ((_scriptType == SmartScriptType.Creature || _scriptType == SmartScriptType.GameObject || _scriptType == SmartScriptType.AreaTriggerEntity || _scriptType == SmartScriptType.AreaTriggerEntityServerside) && !GetBaseObject())
			return;

		if (_me != null && _me.IsInEvadeMode)
		{
			// Check if the timed action list finished and clear it if so.
			// This is required by SMART_ACTION_CALL_TIMED_ACTIONLIST failing if mTimedActionList is not empty.
			if (!_timedActionList.Empty())
			{
				var needCleanup1 = true;

				foreach (var scriptholder in _timedActionList)
					if (scriptholder.EnableTimed)
						needCleanup1 = false;

				if (needCleanup1)
					_timedActionList.Clear();
			}

			return;
		}

		InstallEvents(); //before UpdateTimers

		if (_eventSortingRequired)
		{
			lock (_events)
			{
				SortEvents(_events);
			}

			_eventSortingRequired = false;
		}

		lock (_events)
		{
			foreach (var holder in _events)
				UpdateTimer(holder, diff);
		}

		if (!_storedEvents.Empty())
			foreach (var holder in _storedEvents)
				UpdateTimer(holder, diff);

		var needCleanup = true;

		if (!_timedActionList.Empty())
			for (var i = 0; i < _timedActionList.Count; ++i)
			{
				var scriptHolder = _timedActionList[i];

				if (scriptHolder.EnableTimed)
				{
					UpdateTimer(scriptHolder, diff);
					needCleanup = false;
				}
			}

		if (needCleanup)
			_timedActionList.Clear();

		if (!_remIDs.Empty())
		{
			foreach (var id in _remIDs)
				RemoveStoredEvent(id);

			_remIDs.Clear();
		}

		if (_useTextTimer && _me != null)
		{
			if (_textTimer < diff)
			{
				var textID = _lastTextID;
				_lastTextID = 0;
				var entry = _talkerEntry;
				_talkerEntry = 0;
				_textTimer = 0;
				_useTextTimer = false;
				ProcessEventsFor(SmartEvents.TextOver, null, textID, entry);
			}
			else
			{
				_textTimer -= diff;
			}
		}
	}

	public void OnInitialize(WorldObject obj, AreaTriggerRecord at = null, SceneTemplate scene = null, Quest qst = null)
	{
		if (at != null)
		{
			_scriptType = SmartScriptType.AreaTrigger;
			_trigger = at;
			_player = obj.AsPlayer;

			if (_player == null)
			{
				Log.outError(LogFilter.Misc, $"SmartScript::OnInitialize: source is AreaTrigger with id {_trigger.Id}, missing trigger player");

				return;
			}

			Log.outDebug(LogFilter.ScriptsAi, $"SmartScript::OnInitialize: source is AreaTrigger with id {_trigger.Id}, triggered by player {_player.GUID}");
		}
		else if (scene != null)
		{
			_scriptType = SmartScriptType.Scene;
			_sceneTemplate = scene;
			_player = obj.AsPlayer;

			if (_player == null)
			{
				Log.outError(LogFilter.Misc, $"SmartScript::OnInitialize: source is Scene with id {_sceneTemplate.SceneId}, missing trigger player");

				return;
			}

			Log.outDebug(LogFilter.ScriptsAi, $"SmartScript::OnInitialize: source is Scene with id {_sceneTemplate.SceneId}, triggered by player {_player.GUID}");
		}
		else if (qst != null)
		{
			_scriptType = SmartScriptType.Quest;
			_quest = qst;
			_player = obj.AsPlayer;

			if (_player == null)
			{
				Log.outError(LogFilter.Misc, $"SmartScript::OnInitialize: source is Quest with id {qst.Id}, missing trigger player");

				return;
			}

			Log.outDebug(LogFilter.ScriptsAi, $"SmartScript::OnInitialize: source is Quest with id {qst.Id}, triggered by player {_player.GUID}");
		}
		else if (obj != null) // Handle object based scripts
		{
			switch (obj.TypeId)
			{
				case TypeId.Unit:
					_scriptType = SmartScriptType.Creature;
					_me = obj.AsCreature;
					Log.outDebug(LogFilter.Scripts, $"SmartScript.OnInitialize: source is Creature {_me.Entry}");

					break;
				case TypeId.GameObject:
					_scriptType = SmartScriptType.GameObject;
					_go = obj.AsGameObject;
					Log.outDebug(LogFilter.Scripts, $"SmartScript.OnInitialize: source is GameObject {_go.Entry}");

					break;
				case TypeId.AreaTrigger:
					_areaTrigger = obj.AsAreaTrigger;
					_scriptType = _areaTrigger.IsServerSide ? SmartScriptType.AreaTriggerEntityServerside : SmartScriptType.AreaTriggerEntity;
					Log.outDebug(LogFilter.ScriptsAi, $"SmartScript.OnInitialize: source is AreaTrigger {_areaTrigger.Entry}, IsServerSide {_areaTrigger.IsServerSide}");

					break;
				default:
					Log.outError(LogFilter.Scripts, "SmartScript.OnInitialize: Unhandled TypeID !WARNING!");

					return;
			}
		}
		else
		{
			Log.outError(LogFilter.ScriptsAi, "SmartScript.OnInitialize: !WARNING! Initialized WorldObject is Null.");

			return;
		}

		GetScript(); //load copy of script

		lock (_events)
		{
			foreach (var holder in _events)
				InitTimer(holder); //calculate timers for first Time use
		}

		ProcessEventsFor(SmartEvents.AiInit);
		InstallEvents();
		ProcessEventsFor(SmartEvents.JustCreated);
		_counterList.Clear();
	}

	public void OnMoveInLineOfSight(Unit who)
	{
		if (_me == null)
			return;

		ProcessEventsFor(_me.IsEngaged ? SmartEvents.IcLos : SmartEvents.OocLos, who);
	}

	public Unit DoSelectBelowHpPctFriendlyWithEntry(uint entry, float range, byte minHPDiff = 1, bool excludeSelf = true)
	{
		FriendlyBelowHpPctEntryInRange u_check = new(_me, entry, range, minHPDiff, excludeSelf);
		UnitLastSearcher searcher = new(_me, u_check, GridType.All);
		Cell.VisitGrid(_me, searcher, range);

		return searcher.GetTarget();
	}

	public void SetTimedActionList(SmartScriptHolder e, uint entry, Unit invoker, uint startFromEventId = 0)
	{
		// Do NOT allow to start a new actionlist if a previous one is already running, unless explicitly allowed. We need to always finish the current actionlist
		if (e.GetActionType() == SmartActions.CallTimedActionlist && e.Action.timedActionList.allowOverride == 0 && !_timedActionList.Empty())
			return;

		_timedActionList.Clear();
		_timedActionList = Global.SmartAIMgr.GetScript((int)entry, SmartScriptType.TimedActionlist);

		if (_timedActionList.Empty())
			return;

		_timedActionList.RemoveAll(script => { return script.EventId < startFromEventId; });

		mTimedActionListInvoker = invoker != null ? invoker.GUID : ObjectGuid.Empty;

		for (var i = 0; i < _timedActionList.Count; ++i)
		{
			var scriptHolder = _timedActionList[i];
			scriptHolder.EnableTimed = i == 0; //enable processing only for the first action

			if (e.Action.timedActionList.timerType == 0)
				scriptHolder.Event.type = SmartEvents.UpdateOoc;
			else if (e.Action.timedActionList.timerType == 1)
				scriptHolder.Event.type = SmartEvents.UpdateIc;
			else if (e.Action.timedActionList.timerType > 1)
				scriptHolder.Event.type = SmartEvents.Update;

			InitTimer(scriptHolder);
		}
	}

	public void SetPathId(uint id)
	{
		_pathId = id;
	}

	public uint GetPathId()
	{
		return _pathId;
	}

	public bool HasAnyEventWithFlag(SmartEventFlags flag)
	{
		return _allEventFlags.HasAnyFlag(flag);
	}

	public bool IsUnit(WorldObject obj)
	{
		return obj != null && (obj.IsTypeId(TypeId.Unit) || obj.IsTypeId(TypeId.Player));
	}

	public bool IsPlayer(WorldObject obj)
	{
		return obj != null && obj.IsTypeId(TypeId.Player);
	}

	public bool IsCreature(WorldObject obj)
	{
		return obj != null && obj.IsTypeId(TypeId.Unit);
	}

	public bool IsCharmedCreature(WorldObject obj)
	{
		if (!obj)
			return false;

		var creatureObj = obj.AsCreature;

		if (creatureObj)
			return creatureObj.IsCharmed;

		return false;
	}

	public bool IsGameObject(WorldObject obj)
	{
		return obj != null && obj.IsTypeId(TypeId.GameObject);
	}

	public List<WorldObject> GetStoredTargetList(uint id, WorldObject obj)
	{
		var list = _storedTargets.LookupByKey(id);

		if (list != null)
			return list.GetObjectList(obj);

		return null;
	}

	void ProcessAction(SmartScriptHolder e, Unit unit = null, uint var0 = 0, uint var1 = 0, bool bvar = false, SpellInfo spell = null, GameObject gob = null, string varString = "")
	{
		e.RunOnce = true; //used for repeat check

		//calc random
		if (e.GetEventType() != SmartEvents.Link && e.Event.event_chance < 100 && e.Event.event_chance != 0 && !e.Event.event_flags.HasFlag(SmartEventFlags.TempIgnoreChanceRoll))
			if (RandomHelper.randChance(e.Event.event_chance))
				return;

		// Remove SMART_EVENT_FLAG_TEMP_IGNORE_CHANCE_ROLL flag after processing roll chances as it's not needed anymore
		e.Event.event_flags &= ~SmartEventFlags.TempIgnoreChanceRoll;

		if (unit != null)
			LastInvoker = unit.GUID;

		var tempInvoker = GetLastInvoker();

		if (tempInvoker != null)
			Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction: Invoker: {0} (guidlow: {1})", tempInvoker.GetName(), tempInvoker.GUID.ToString());

		var targets = GetTargets(e, unit != null ? unit : gob);

		switch (e.GetActionType())
		{
			case SmartActions.Talk:
			{
				var talker = e.Target.type == 0 ? _me : null;
				Unit talkTarget = null;

				foreach (var target in targets)
					if (IsCreature(target) && !target.AsCreature.IsPet) // Prevented sending text to pets.
					{
						if (e.Action.talk.useTalkTarget != 0)
						{
							talker = _me;
							talkTarget = target.AsCreature;
						}
						else
						{
							talker = target.AsCreature;
						}

						break;
					}
					else if (IsPlayer(target))
					{
						talker = _me;
						talkTarget = target.AsPlayer;

						break;
					}

				if (talkTarget == null)
					talkTarget = GetLastInvoker();

				if (talker == null)
					break;

				_talkerEntry = talker.Entry;
				_lastTextID = e.Action.talk.textGroupId;
				_textTimer = e.Action.talk.duration;

				_useTextTimer = true;
				Global.CreatureTextMgr.SendChat(talker, (byte)e.Action.talk.textGroupId, talkTarget);

				Log.outDebug(LogFilter.ScriptsAi,
							"SmartScript.ProcessAction: SMART_ACTION_TALK: talker: {0} (Guid: {1}), textGuid: {2}",
							talker.GetName(),
							talker.GUID.ToString(),
							_textGUID.ToString());

				break;
			}
			case SmartActions.SimpleTalk:
			{
				foreach (var target in targets)
				{
					if (IsCreature(target))
					{
						Global.CreatureTextMgr.SendChat(target.AsCreature, (byte)e.Action.simpleTalk.textGroupId, IsPlayer(GetLastInvoker()) ? GetLastInvoker() : null);
					}
					else if (IsPlayer(target) && _me != null)
					{
						var templastInvoker = GetLastInvoker();
						Global.CreatureTextMgr.SendChat(_me, (byte)e.Action.simpleTalk.textGroupId, IsPlayer(templastInvoker) ? templastInvoker : null, ChatMsg.Addon, Language.Addon, CreatureTextRange.Normal, 0, SoundKitPlayType.Normal, TeamFaction.Other, false, target.AsPlayer);
					}

					Log.outDebug(LogFilter.ScriptsAi,
								"SmartScript.ProcessAction. SMART_ACTION_SIMPLE_TALK: talker: {0} (GuidLow: {1}), textGroupId: {2}",
								target.GetName(),
								target.GUID.ToString(),
								e.Action.simpleTalk.textGroupId);
				}

				break;
			}
			case SmartActions.PlayEmote:
			{
				foreach (var target in targets)
					if (IsUnit(target))
					{
						target.AsUnit.HandleEmoteCommand((Emote)e.Action.emote.emoteId);

						Log.outDebug(LogFilter.ScriptsAi,
									"SmartScript.ProcessAction. SMART_ACTION_PLAY_EMOTE: target: {0} (GuidLow: {1}), emote: {2}",
									target.GetName(),
									target.GUID.ToString(),
									e.Action.emote.emoteId);
					}

				break;
			}
			case SmartActions.Sound:
			{
				foreach (var target in targets)
					if (IsUnit(target))
					{
						if (e.Action.sound.distance == 1)
							target.PlayDistanceSound(e.Action.sound.soundId, e.Action.sound.onlySelf != 0 ? target.AsPlayer : null);
						else
							target.PlayDirectSound(e.Action.sound.soundId, e.Action.sound.onlySelf != 0 ? target.AsPlayer : null, e.Action.sound.keyBroadcastTextId);

						Log.outDebug(LogFilter.ScriptsAi,
									"SmartScript.ProcessAction. SMART_ACTION_SOUND: target: {0} (GuidLow: {1}), sound: {2}, onlyself: {3}",
									target.GetName(),
									target.GUID.ToString(),
									e.Action.sound.soundId,
									e.Action.sound.onlySelf);
					}

				break;
			}
			case SmartActions.SetFaction:
			{
				foreach (var target in targets)
					if (IsCreature(target))
					{
						if (e.Action.faction.factionId != 0)
						{
							target.AsCreature.Faction = e.Action.faction.factionId;

							Log.outDebug(LogFilter.ScriptsAi,
										"SmartScript.ProcessAction. SMART_ACTION_SET_FACTION: Creature entry {0}, GuidLow {1} set faction to {2}",
										target.Entry,
										target.GUID.ToString(),
										e.Action.faction.factionId);
						}
						else
						{
							var ci = Global.ObjectMgr.GetCreatureTemplate(target.AsCreature.Entry);

							if (ci != null)
								if (target.AsCreature.Faction != ci.Faction)
								{
									target.AsCreature.Faction = ci.Faction;

									Log.outDebug(LogFilter.ScriptsAi,
												"SmartScript.ProcessAction. SMART_ACTION_SET_FACTION: Creature entry {0}, GuidLow {1} set faction to {2}",
												target.Entry,
												target.GUID.ToString(),
												ci.Faction);
								}
						}
					}

				break;
			}
			case SmartActions.MorphToEntryOrModel:
			{
				foreach (var target in targets)
				{
					if (!IsCreature(target))
						continue;

					if (e.Action.morphOrMount.creature != 0 || e.Action.morphOrMount.model != 0)
					{
						//set model based on entry from creature_template
						if (e.Action.morphOrMount.creature != 0)
						{
							var ci = Global.ObjectMgr.GetCreatureTemplate(e.Action.morphOrMount.creature);

							if (ci != null)
							{
								var model = ObjectManager.ChooseDisplayId(ci);
								target.AsCreature.SetDisplayId(model.CreatureDisplayId, model.DisplayScale);

								Log.outDebug(LogFilter.ScriptsAi,
											"SmartScript.ProcessAction. SMART_ACTION_MORPH_TO_ENTRY_OR_MODEL: Creature entry {0}, GuidLow {1} set displayid to {2}",
											target.Entry,
											target.GUID.ToString(),
											model.CreatureDisplayId);
							}
						}
						//if no param1, then use value from param2 (modelId)
						else
						{
							target.AsCreature.SetDisplayId(e.Action.morphOrMount.model);

							Log.outDebug(LogFilter.ScriptsAi,
										"SmartScript.ProcessAction. SMART_ACTION_MORPH_TO_ENTRY_OR_MODEL: Creature entry {0}, GuidLow {1} set displayid to {2}",
										target.Entry,
										target.GUID.ToString(),
										e.Action.morphOrMount.model);
						}
					}
					else
					{
						target.AsCreature.DeMorph();

						Log.outDebug(LogFilter.ScriptsAi,
									"SmartScript.ProcessAction. SMART_ACTION_MORPH_TO_ENTRY_OR_MODEL: Creature entry {0}, GuidLow {1} demorphs.",
									target.Entry,
									target.GUID.ToString());
					}
				}

				break;
			}
			case SmartActions.FailQuest:
			{
				foreach (var target in targets)
					if (IsPlayer(target))
					{
						target.AsPlayer.FailQuest(e.Action.quest.questId);

						Log.outDebug(LogFilter.ScriptsAi,
									"SmartScript.ProcessAction. SMART_ACTION_FAIL_QUEST: Player guidLow {0} fails quest {1}",
									target.GUID.ToString(),
									e.Action.quest.questId);
					}

				break;
			}
			case SmartActions.OfferQuest:
			{
				foreach (var target in targets)
				{
					var player = target.AsPlayer;

					if (player)
					{
						var quest = Global.ObjectMgr.GetQuestTemplate(e.Action.questOffer.questId);

						if (quest != null)
						{
							if (_me && e.Action.questOffer.directAdd == 0)
							{
								if (player.CanTakeQuest(quest, true))
								{
									var session = player.Session;

									if (session)
									{
										PlayerMenu menu = new(session);
										menu.SendQuestGiverQuestDetails(quest, _me.GUID, true, false);
										Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction:: SMART_ACTION_OFFER_QUEST: Player {0} - offering quest {1}", player.GUID.ToString(), e.Action.questOffer.questId);
									}
								}
							}
							else
							{
								player.AddQuestAndCheckCompletion(quest, null);
								Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction: SMART_ACTION_ADD_QUEST: Player {0} add quest {1}", player.GUID.ToString(), e.Action.questOffer.questId);
							}
						}
					}
				}

				break;
			}
			case SmartActions.SetReactState:
			{
				foreach (var target in targets)
				{
					if (!IsCreature(target))
						continue;

					target.AsCreature.ReactState = (ReactStates)e.Action.react.state;
				}

				break;
			}
			case SmartActions.RandomEmote:
			{
				List<uint> emotes = new();
				var randomEmote = e.Action.randomEmote;

				foreach (var id in new[]
						{
							randomEmote.emote1, randomEmote.emote2, randomEmote.emote3, randomEmote.emote4, randomEmote.emote5, randomEmote.emote6,
						})
					if (id != 0)
						emotes.Add(id);

				foreach (var target in targets)
					if (IsUnit(target))
					{
						var emote = emotes.SelectRandom();
						target.AsUnit.HandleEmoteCommand((Emote)emote);

						Log.outDebug(LogFilter.ScriptsAi,
									"SmartScript.ProcessAction. SMART_ACTION_RANDOM_EMOTE: Creature guidLow {0} handle random emote {1}",
									target.GUID.ToString(),
									emote);
					}

				break;
			}
			case SmartActions.ThreatAllPct:
			{
				if (_me == null)
					break;

				foreach (var refe in _me.GetThreatManager().GetModifiableThreatList())
				{
					refe.ModifyThreatByPercent(Math.Max(-100, (int)(e.Action.threatPCT.threatINC - e.Action.threatPCT.threatDEC)));
					Log.outDebug(LogFilter.ScriptsAi, $"SmartScript.ProcessAction: SMART_ACTION_THREAT_ALL_PCT: Creature {_me.GUID} modify threat for {refe.Victim.GUID}, value {e.Action.threatPCT.threatINC - e.Action.threatPCT.threatDEC}");
				}

				break;
			}
			case SmartActions.ThreatSinglePct:
			{
				if (_me == null)
					break;

				foreach (var target in targets)
					if (IsUnit(target))
					{
						_me.GetThreatManager().ModifyThreatByPercent(target.AsUnit, Math.Max(-100, (int)(e.Action.threatPCT.threatINC - e.Action.threatPCT.threatDEC)));
						Log.outDebug(LogFilter.ScriptsAi, $"SmartScript.ProcessAction: SMART_ACTION_THREAT_SINGLE_PCT: Creature {_me.GUID} modify threat for {target.GUID}, value {e.Action.threatPCT.threatINC - e.Action.threatPCT.threatDEC}");
					}

				break;
			}
			case SmartActions.CallAreaexploredoreventhappens:
			{
				foreach (var target in targets)
				{
					// Special handling for vehicles
					if (IsUnit(target))
					{
						var vehicle = target.AsUnit.VehicleKit1;

						if (vehicle != null)
							foreach (var seat in vehicle.Seats)
							{
								var player = Global.ObjAccessor.GetPlayer(target, seat.Value.Passenger.Guid);

								if (player != null)
									player.AreaExploredOrEventHappens(e.Action.quest.questId);
							}
					}

					if (IsPlayer(target))
					{
						target.AsPlayer.AreaExploredOrEventHappens(e.Action.quest.questId);

						Log.outDebug(LogFilter.ScriptsAi,
									"SmartScript.ProcessAction. SMART_ACTION_CALL_AREAEXPLOREDOREVENTHAPPENS: {0} credited quest {1}",
									target.GUID.ToString(),
									e.Action.quest.questId);
					}
				}

				break;
			}
			case SmartActions.Cast:
			{
				if (e.Action.cast.targetsLimit > 0 && targets.Count > e.Action.cast.targetsLimit)
					targets.RandomResize(e.Action.cast.targetsLimit);

				var failedSpellCast = false;
				var successfulSpellCast = false;

				foreach (var target in targets)
				{
					if (_go != null)
						_go.CastSpell(target.AsUnit, e.Action.cast.spell);

					if (!IsUnit(target))
						continue;

					if (!e.Action.cast.castFlags.HasAnyFlag((uint)SmartCastFlags.AuraNotPresent) || !target.AsUnit.HasAura(e.Action.cast.spell))
					{
						var triggerFlag = TriggerCastFlags.None;

						if (e.Action.cast.castFlags.HasAnyFlag((uint)SmartCastFlags.Triggered))
						{
							if (e.Action.cast.triggerFlags != 0)
								triggerFlag = (TriggerCastFlags)e.Action.cast.triggerFlags;
							else
								triggerFlag = TriggerCastFlags.FullMask;
						}

						if (_me)
						{
							if (e.Action.cast.castFlags.HasAnyFlag((uint)SmartCastFlags.InterruptPrevious))
								_me.InterruptNonMeleeSpells(false);

							var result = _me.CastSpell(target.AsUnit, e.Action.cast.spell, new CastSpellExtraArgs(triggerFlag));
							var spellCastFailed = (result != SpellCastResult.SpellCastOk && result != SpellCastResult.SpellInProgress);

							if (e.Action.cast.castFlags.HasAnyFlag((uint)SmartCastFlags.CombatMove))
								((SmartAI)_me.AI).SetCombatMove(spellCastFailed, true);

							if (spellCastFailed)
								failedSpellCast = true;
							else
								successfulSpellCast = true;
						}
						else if (_go)
						{
							_go.CastSpell(target.AsUnit, e.Action.cast.spell, new CastSpellExtraArgs(triggerFlag));
						}
					}
					else
					{
						Log.outDebug(LogFilter.ScriptsAi,
									"Spell {0} not casted because it has flag SMARTCAST_AURA_NOT_PRESENT and the target (Guid: {1} Entry: {2} Type: {3}) already has the aura",
									e.Action.cast.spell,
									target.GUID,
									target.Entry,
									target.TypeId);
					}
				}

				// If there is at least 1 failed cast and no successful casts at all, retry again on next loop
				if (failedSpellCast && !successfulSpellCast)
				{
					RetryLater(e, true);

					// Don't execute linked events
					return;
				}

				break;
			}
			case SmartActions.SelfCast:
			{
				if (targets.Empty())
					break;

				if (e.Action.cast.targetsLimit != 0)
					targets.RandomResize(e.Action.cast.targetsLimit);

				var triggerFlags = TriggerCastFlags.None;

				if (e.Action.cast.castFlags.HasAnyFlag((uint)SmartCastFlags.Triggered))
				{
					if (e.Action.cast.triggerFlags != 0)
						triggerFlags = (TriggerCastFlags)e.Action.cast.triggerFlags;
					else
						triggerFlags = TriggerCastFlags.FullMask;
				}

				foreach (var target in targets)
				{
					var uTarget = target.AsUnit;

					if (uTarget == null)
						continue;

					if (!e.Action.cast.castFlags.HasAnyFlag((uint)SmartCastFlags.AuraNotPresent) || !uTarget.HasAura(e.Action.cast.spell))
					{
						if (e.Action.cast.castFlags.HasAnyFlag((uint)SmartCastFlags.InterruptPrevious))
							uTarget.InterruptNonMeleeSpells(false);

						uTarget.CastSpell(uTarget, e.Action.cast.spell, new CastSpellExtraArgs(triggerFlags));
					}
				}

				break;
			}
			case SmartActions.InvokerCast:
			{
				var tempLastInvoker = GetLastInvoker(unit);

				if (tempLastInvoker == null)
					break;

				if (targets.Empty())
					break;

				if (e.Action.cast.targetsLimit != 0)
					targets.RandomResize(e.Action.cast.targetsLimit);

				foreach (var target in targets)
				{
					if (!IsUnit(target))
						continue;

					if (!e.Action.cast.castFlags.HasAnyFlag((uint)SmartCastFlags.AuraNotPresent) || !target.AsUnit.HasAura(e.Action.cast.spell))
					{
						if (e.Action.cast.castFlags.HasAnyFlag((uint)SmartCastFlags.InterruptPrevious))
							tempLastInvoker.InterruptNonMeleeSpells(false);

						var triggerFlag = TriggerCastFlags.None;

						if (e.Action.cast.castFlags.HasAnyFlag((uint)SmartCastFlags.Triggered))
						{
							if (e.Action.cast.triggerFlags != 0)
								triggerFlag = (TriggerCastFlags)e.Action.cast.triggerFlags;
							else
								triggerFlag = TriggerCastFlags.FullMask;
						}

						tempLastInvoker.CastSpell(target.AsUnit, e.Action.cast.spell, new CastSpellExtraArgs(triggerFlag));

						Log.outDebug(LogFilter.ScriptsAi,
									"SmartScript.ProcessAction. SMART_ACTION_INVOKER_CAST: Invoker {0} casts spell {1} on target {2} with castflags {3}",
									tempLastInvoker.GUID.ToString(),
									e.Action.cast.spell,
									target.GUID.ToString(),
									e.Action.cast.castFlags);
					}
					else
					{
						Log.outDebug(LogFilter.ScriptsAi, "Spell {0} not cast because it has flag SMARTCAST_AURA_NOT_PRESENT and the target ({1}) already has the aura", e.Action.cast.spell, target.GUID.ToString());
					}
				}

				break;
			}
			case SmartActions.ActivateGobject:
			{
				foreach (var target in targets)
					if (IsGameObject(target))
					{
						// Activate
						target. // Activate
							AsGameObject.SetLootState(LootState.Ready);

						target.AsGameObject.UseDoorOrButton(0, false, unit);

						Log.outDebug(LogFilter.ScriptsAi,
									"SmartScript.ProcessAction. SMART_ACTION_ACTIVATE_GOBJECT. Gameobject {0} (entry: {1}) activated",
									target.GUID.ToString(),
									target.Entry);
					}

				break;
			}
			case SmartActions.ResetGobject:
			{
				foreach (var target in targets)
					if (IsGameObject(target))
					{
						target.AsGameObject.ResetDoorOrButton();

						Log.outDebug(LogFilter.ScriptsAi,
									"SmartScript.ProcessAction. SMART_ACTION_RESET_GOBJECT. Gameobject {0} (entry: {1}) reset",
									target.GUID.ToString(),
									target.Entry);
					}

				break;
			}
			case SmartActions.SetEmoteState:
			{
				foreach (var target in targets)
					if (IsUnit(target))
					{
						target.AsUnit.EmoteState = (Emote)e.Action.emote.emoteId;

						Log.outDebug(LogFilter.ScriptsAi,
									"SmartScript.ProcessAction. SMART_ACTION_SET_EMOTE_STATE. Unit {0} set emotestate to {1}",
									target.GUID.ToString(),
									e.Action.emote.emoteId);
					}

				break;
			}
			case SmartActions.AutoAttack:
			{
				_me.CanMelee = e.Action.autoAttack.attack != 0;

				Log.outDebug(LogFilter.ScriptsAi,
							"SmartScript.ProcessAction. SMART_ACTION_AUTO_ATTACK: Creature: {0} bool on = {1}",
							_me.GUID.ToString(),
							e.Action.autoAttack.attack);

				break;
			}
			case SmartActions.AllowCombatMovement:
			{
				if (!IsSmart())
					break;

				var move = e.Action.combatMove.move != 0;
				((SmartAI)_me.AI).SetCombatMove(move);

				Log.outDebug(LogFilter.ScriptsAi,
							"SmartScript.ProcessAction. SMART_ACTION_ALLOW_COMBAT_MOVEMENT: Creature {0} bool on = {1}",
							_me.GUID.ToString(),
							e.Action.combatMove.move);

				break;
			}
			case SmartActions.SetEventPhase:
			{
				if (GetBaseObject() == null)
					break;

				SetPhase(e.Action.setEventPhase.phase);

				Log.outDebug(LogFilter.ScriptsAi,
							"SmartScript.ProcessAction. SMART_ACTION_SET_EVENT_PHASE: Creature {0} set event phase {1}",
							GetBaseObject().GUID.ToString(),
							e.Action.setEventPhase.phase);

				break;
			}
			case SmartActions.IncEventPhase:
			{
				if (GetBaseObject() == null)
					break;

				IncPhase(e.Action.incEventPhase.inc);
				DecPhase(e.Action.incEventPhase.dec);

				Log.outDebug(LogFilter.ScriptsAi,
							"SmartScript.ProcessAction. SMART_ACTION_INC_EVENT_PHASE: Creature {0} inc event phase by {1}, " +
							"decrease by {2}",
							GetBaseObject().GUID.ToString(),
							e.Action.incEventPhase.inc,
							e.Action.incEventPhase.dec);

				break;
			}
			case SmartActions.Evade:
			{
				if (_me == null)
					break;

				// Reset home position to respawn position if specified in the parameters
				if (e.Action.evade.toRespawnPosition == 0)
					_me.HomePosition = _me.RespawnPosition;

				_me.AI.EnterEvadeMode();
				Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_EVADE: Creature {0} EnterEvadeMode", _me.GUID.ToString());

				break;
			}
			case SmartActions.FleeForAssist:
			{
				if (!_me)
					break;

				_me.DoFleeToGetAssistance();

				if (e.Action.fleeAssist.withEmote != 0)
				{
					var builder = new BroadcastTextBuilder(_me, ChatMsg.MonsterEmote, (uint)BroadcastTextIds.FleeForAssist, _me.Gender);
					Global.CreatureTextMgr.SendChatPacket(_me, builder, ChatMsg.Emote);
				}

				Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction. SMART_ACTION_FLEE_FOR_ASSIST: Creature {0} DoFleeToGetAssistance", _me.GUID.ToString());

				break;
			}
			case SmartActions.CallGroupeventhappens:
			{
				if (unit == null)
					break;

				// If invoker was pet or charm
				var playerCharmed = unit.CharmerOrOwnerPlayerOrPlayerItself;

				if (playerCharmed && GetBaseObject() != null)
				{
					playerCharmed.GroupEventHappens(e.Action.quest.questId, GetBaseObject());

					Log.outDebug(LogFilter.ScriptsAi,
								"SmartScript.ProcessAction: SMART_ACTION_CALL_GROUPEVENTHAPPENS: Player {0}, group credit for quest {1}",
								unit.GUID.ToString(),
								e.Action.quest.questId);
				}

				// Special handling for vehicles
				var vehicle = unit.VehicleKit1;

				if (vehicle != null)
					foreach (var seat in vehicle.Seats)
					{
						var passenger = Global.ObjAccessor.GetPlayer(unit, seat.Value.Passenger.Guid);

						if (passenger != null)
							passenger.GroupEventHappens(e.Action.quest.questId, GetBaseObject());
					}

				break;
			}
			case SmartActions.CombatStop:
			{
				if (!_me)
					break;

				_me.CombatStop(true);
				Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction: SMART_ACTION_COMBAT_STOP: {0} CombatStop", _me.GUID.ToString());

				break;
			}
			case SmartActions.RemoveAurasFromSpell:
			{
				foreach (var target in targets)
				{
					if (!IsUnit(target))
						continue;

					if (e.Action.removeAura.spell != 0)
					{
						ObjectGuid casterGUID = default;

						if (e.Action.removeAura.onlyOwnedAuras != 0)
						{
							if (_me == null)
								break;

							casterGUID = _me.GUID;
						}

						if (e.Action.removeAura.charges != 0)
						{
							var aur = target.AsUnit.GetAura(e.Action.removeAura.spell, casterGUID);

							if (aur != null)
								aur.ModCharges(-(int)e.Action.removeAura.charges, AuraRemoveMode.Expire);
						}

						target.AsUnit.RemoveAura(e.Action.removeAura.spell);
					}
					else
					{
						target.AsUnit.RemoveAllAuras();
					}

					Log.outDebug(LogFilter.ScriptsAi,
								"SmartScript.ProcessAction: SMART_ACTION_REMOVEAURASFROMSPELL: Unit {0}, spell {1}",
								target.GUID.ToString(),
								e.Action.removeAura.spell);
				}

				break;
			}
			case SmartActions.Follow:
			{
				if (!IsSmart())
					break;

				if (targets.Empty())
				{
					((SmartAI)_me.AI).StopFollow(false);

					break;
				}

				foreach (var target in targets)
					if (IsUnit(target))
					{
						var angle = e.Action.follow.angle > 6 ? (e.Action.follow.angle * (float)Math.PI / 180.0f) : e.Action.follow.angle;
						((SmartAI)_me.AI).SetFollow(target.AsUnit, e.Action.follow.dist + 0.1f, angle, e.Action.follow.credit, e.Action.follow.entry, e.Action.follow.creditType);

						Log.outDebug(LogFilter.ScriptsAi,
									"SmartScript.ProcessAction: SMART_ACTION_FOLLOW: Creature {0} following target {1}",
									_me.GUID.ToString(),
									target.GUID.ToString());

						break;
					}

				break;
			}
			case SmartActions.RandomPhase:
			{
				if (GetBaseObject() == null)
					break;

				List<uint> phases = new();
				var randomPhase = e.Action.randomPhase;

				foreach (var id in new[]
						{
							randomPhase.phase1, randomPhase.phase2, randomPhase.phase3, randomPhase.phase4, randomPhase.phase5, randomPhase.phase6
						})
					if (id != 0)
						phases.Add(id);

				var phase = phases.SelectRandom();
				SetPhase(phase);

				Log.outDebug(LogFilter.ScriptsAi,
							"SmartScript.ProcessAction: SMART_ACTION_RANDOM_PHASE: Creature {0} sets event phase to {1}",
							GetBaseObject().GUID.ToString(),
							phase);

				break;
			}
			case SmartActions.RandomPhaseRange:
			{
				if (GetBaseObject() == null)
					break;

				var phase = RandomHelper.URand(e.Action.randomPhaseRange.phaseMin, e.Action.randomPhaseRange.phaseMax);
				SetPhase(phase);

				Log.outDebug(LogFilter.ScriptsAi,
							"SmartScript.ProcessAction: SMART_ACTION_RANDOM_PHASE_RANGE: Creature {0} sets event phase to {1}",
							GetBaseObject().GUID.ToString(),
							phase);

				break;
			}
			case SmartActions.CallKilledmonster:
			{
				if (e.Target.type == SmartTargets.None || e.Target.type == SmartTargets.Self) // Loot recipient and his group members
				{
					if (_me == null)
						break;

					foreach (var tapperGuid in _me.TapList)
					{
						var tapper = Global.ObjAccessor.GetPlayer(_me, tapperGuid);

						if (tapper != null)
						{
							tapper.KilledMonsterCredit(e.Action.killedMonster.creature, _me.GUID);
							Log.outDebug(LogFilter.ScriptsAi, $"SmartScript::ProcessAction: SMART_ACTION_CALL_KILLEDMONSTER: Player {tapper.GUID}, Killcredit: {e.Action.killedMonster.creature}");
						}
					}
				}
				else // Specific target type
				{
					foreach (var target in targets)
						if (IsPlayer(target))
						{
							target.AsPlayer.KilledMonsterCredit(e.Action.killedMonster.creature);

							Log.outDebug(LogFilter.ScriptsAi,
										"SmartScript.ProcessAction: SMART_ACTION_CALL_KILLEDMONSTER: Player {0}, Killcredit: {1}",
										target.GUID.ToString(),
										e.Action.killedMonster.creature);
						}
						else if (IsUnit(target)) // Special handling for vehicles
						{
							var vehicle = target.AsUnit.VehicleKit1;

							if (vehicle != null)
								foreach (var seat in vehicle.Seats)
								{
									var player = Global.ObjAccessor.GetPlayer(target, seat.Value.Passenger.Guid);

									if (player != null)
										player.KilledMonsterCredit(e.Action.killedMonster.creature);
								}
						}
				}

				break;
			}
			case SmartActions.SetInstData:
			{
				var obj = GetBaseObject();

				if (obj == null)
					obj = unit;

				if (obj == null)
					break;

				var instance = obj.InstanceScript;

				if (instance == null)
				{
					Log.outError(LogFilter.Sql, "SmartScript: Event {0} attempt to set instance data without instance script. EntryOrGuid {1}", e.GetEventType(), e.EntryOrGuid);

					break;
				}

				switch (e.Action.setInstanceData.type)
				{
					case 0:
						instance.SetData(e.Action.setInstanceData.field, e.Action.setInstanceData.data);

						Log.outDebug(LogFilter.ScriptsAi,
									"SmartScript.ProcessAction: SMART_ACTION_SET_INST_DATA: SetData Field: {0}, data: {1}",
									e.Action.setInstanceData.field,
									e.Action.setInstanceData.data);

						break;
					case 1:
						instance.SetBossState(e.Action.setInstanceData.field, (EncounterState)e.Action.setInstanceData.data);

						Log.outDebug(LogFilter.ScriptsAi,
									"SmartScript.ProcessAction: SMART_ACTION_SET_INST_DATA: SetBossState BossId: {0}, State: {1} ({2})",
									e.Action.setInstanceData.field,
									e.Action.setInstanceData.data,
									(EncounterState)e.Action.setInstanceData.data);

						break;
					default: // Static analysis
						break;
				}

				break;
			}
			case SmartActions.SetInstData64:
			{
				var obj = GetBaseObject();

				if (obj == null)
					obj = unit;

				if (obj == null)
					break;

				var instance = obj.InstanceScript;

				if (instance == null)
				{
					Log.outError(LogFilter.Sql, "SmartScript: Event {0} attempt to set instance data without instance script. EntryOrGuid {1}", e.GetEventType(), e.EntryOrGuid);

					break;
				}

				if (targets.Empty())
					break;

				instance.SetGuidData(e.Action.setInstanceData64.field, targets.First().GUID);

				Log.outDebug(LogFilter.ScriptsAi,
							"SmartScript.ProcessAction: SMART_ACTION_SET_INST_DATA64: Field: {0}, data: {1}",
							e.Action.setInstanceData64.field,
							targets.First().GUID);

				break;
			}
			case SmartActions.UpdateTemplate:
			{
				foreach (var target in targets)
					if (IsCreature(target))
						target.AsCreature.UpdateEntry(e.Action.updateTemplate.creature, null, e.Action.updateTemplate.updateLevel != 0);

				break;
			}
			case SmartActions.Die:
			{
				if (_me != null && !_me.IsDead)
				{
					_me.KillSelf();
					Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction: SMART_ACTION_DIE: Creature {0}", _me.GUID.ToString());
				}

				break;
			}
			case SmartActions.SetInCombatWithZone:
			{
				if (_me != null && _me.IsAIEnabled)
				{
					_me.AI.DoZoneInCombat();
					Log.outDebug(LogFilter.ScriptsAi, $"SmartScript.ProcessAction: SMART_ACTION_SET_IN_COMBAT_WITH_ZONE: Creature: {_me.GUID}");
				}

				break;
			}
			case SmartActions.CallForHelp:
			{
				if (_me != null)
				{
					_me.CallForHelp(e.Action.callHelp.range);

					if (e.Action.callHelp.withEmote != 0)
					{
						var builder = new BroadcastTextBuilder(_me, ChatMsg.Emote, (uint)BroadcastTextIds.CallForHelp, _me.Gender);
						Global.CreatureTextMgr.SendChatPacket(_me, builder, ChatMsg.MonsterEmote);
					}

					Log.outDebug(LogFilter.ScriptsAi, $"SmartScript.ProcessAction: SMART_ACTION_CALL_FOR_HELP: Creature: {_me.GUID}");
				}

				break;
			}
			case SmartActions.SetSheath:
			{
				if (_me != null)
				{
					_me.Sheath = (SheathState)e.Action.setSheath.sheath;

					Log.outDebug(LogFilter.ScriptsAi,
								"SmartScript.ProcessAction: SMART_ACTION_SET_SHEATH: Creature {0}, State: {1}",
								_me.GUID.ToString(),
								e.Action.setSheath.sheath);
				}

				break;
			}
			case SmartActions.ForceDespawn:
			{
				// there should be at least a world update tick before despawn, to avoid breaking linked actions
				var despawnDelay = TimeSpan.FromMilliseconds(e.Action.forceDespawn.delay);

				if (despawnDelay <= TimeSpan.Zero)
					despawnDelay = TimeSpan.FromMilliseconds(1);

				var forceRespawnTimer = TimeSpan.FromSeconds(e.Action.forceDespawn.forceRespawnTimer);

				foreach (var target in targets)
				{
					var creature = target.AsCreature;

					if (creature != null)
					{
						creature.DespawnOrUnsummon(despawnDelay, forceRespawnTimer);
					}
					else
					{
						var go = target.AsGameObject;

						if (go != null)
							go.DespawnOrUnsummon(despawnDelay, forceRespawnTimer);
					}
				}

				break;
			}
			case SmartActions.SetIngamePhaseId:
			{
				foreach (var target in targets)
					if (e.Action.ingamePhaseId.apply == 1)
						PhasingHandler.AddPhase(target, e.Action.ingamePhaseId.id, true);
					else
						PhasingHandler.RemovePhase(target, e.Action.ingamePhaseId.id, true);

				break;
			}
			case SmartActions.SetIngamePhaseGroup:
			{
				foreach (var target in targets)
					if (e.Action.ingamePhaseGroup.apply == 1)
						PhasingHandler.AddPhaseGroup(target, e.Action.ingamePhaseGroup.groupId, true);
					else
						PhasingHandler.RemovePhaseGroup(target, e.Action.ingamePhaseGroup.groupId, true);

				break;
			}
			case SmartActions.MountToEntryOrModel:
			{
				foreach (var target in targets)
				{
					if (!IsUnit(target))
						continue;

					if (e.Action.morphOrMount.creature != 0 || e.Action.morphOrMount.model != 0)
					{
						if (e.Action.morphOrMount.creature > 0)
						{
							var cInfo = Global.ObjectMgr.GetCreatureTemplate(e.Action.morphOrMount.creature);

							if (cInfo != null)
								target.AsUnit.Mount(ObjectManager.ChooseDisplayId(cInfo).CreatureDisplayId);
						}
						else
						{
							target.AsUnit.Mount(e.Action.morphOrMount.model);
						}
					}
					else
					{
						target.AsUnit.Dismount();
					}
				}

				break;
			}
			case SmartActions.SetInvincibilityHpLevel:
			{
				foreach (var target in targets)
					if (IsCreature(target))
					{
						var ai = (SmartAI)_me.AI;

						if (ai == null)
							continue;

						if (e.Action.invincHP.percent != 0)
							ai.SetInvincibilityHpLevel((uint)target.AsCreature.CountPctFromMaxHealth((int)e.Action.invincHP.percent));
						else
							ai.SetInvincibilityHpLevel(e.Action.invincHP.minHP);
					}

				break;
			}
			case SmartActions.SetData:
			{
				foreach (var target in targets)
				{
					var cTarget = target.AsCreature;

					if (cTarget != null)
					{
						var ai = cTarget.AI;

						if (IsSmart(cTarget, true))
							((SmartAI)ai).SetData(e.Action.setData.field, e.Action.setData.data, _me);
						else
							ai.SetData(e.Action.setData.field, e.Action.setData.data);
					}
					else
					{
						var oTarget = target.AsGameObject;

						if (oTarget != null)
						{
							var ai = oTarget.AI;

							if (IsSmart(oTarget, true))
								((SmartGameObjectAI)ai).SetData(e.Action.setData.field, e.Action.setData.data, _me);
							else
								ai.SetData(e.Action.setData.field, e.Action.setData.data);
						}
					}
				}

				break;
			}
			case SmartActions.AttackStop:
			{
				foreach (var target in targets)
				{
					var unitTarget = target.AsUnit;

					if (unitTarget != null)
						unitTarget.AttackStop();
				}

				break;
			}
			case SmartActions.MoveOffset:
			{
				foreach (var target in targets)
				{
					if (!IsCreature(target))
						continue;

					if (!e.Event.event_flags.HasAnyFlag(SmartEventFlags.WhileCharmed) && IsCharmedCreature(target))
						continue;

					Position pos = target.Location;

					// Use forward/backward/left/right cartesian plane movement
					var o = pos.Orientation;
					var x = (float)(pos.X + (Math.Cos(o - (Math.PI / 2)) * e.Target.x) + (Math.Cos(o) * e.Target.y));
					var y = (float)(pos.Y + (Math.Sin(o - (Math.PI / 2)) * e.Target.x) + (Math.Sin(o) * e.Target.y));
					var z = pos.Z + e.Target.z;
					target.AsCreature.MotionMaster.MovePoint(e.Action.moveOffset.PointId, x, y, z);
				}

				break;
			}
			case SmartActions.SetVisibility:
			{
				foreach (var target in targets)
					if (IsUnit(target))
						target.AsUnit.SetVisible(e.Action.visibility.state != 0);

				break;
			}
			case SmartActions.SetActive:
			{
				foreach (var target in targets)
					target.SetActive(e.Action.active.state != 0);

				break;
			}
			case SmartActions.AttackStart:
			{
				if (_me == null)
					break;

				if (targets.Empty())
					break;

				var target = targets.SelectRandom().AsUnit;

				if (target != null)
					_me.AI.AttackStart(target);

				break;
			}
			case SmartActions.SummonCreature:
			{
				var flags = (SmartActionSummonCreatureFlags)e.Action.summonCreature.flags;
				var preferUnit = flags.HasAnyFlag(SmartActionSummonCreatureFlags.PreferUnit);
				var summoner = preferUnit ? unit : GetBaseObjectOrUnitInvoker(unit);

				if (summoner == null)
					break;

				var privateObjectOwner = ObjectGuid.Empty;

				if (flags.HasAnyFlag(SmartActionSummonCreatureFlags.PersonalSpawn))
					privateObjectOwner = summoner.IsPrivateObject ? summoner.PrivateObjectOwner : summoner.GUID;

				var spawnsCount = Math.Max(e.Action.summonCreature.count, 1u);

				foreach (var target in targets)
				{
					var pos = target.Location.Copy();
					pos.X += e.Target.x;
					pos.Y += e.Target.y;
					pos.Z += e.Target.z;
					pos.Orientation += e.Target.o;

					for (uint counter = 0; counter < spawnsCount; counter++)
					{
						Creature summon = summoner.SummonCreature(e.Action.summonCreature.creature, pos, (TempSummonType)e.Action.summonCreature.type, TimeSpan.FromMilliseconds(e.Action.summonCreature.duration), 0, 0, privateObjectOwner);

						if (summon != null)
							if (e.Action.summonCreature.attackInvoker != 0)
								summon.AI.AttackStart(target.AsUnit);
					}
				}

				if (e.GetTargetType() != SmartTargets.Position)
					break;

				for (uint counter = 0; counter < spawnsCount; counter++)
				{
					Creature summon = summoner.SummonCreature(e.Action.summonCreature.creature, new Position(e.Target.x, e.Target.y, e.Target.z, e.Target.o), (TempSummonType)e.Action.summonCreature.type, TimeSpan.FromMilliseconds(e.Action.summonCreature.duration), 0, 0, privateObjectOwner);

					if (summon != null)
						if (unit != null && e.Action.summonCreature.attackInvoker != 0)
							summon.AI.AttackStart(unit);
				}

				break;
			}
			case SmartActions.SummonGo:
			{
				var summoner = GetBaseObjectOrUnitInvoker(unit);

				if (!summoner)
					break;

				foreach (var target in targets)
				{
					var pos = target.Location.GetPositionWithOffset(new Position(e.Target.x, e.Target.y, e.Target.z, e.Target.o));
					var rot = Quaternion.CreateFromRotationMatrix(Extensions.fromEulerAnglesZYX(pos.Orientation, 0f, 0f));
					summoner.SummonGameObject(e.Action.summonGO.entry, pos, rot, TimeSpan.FromSeconds(e.Action.summonGO.despawnTime), (GameObjectSummonType)e.Action.summonGO.summonType);
				}

				if (e.GetTargetType() != SmartTargets.Position)
					break;

				var _rot = Quaternion.CreateFromRotationMatrix(Extensions.fromEulerAnglesZYX(e.Target.o, 0f, 0f));
				summoner.SummonGameObject(e.Action.summonGO.entry, new Position(e.Target.x, e.Target.y, e.Target.z, e.Target.o), _rot, TimeSpan.FromSeconds(e.Action.summonGO.despawnTime), (GameObjectSummonType)e.Action.summonGO.summonType);

				break;
			}
			case SmartActions.KillUnit:
			{
				foreach (var target in targets)
				{
					if (!IsUnit(target))
						continue;

					target.AsUnit.KillSelf();
				}

				break;
			}
			case SmartActions.AddItem:
			{
				foreach (var target in targets)
				{
					if (!IsPlayer(target))
						continue;

					target.AsPlayer.AddItem(e.Action.item.entry, e.Action.item.count);
				}

				break;
			}
			case SmartActions.RemoveItem:
			{
				foreach (var target in targets)
				{
					if (!IsPlayer(target))
						continue;

					target.AsPlayer.DestroyItemCount(e.Action.item.entry, e.Action.item.count, true);
				}

				break;
			}
			case SmartActions.StoreTargetList:
			{
				StoreTargetList(targets, e.Action.storeTargets.id);

				break;
			}
			case SmartActions.Teleport:
			{
				foreach (var target in targets)
					if (IsPlayer(target))
						target.AsPlayer.TeleportTo(e.Action.teleport.mapID, e.Target.x, e.Target.y, e.Target.z, e.Target.o);
					else if (IsCreature(target))
						target.AsCreature.NearTeleportTo(e.Target.x, e.Target.y, e.Target.z, e.Target.o);

				break;
			}
			case SmartActions.SetDisableGravity:
			{
				if (!IsSmart())
					break;

				((SmartAI)_me.AI).SetDisableGravity(e.Action.setDisableGravity.disable != 0);

				break;
			}
			case SmartActions.SetRun:
			{
				if (!IsSmart())
					break;

				((SmartAI)_me.AI).SetRun(e.Action.setRun.run != 0);

				break;
			}
			case SmartActions.SetCounter:
			{
				if (!targets.Empty())
				{
					foreach (var target in targets)
						if (IsCreature(target))
						{
							var ai = (SmartAI)target.AsCreature.AI;

							if (ai != null)
								ai.GetScript().StoreCounter(e.Action.setCounter.counterId, e.Action.setCounter.value, e.Action.setCounter.reset);
							else
								Log.outError(LogFilter.Sql, "SmartScript: Action target for SMART_ACTION_SET_COUNTER is not using SmartAI, skipping");
						}
						else if (IsGameObject(target))
						{
							var ai = (SmartGameObjectAI)target.AsGameObject.AI;

							if (ai != null)
								ai.GetScript().StoreCounter(e.Action.setCounter.counterId, e.Action.setCounter.value, e.Action.setCounter.reset);
							else
								Log.outError(LogFilter.Sql, "SmartScript: Action target for SMART_ACTION_SET_COUNTER is not using SmartGameObjectAI, skipping");
						}
				}
				else
				{
					StoreCounter(e.Action.setCounter.counterId, e.Action.setCounter.value, e.Action.setCounter.reset);
				}

				break;
			}
			case SmartActions.WpStart:
			{
				if (!IsSmart())
					break;

				var run = e.Action.wpStart.run != 0;
				var entry = e.Action.wpStart.pathID;
				var repeat = e.Action.wpStart.repeat != 0;

				foreach (var target in targets)
					if (IsPlayer(target))
					{
						StoreTargetList(targets, SharedConst.SmartEscortTargets);

						break;
					}

				_me.GetAI<SmartAI>().StartPath(run, entry, repeat, unit);

				var quest = e.Action.wpStart.quest;
				var DespawnTime = e.Action.wpStart.despawnTime;
				_me.GetAI<SmartAI>().EscortQuestID = quest;
				_me.GetAI<SmartAI>().SetDespawnTime(DespawnTime);

				break;
			}
			case SmartActions.WpPause:
			{
				if (!IsSmart())
					break;

				var delay = e.Action.wpPause.delay;
				((SmartAI)_me.AI).PausePath(delay, true);

				break;
			}
			case SmartActions.WpStop:
			{
				if (!IsSmart())
					break;

				var DespawnTime = e.Action.wpStop.despawnTime;
				var quest = e.Action.wpStop.quest;
				var fail = e.Action.wpStop.fail != 0;
				((SmartAI)_me.AI).StopPath(DespawnTime, quest, fail);

				break;
			}
			case SmartActions.WpResume:
			{
				if (!IsSmart())
					break;

				// Set the timer to 1 ms so the path will be resumed on next update loop
				if (_me.GetAI<SmartAI>().CanResumePath())
					_me.GetAI<SmartAI>().SetWPPauseTimer(1);

				break;
			}
			case SmartActions.SetOrientation:
			{
				if (_me == null)
					break;

				if (e.GetTargetType() == SmartTargets.Self)
					_me.SetFacingTo((_me.Transport != null ? _me.TransportHomePosition : _me.HomePosition).Orientation);
				else if (e.GetTargetType() == SmartTargets.Position)
					_me.SetFacingTo(e.Target.o);
				else if (!targets.Empty())
					_me.SetFacingToObject(targets.First());

				break;
			}
			case SmartActions.Playmovie:
			{
				foreach (var target in targets)
				{
					if (!IsPlayer(target))
						continue;

					target.AsPlayer.SendMovieStart(e.Action.movie.entry);
				}

				break;
			}
			case SmartActions.MoveToPos:
			{
				if (!IsSmart())
					break;

				WorldObject target = null;

				/*if (e.GetTargetType() == SmartTargets.CreatureRange || e.GetTargetType() == SmartTargets.CreatureGuid ||
					e.GetTargetType() == SmartTargets.CreatureDistance || e.GetTargetType() == SmartTargets.GameobjectRange ||
					e.GetTargetType() == SmartTargets.GameobjectGuid || e.GetTargetType() == SmartTargets.GameobjectDistance ||
					e.GetTargetType() == SmartTargets.ClosestCreature || e.GetTargetType() == SmartTargets.ClosestGameobject ||
					e.GetTargetType() == SmartTargets.OwnerOrSummoner || e.GetTargetType() == SmartTargets.ActionInvoker ||
					e.GetTargetType() == SmartTargets.ClosestEnemy || e.GetTargetType() == SmartTargets.ClosestFriendly)*/
				{
					// we want to move to random element
					if (!targets.Empty())
						target = targets.SelectRandom();
				}

				if (target == null)
				{
					Position dest = new(e.Target.x, e.Target.y, e.Target.z);

					if (e.Action.moveToPos.transport != 0)
					{
						var trans = _me.DirectTransport;

						if (trans != null)
							trans.CalculatePassengerPosition(dest);
					}

					_me.MotionMaster.MovePoint(e.Action.moveToPos.pointId, dest, e.Action.moveToPos.disablePathfinding == 0);
				}
				else
				{
					var pos = target.Location.Copy();

					if (e.Action.moveToPos.contactDistance > 0)
						target.GetContactPoint(_me, pos, e.Action.moveToPos.contactDistance);

					_me.MotionMaster.MovePoint(e.Action.moveToPos.pointId, pos.X + e.Target.x, pos.Y + e.Target.y, pos.Z + e.Target.z, e.Action.moveToPos.disablePathfinding == 0);
				}

				break;
			}
			case SmartActions.EnableTempGobj:
			{
				foreach (var target in targets)
					if (IsCreature(target))
					{
						Log.outWarn(LogFilter.Sql, $"Invalid creature target '{target.GetName()}' (entry {target.Entry}, spawnId {target.AsCreature.SpawnId}) specified for SMART_ACTION_ENABLE_TEMP_GOBJ");
					}
					else if (IsGameObject(target))
					{
						if (target.AsGameObject.IsSpawnedByDefault)
							Log.outWarn(LogFilter.Sql, $"Invalid gameobject target '{target.GetName()}' (entry {target.Entry}, spawnId {target.AsGameObject.SpawnId}) for SMART_ACTION_ENABLE_TEMP_GOBJ - the object is spawned by default");
						else
							target.AsGameObject.SetRespawnTime((int)e.Action.enableTempGO.duration);
					}

				break;
			}
			case SmartActions.CloseGossip:
			{
				foreach (var target in targets)
					if (IsPlayer(target))
						target.AsPlayer.PlayerTalkClass.SendCloseGossip();

				break;
			}
			case SmartActions.Equip:
			{
				foreach (var target in targets)
				{
					var npc = target.AsCreature;

					if (npc != null)
					{
						var slot = new EquipmentItem[SharedConst.MaxEquipmentItems];
						var equipId = (sbyte)e.Action.equip.entry;

						if (equipId != 0)
						{
							var eInfo = Global.ObjectMgr.GetEquipmentInfo(npc.Entry, equipId);

							if (eInfo == null)
							{
								Log.outError(LogFilter.Sql, "SmartScript: SMART_ACTION_EQUIP uses non-existent equipment info id {0} for creature {1}", equipId, npc.Entry);

								break;
							}

							npc.CurrentEquipmentId = (byte)equipId;
							Array.Copy(eInfo.Items, slot, SharedConst.MaxEquipmentItems);
						}
						else
						{
							slot[0].ItemId = e.Action.equip.slot1;
							slot[1].ItemId = e.Action.equip.slot2;
							slot[2].ItemId = e.Action.equip.slot3;
						}

						for (uint i = 0; i < SharedConst.MaxEquipmentItems; ++i)
							if (e.Action.equip.mask == 0 || (e.Action.equip.mask & (1 << (int)i)) != 0)
								npc.SetVirtualItem(i, slot[i].ItemId, slot[i].AppearanceModId, slot[i].ItemVisual);
					}
				}

				break;
			}
			case SmartActions.CreateTimedEvent:
			{
				SmartEvent ne = new();
				ne.type = SmartEvents.Update;
				ne.event_chance = e.Action.timeEvent.chance;

				if (ne.event_chance == 0)
					ne.event_chance = 100;

				ne.minMaxRepeat.min = e.Action.timeEvent.min;
				ne.minMaxRepeat.max = e.Action.timeEvent.max;
				ne.minMaxRepeat.repeatMin = e.Action.timeEvent.repeatMin;
				ne.minMaxRepeat.repeatMax = e.Action.timeEvent.repeatMax;

				ne.event_flags = 0;

				if (ne.minMaxRepeat.repeatMin == 0 && ne.minMaxRepeat.repeatMax == 0)
					ne.event_flags |= SmartEventFlags.NotRepeatable;

				SmartAction ac = new();
				ac.type = SmartActions.TriggerTimedEvent;
				ac.timeEvent.id = e.Action.timeEvent.id;

				SmartScriptHolder ev = new();
				ev.Event = ne;
				ev.EventId = e.Action.timeEvent.id;
				ev.Target = e.Target;
				ev.Action = ac;
				InitTimer(ev);
				_storedEvents.Add(ev);

				break;
			}
			case SmartActions.TriggerTimedEvent:
				ProcessEventsFor(SmartEvents.TimedEventTriggered, null, e.Action.timeEvent.id);

				// remove this event if not repeatable
				if (e.Event.event_flags.HasAnyFlag(SmartEventFlags.NotRepeatable))
					_remIDs.Add(e.Action.timeEvent.id);

				break;
			case SmartActions.RemoveTimedEvent:
				_remIDs.Add(e.Action.timeEvent.id);

				break;
			case SmartActions.CallScriptReset:
				SetPhase(0);
				OnReset();

				break;
			case SmartActions.SetRangedMovement:
			{
				if (!IsSmart())
					break;

				float attackDistance = e.Action.setRangedMovement.distance;
				var attackAngle = e.Action.setRangedMovement.angle / 180.0f * MathFunctions.PI;

				foreach (var target in targets)
				{
					var creature = target.AsCreature;

					if (creature != null)
						if (IsSmart(creature) && creature.Victim != null)
							if (((SmartAI)creature.AI).CanCombatMove())
								creature.MotionMaster.MoveChase(creature.Victim, attackDistance, attackAngle);
				}

				break;
			}
			case SmartActions.CallTimedActionlist:
			{
				if (e.GetTargetType() == SmartTargets.None)
				{
					Log.outError(LogFilter.Sql, "SmartScript: Entry {0} SourceType {1} Event {2} Action {3} is using TARGET_NONE(0) for Script9 target. Please correct target_type in database.", e.EntryOrGuid, e.GetScriptType(), e.GetEventType(), e.GetActionType());

					break;
				}

				foreach (var target in targets)
				{
					var creature = target.AsCreature;

					if (creature != null)
					{
						if (IsSmart(creature))
							creature.GetAI<SmartAI>().SetTimedActionList(e, e.Action.timedActionList.id, GetLastInvoker());
					}
					else
					{
						var go = target.AsGameObject;

						if (go != null)
						{
							if (IsSmart(go))
								go.GetAI<SmartGameObjectAI>().SetTimedActionList(e, e.Action.timedActionList.id, GetLastInvoker());
						}
						else
						{
							var areaTriggerTarget = target.AsAreaTrigger;

							if (areaTriggerTarget != null)
								areaTriggerTarget.ForEachAreaTriggerScript<IAreaTriggerSmartScript>(a => a.SetTimedActionList(e, e.Action.timedActionList.id, GetLastInvoker()));
						}
					}
				}

				break;
			}
			case SmartActions.SetNpcFlag:
			{
				foreach (var target in targets)
					if (IsUnit(target))
						target.AsUnit.ReplaceAllNpcFlags((NPCFlags)e.Action.flag.flag);

				break;
			}
			case SmartActions.AddNpcFlag:
			{
				foreach (var target in targets)
					if (IsUnit(target))
						target.AsUnit.SetNpcFlag((NPCFlags)e.Action.flag.flag);

				break;
			}
			case SmartActions.RemoveNpcFlag:
			{
				foreach (var target in targets)
					if (IsUnit(target))
						target.AsUnit.RemoveNpcFlag((NPCFlags)e.Action.flag.flag);

				break;
			}
			case SmartActions.CrossCast:
			{
				if (targets.Empty())
					break;

				var casters = GetTargets(CreateSmartEvent(SmartEvents.UpdateIc, 0, 0, 0, 0, 0, 0, SmartActions.None, 0, 0, 0, 0, 0, 0, 0, (SmartTargets)e.Action.crossCast.targetType, e.Action.crossCast.targetParam1, e.Action.crossCast.targetParam2, e.Action.crossCast.targetParam3, 0, 0), unit);

				foreach (var caster in casters)
				{
					if (!IsUnit(caster))
						continue;

					var casterUnit = caster.AsUnit;
					var interruptedSpell = false;

					foreach (var target in targets)
					{
						if (!IsUnit(target))
							continue;

						if (!(e.Action.crossCast.castFlags.HasAnyFlag((uint)SmartCastFlags.AuraNotPresent)) || !target.AsUnit.HasAura(e.Action.crossCast.spell))
						{
							if (!interruptedSpell && e.Action.crossCast.castFlags.HasAnyFlag((uint)SmartCastFlags.InterruptPrevious))
							{
								casterUnit.InterruptNonMeleeSpells(false);
								interruptedSpell = true;
							}

							casterUnit.CastSpell(target.AsUnit, e.Action.crossCast.spell, e.Action.crossCast.castFlags.HasAnyFlag((uint)SmartCastFlags.Triggered));
						}
						else
						{
							Log.outDebug(LogFilter.ScriptsAi, "Spell {0} not cast because it has flag SMARTCAST_AURA_NOT_PRESENT and the target ({1}) already has the aura", e.Action.crossCast.spell, target.GUID.ToString());
						}
					}
				}

				break;
			}
			case SmartActions.CallRandomTimedActionlist:
			{
				List<uint> actionLists = new();
				var randTimedActionList = e.Action.randTimedActionList;

				foreach (var id in new[]
						{
							randTimedActionList.actionList1, randTimedActionList.actionList2, randTimedActionList.actionList3, randTimedActionList.actionList4, randTimedActionList.actionList5, randTimedActionList.actionList6
						})
					if (id != 0)
						actionLists.Add(id);

				if (e.GetTargetType() == SmartTargets.None)
				{
					Log.outError(LogFilter.Sql, "SmartScript: Entry {0} SourceType {1} Event {2} Action {3} is using TARGET_NONE(0) for Script9 target. Please correct target_type in database.", e.EntryOrGuid, e.GetScriptType(), e.GetEventType(), e.GetActionType());

					break;
				}

				var randomId = actionLists.SelectRandom();

				foreach (var target in targets)
				{
					var creature = target.AsCreature;

					if (creature != null)
					{
						if (IsSmart(creature))
							creature.GetAI<SmartAI>().SetTimedActionList(e, randomId, GetLastInvoker());
					}
					else
					{
						var go = target.AsGameObject;

						if (go != null)
						{
							if (IsSmart(go))
								go.GetAI<SmartGameObjectAI>().SetTimedActionList(e, randomId, GetLastInvoker());
						}
						else
						{
							var areaTriggerTarget = target.AsAreaTrigger;

							if (areaTriggerTarget != null)
								areaTriggerTarget.ForEachAreaTriggerScript<IAreaTriggerSmartScript>(a => a.SetTimedActionList(e, randomId, GetLastInvoker()));
						}
					}
				}

				break;
			}
			case SmartActions.CallRandomRangeTimedActionlist:
			{
				var id = RandomHelper.URand(e.Action.randRangeTimedActionList.idMin, e.Action.randRangeTimedActionList.idMax);

				if (e.GetTargetType() == SmartTargets.None)
				{
					Log.outError(LogFilter.Sql, "SmartScript: Entry {0} SourceType {1} Event {2} Action {3} is using TARGET_NONE(0) for Script9 target. Please correct target_type in database.", e.EntryOrGuid, e.GetScriptType(), e.GetEventType(), e.GetActionType());

					break;
				}

				foreach (var target in targets)
				{
					var creature = target.AsCreature;

					if (creature != null)
					{
						if (IsSmart(creature))
							creature.GetAI<SmartAI>().SetTimedActionList(e, id, GetLastInvoker());
					}
					else
					{
						var go = target.AsGameObject;

						if (go != null)
						{
							if (IsSmart(go))
								go.GetAI<SmartGameObjectAI>().SetTimedActionList(e, id, GetLastInvoker());
						}
						else
						{
							var areaTriggerTarget = target.AsAreaTrigger;

							if (areaTriggerTarget != null)
								areaTriggerTarget.ForEachAreaTriggerScript<IAreaTriggerSmartScript>(a => a.SetTimedActionList(e, id, GetLastInvoker()));
						}
					}
				}

				break;
			}
			case SmartActions.ActivateTaxi:
			{
				foreach (var target in targets)
					if (IsPlayer(target))
						target.AsPlayer.ActivateTaxiPathTo(e.Action.taxi.id);

				break;
			}
			case SmartActions.RandomMove:
			{
				var foundTarget = false;

				foreach (var obj in targets)
					if (IsCreature(obj))
					{
						if (e.Action.moveRandom.distance != 0)
							obj.AsCreature.MotionMaster.MoveRandom(e.Action.moveRandom.distance);
						else
							obj.AsCreature.MotionMaster.MoveIdle();
					}

				if (!foundTarget && _me != null && IsCreature(_me))
				{
					if (e.Action.moveRandom.distance != 0)
						_me.MotionMaster.MoveRandom(e.Action.moveRandom.distance);
					else
						_me.MotionMaster.MoveIdle();
				}

				break;
			}
			case SmartActions.SetUnitFieldBytes1:
			{
				foreach (var target in targets)
					if (IsUnit(target))
						switch (e.Action.setunitByte.type)
						{
							case 0:
								target.AsUnit.SetStandState((UnitStandStateType)e.Action.setunitByte.byte1);

								break;
							case 1:
								// pet talent points
								break;
							case 2:
								target.AsUnit.SetVisFlag((UnitVisFlags)e.Action.setunitByte.byte1);

								break;
							case 3:
								target.AsUnit.SetAnimTier((AnimTier)e.Action.setunitByte.byte1);

								break;
						}

				break;
			}
			case SmartActions.RemoveUnitFieldBytes1:
			{
				foreach (var target in targets)
					if (IsUnit(target))
						switch (e.Action.setunitByte.type)
						{
							case 0:
								target.AsUnit.SetStandState(UnitStandStateType.Stand);

								break;
							case 1:
								// pet talent points
								break;
							case 2:
								target.AsUnit.RemoveVisFlag((UnitVisFlags)e.Action.setunitByte.byte1);

								break;
							case 3:
								target.AsUnit.SetAnimTier(AnimTier.Ground);

								break;
						}

				break;
			}
			case SmartActions.InterruptSpell:
			{
				foreach (var target in targets)
					if (IsUnit(target))
						target.AsUnit.InterruptNonMeleeSpells(e.Action.interruptSpellCasting.withDelayed != 0, e.Action.interruptSpellCasting.spell_id, e.Action.interruptSpellCasting.withInstant != 0);

				break;
			}
			case SmartActions.AddDynamicFlag:
			{
				foreach (var target in targets)
					if (IsUnit(target))
						target.AsUnit.SetDynamicFlag((UnitDynFlags)e.Action.flag.flag);

				break;
			}
			case SmartActions.RemoveDynamicFlag:
			{
				foreach (var target in targets)
					if (IsUnit(target))
						target.AsUnit.RemoveDynamicFlag((UnitDynFlags)e.Action.flag.flag);

				break;
			}
			case SmartActions.JumpToPos:
			{
				WorldObject target = null;

				if (!targets.Empty())
					target = targets.SelectRandom();

				Position pos = new(e.Target.x, e.Target.y, e.Target.z);

				if (target)
				{
					var tpos = target.Location.Copy();

					if (e.Action.jump.ContactDistance > 0)
						target.GetContactPoint(_me, tpos, e.Action.jump.ContactDistance);

					pos = new Position(tpos.X + e.Target.x, tpos.Y + e.Target.y, tpos.Z + e.Target.z);
				}

				if (e.Action.jump.Gravity != 0 || e.Action.jump.UseDefaultGravity != 0)
				{
					var gravity = e.Action.jump.UseDefaultGravity != 0 ? (float)MotionMaster.gravity : e.Action.jump.Gravity;
					_me.MotionMaster.MoveJumpWithGravity(pos, e.Action.jump.SpeedXY, gravity, e.Action.jump.PointId);
				}
				else
				{
					_me.MotionMaster.MoveJump(pos, e.Action.jump.SpeedXY, e.Action.jump.SpeedZ, e.Action.jump.PointId);
				}

				break;
			}
			case SmartActions.GoSetLootState:
			{
				foreach (var target in targets)
					if (IsGameObject(target))
						target.AsGameObject.SetLootState((LootState)e.Action.setGoLootState.state);

				break;
			}
			case SmartActions.GoSetGoState:
			{
				foreach (var target in targets)
					if (IsGameObject(target))
						target.AsGameObject.SetGoState((GameObjectState)e.Action.goState.state);

				break;
			}
			case SmartActions.SendTargetToTarget:
			{
				var baseObject = GetBaseObject();

				if (baseObject == null)
					baseObject = unit;

				if (baseObject == null)
					break;

				var storedTargets = GetStoredTargetList(e.Action.sendTargetToTarget.id, baseObject);

				if (storedTargets == null)
					break;

				foreach (var target in targets)
					if (IsCreature(target))
					{
						var ai = (SmartAI)target.AsCreature.AI;

						if (ai != null)
							ai.GetScript().StoreTargetList(new List<WorldObject>(storedTargets), e.Action.sendTargetToTarget.id); // store a copy of target list
						else
							Log.outError(LogFilter.Sql, "SmartScript: Action target for SMART_ACTION_SEND_TARGET_TO_TARGET is not using SmartAI, skipping");
					}
					else if (IsGameObject(target))
					{
						var ai = (SmartGameObjectAI)target.AsGameObject.AI;

						if (ai != null)
							ai.GetScript().StoreTargetList(new List<WorldObject>(storedTargets), e.Action.sendTargetToTarget.id); // store a copy of target list
						else
							Log.outError(LogFilter.Sql, "SmartScript: Action target for SMART_ACTION_SEND_TARGET_TO_TARGET is not using SmartGameObjectAI, skipping");
					}

				break;
			}
			case SmartActions.SendGossipMenu:
			{
				if (GetBaseObject() == null || !IsSmart())
					break;

				Log.outDebug(LogFilter.ScriptsAi,
							"SmartScript.ProcessAction. SMART_ACTION_SEND_GOSSIP_MENU: gossipMenuId {0}, gossipNpcTextId {1}",
							e.Action.sendGossipMenu.gossipMenuId,
							e.Action.sendGossipMenu.gossipNpcTextId);

				// override default gossip
				if (_me)
					((SmartAI)_me.AI).SetGossipReturn(true);
				else if (_go)
					((SmartGameObjectAI)_go.AI).SetGossipReturn(true);

				foreach (var target in targets)
				{
					var player = target.AsPlayer;

					if (player != null)
					{
						if (e.Action.sendGossipMenu.gossipMenuId != 0)
							player.PrepareGossipMenu(GetBaseObject(), e.Action.sendGossipMenu.gossipMenuId, true);
						else
							player.PlayerTalkClass.ClearMenus();

						var gossipNpcTextId = e.Action.sendGossipMenu.gossipNpcTextId;

						if (gossipNpcTextId == 0)
							gossipNpcTextId = player.GetGossipTextId(e.Action.sendGossipMenu.gossipMenuId, GetBaseObject());

						player.PlayerTalkClass.SendGossipMenu(gossipNpcTextId, GetBaseObject().GUID);
					}
				}

				break;
			}
			case SmartActions.SetHomePos:
			{
				foreach (var target in targets)
					if (IsCreature(target))
					{
						if (e.GetTargetType() == SmartTargets.Self)
							target.AsCreature.SetHomePosition(_me.Location.X, _me.Location.Y, _me.Location.Z, _me.Location.Orientation);
						else if (e.GetTargetType() == SmartTargets.Position)
							target.AsCreature.SetHomePosition(e.Target.x, e.Target.y, e.Target.z, e.Target.o);
						else if (e.GetTargetType() == SmartTargets.CreatureRange ||
								e.GetTargetType() == SmartTargets.CreatureGuid ||
								e.GetTargetType() == SmartTargets.CreatureDistance ||
								e.GetTargetType() == SmartTargets.GameobjectRange ||
								e.GetTargetType() == SmartTargets.GameobjectGuid ||
								e.GetTargetType() == SmartTargets.GameobjectDistance ||
								e.GetTargetType() == SmartTargets.ClosestCreature ||
								e.GetTargetType() == SmartTargets.ClosestGameobject ||
								e.GetTargetType() == SmartTargets.OwnerOrSummoner ||
								e.GetTargetType() == SmartTargets.ActionInvoker ||
								e.GetTargetType() == SmartTargets.ClosestEnemy ||
								e.GetTargetType() == SmartTargets.ClosestFriendly ||
								e.GetTargetType() == SmartTargets.ClosestUnspawnedGameobject)
							target.AsCreature.SetHomePosition(target.Location.X, target.Location.Y, target.Location.Z, target.Location.Orientation);
						else
							Log.outError(LogFilter.Sql, "SmartScript: Action target for SMART_ACTION_SET_HOME_POS is invalid, skipping");
					}

				break;
			}
			case SmartActions.SetHealthRegen:
			{
				foreach (var target in targets)
					if (IsCreature(target))
						target.AsCreature.SetRegenerateHealth(e.Action.setHealthRegen.regenHealth != 0);

				break;
			}
			case SmartActions.SetRoot:
			{
				foreach (var target in targets)
					if (IsCreature(target))
						target.AsCreature.SetControlled(e.Action.setRoot.root != 0, UnitState.Root);

				break;
			}
			case SmartActions.SummonCreatureGroup:
			{
				GetBaseObject().SummonCreatureGroup((byte)e.Action.creatureGroup.group, out var summonList);

				foreach (var summon in summonList)
					if (unit == null && e.Action.creatureGroup.attackInvoker != 0)
						summon.AI.AttackStart(unit);

				break;
			}
			case SmartActions.SetPower:
			{
				foreach (var target in targets)
					if (IsUnit(target))
						target.AsUnit.SetPower((PowerType)e.Action.power.powerType, (int)e.Action.power.newPower);

				break;
			}
			case SmartActions.AddPower:
			{
				foreach (var target in targets)
					if (IsUnit(target))
						target.AsUnit.SetPower((PowerType)e.Action.power.powerType, target.AsUnit.GetPower((PowerType)e.Action.power.powerType) + (int)e.Action.power.newPower);

				break;
			}
			case SmartActions.RemovePower:
			{
				foreach (var target in targets)
					if (IsUnit(target))
						target.AsUnit.SetPower((PowerType)e.Action.power.powerType, target.AsUnit.GetPower((PowerType)e.Action.power.powerType) - (int)e.Action.power.newPower);

				break;
			}
			case SmartActions.GameEventStop:
			{
				var eventId = (ushort)e.Action.gameEventStop.id;

				if (!Global.GameEventMgr.IsActiveEvent(eventId))
				{
					Log.outError(LogFilter.Sql, "SmartScript.ProcessAction: At case SMART_ACTION_GAME_EVENT_STOP, inactive event (id: {0})", eventId);

					break;
				}

				Global.GameEventMgr.StopEvent(eventId, true);

				break;
			}
			case SmartActions.GameEventStart:
			{
				var eventId = (ushort)e.Action.gameEventStart.id;

				if (Global.GameEventMgr.IsActiveEvent(eventId))
				{
					Log.outError(LogFilter.Sql, "SmartScript.ProcessAction: At case SMART_ACTION_GAME_EVENT_START, already activated event (id: {0})", eventId);

					break;
				}

				Global.GameEventMgr.StartEvent(eventId, true);

				break;
			}
			case SmartActions.StartClosestWaypoint:
			{
				List<uint> waypoints = new();
				var closestWaypointFromList = e.Action.closestWaypointFromList;

				foreach (var id in new[]
						{
							closestWaypointFromList.wp1, closestWaypointFromList.wp2, closestWaypointFromList.wp3, closestWaypointFromList.wp4, closestWaypointFromList.wp5, closestWaypointFromList.wp6
						})
					if (id != 0)
						waypoints.Add(id);

				var distanceToClosest = float.MaxValue;
				uint closestPathId = 0;
				uint closestWaypointId = 0;

				foreach (var target in targets)
				{
					var creature = target.AsCreature;

					if (creature != null)
						if (IsSmart(creature))
						{
							foreach (var pathId in waypoints)
							{
								var path = Global.SmartAIMgr.GetPath(pathId);

								if (path == null || path.nodes.Empty())
									continue;

								foreach (var waypoint in path.nodes)
								{
									var distToThisPath = creature.GetDistance(waypoint.x, waypoint.y, waypoint.z);

									if (distToThisPath < distanceToClosest)
									{
										distanceToClosest = distToThisPath;
										closestPathId = pathId;
										closestWaypointId = waypoint.id;
									}
								}
							}

							if (closestPathId != 0)
								((SmartAI)creature.AI).StartPath(false, closestPathId, true, null, closestWaypointId);
						}
				}

				break;
			}
			case SmartActions.RandomSound:
			{
				List<uint> sounds = new();
				var randomSound = e.Action.randomSound;

				foreach (var id in new[]
						{
							randomSound.sound1, randomSound.sound2, randomSound.sound3, randomSound.sound4
						})
					if (id != 0)
						sounds.Add(id);

				var onlySelf = e.Action.randomSound.onlySelf != 0;

				foreach (var target in targets)
					if (IsUnit(target))
					{
						var sound = sounds.SelectRandom();

						if (e.Action.randomSound.distance == 1)
							target.PlayDistanceSound(sound, onlySelf ? target.AsPlayer : null);
						else
							target.PlayDirectSound(sound, onlySelf ? target.AsPlayer : null);

						Log.outDebug(LogFilter.ScriptsAi,
									"SmartScript.ProcessAction:: SMART_ACTION_RANDOM_SOUND: target: {0} ({1}), sound: {2}, onlyself: {3}",
									target.GetName(),
									target.GUID.ToString(),
									sound,
									onlySelf);
					}

				break;
			}
			case SmartActions.SetCorpseDelay:
			{
				foreach (var target in targets)
					if (IsCreature(target))
						target.AsCreature.SetCorpseDelay(e.Action.corpseDelay.timer, e.Action.corpseDelay.includeDecayRatio == 0);

				break;
			}
			case SmartActions.SpawnSpawngroup:
			{
				if (e.Action.groupSpawn.minDelay == 0 && e.Action.groupSpawn.maxDelay == 0)
				{
					var ignoreRespawn = ((e.Action.groupSpawn.spawnflags & (uint)SmartAiSpawnFlags.IgnoreRespawn) != 0);
					var force = ((e.Action.groupSpawn.spawnflags & (uint)SmartAiSpawnFlags.ForceSpawn) != 0);

					// Instant spawn
					GetBaseObject()
						.
						// Instant spawn
						Map.SpawnGroupSpawn(e.Action.groupSpawn.groupId, ignoreRespawn, force);
				}
				else
				{
					// Delayed spawn (use values from parameter to schedule event to call us back
					SmartEvent ne = new();
					ne.type = SmartEvents.Update;
					ne.event_chance = 100;

					ne.minMaxRepeat.min = e.Action.groupSpawn.minDelay;
					ne.minMaxRepeat.max = e.Action.groupSpawn.maxDelay;
					ne.minMaxRepeat.repeatMin = 0;
					ne.minMaxRepeat.repeatMax = 0;

					ne.event_flags = 0;
					ne.event_flags |= SmartEventFlags.NotRepeatable;

					SmartAction ac = new();
					ac.type = SmartActions.SpawnSpawngroup;
					ac.groupSpawn.groupId = e.Action.groupSpawn.groupId;
					ac.groupSpawn.minDelay = 0;
					ac.groupSpawn.maxDelay = 0;
					ac.groupSpawn.spawnflags = e.Action.groupSpawn.spawnflags;
					ac.timeEvent.id = e.Action.timeEvent.id;

					SmartScriptHolder ev = new();
					ev.Event = ne;
					ev.EventId = e.EventId;
					ev.Target = e.Target;
					ev.Action = ac;
					InitTimer(ev);
					_storedEvents.Add(ev);
				}

				break;
			}
			case SmartActions.DespawnSpawngroup:
			{
				if (e.Action.groupSpawn.minDelay == 0 && e.Action.groupSpawn.maxDelay == 0)
				{
					var deleteRespawnTimes = ((e.Action.groupSpawn.spawnflags & (uint)SmartAiSpawnFlags.NosaveRespawn) != 0);

					// Instant spawn
					GetBaseObject()
						.
						// Instant spawn
						Map.SpawnGroupSpawn(e.Action.groupSpawn.groupId, deleteRespawnTimes);
				}
				else
				{
					// Delayed spawn (use values from parameter to schedule event to call us back
					SmartEvent ne = new();
					ne.type = SmartEvents.Update;
					ne.event_chance = 100;

					ne.minMaxRepeat.min = e.Action.groupSpawn.minDelay;
					ne.minMaxRepeat.max = e.Action.groupSpawn.maxDelay;
					ne.minMaxRepeat.repeatMin = 0;
					ne.minMaxRepeat.repeatMax = 0;

					ne.event_flags = 0;
					ne.event_flags |= SmartEventFlags.NotRepeatable;

					SmartAction ac = new();
					ac.type = SmartActions.DespawnSpawngroup;
					ac.groupSpawn.groupId = e.Action.groupSpawn.groupId;
					ac.groupSpawn.minDelay = 0;
					ac.groupSpawn.maxDelay = 0;
					ac.groupSpawn.spawnflags = e.Action.groupSpawn.spawnflags;
					ac.timeEvent.id = e.Action.timeEvent.id;

					SmartScriptHolder ev = new();
					ev.Event = ne;
					ev.EventId = e.EventId;
					ev.Target = e.Target;
					ev.Action = ac;
					InitTimer(ev);
					_storedEvents.Add(ev);
				}

				break;
			}
			case SmartActions.DisableEvade:
			{
				if (!IsSmart())
					break;

				((SmartAI)_me.AI).SetEvadeDisabled(e.Action.disableEvade.disable != 0);

				break;
			}
			case SmartActions.AddThreat:
			{
				if (!_me.CanHaveThreatList)
					break;

				foreach (var target in targets)
					if (IsUnit(target))
						_me.GetThreatManager().AddThreat(target.AsUnit, (float)(e.Action.threat.threatINC - (float)e.Action.threat.threatDEC), null, true, true);

				break;
			}
			case SmartActions.LoadEquipment:
			{
				foreach (var target in targets)
					if (IsCreature(target))
						target.AsCreature.LoadEquipment((int)e.Action.loadEquipment.id, e.Action.loadEquipment.force != 0);

				break;
			}
			case SmartActions.TriggerRandomTimedEvent:
			{
				var eventId = RandomHelper.URand(e.Action.randomTimedEvent.minId, e.Action.randomTimedEvent.maxId);
				ProcessEventsFor(SmartEvents.TimedEventTriggered, null, eventId);

				break;
			}
			case SmartActions.PauseMovement:
			{
				foreach (var target in targets)
					if (IsUnit(target))
						target.AsUnit.PauseMovement(e.Action.pauseMovement.pauseTimer, (MovementSlot)e.Action.pauseMovement.movementSlot, e.Action.pauseMovement.force != 0);

				break;
			}
			case SmartActions.RespawnBySpawnId:
			{
				Map map = null;
				var obj = GetBaseObject();

				if (obj != null)
					map = obj.Map;
				else if (!targets.Empty())
					map = targets.First().Map;

				if (map)
					map.Respawn((SpawnObjectType)e.Action.respawnData.spawnType, e.Action.respawnData.spawnId);
				else
					Log.outError(LogFilter.Sql, $"SmartScript.ProcessAction: Entry {e.EntryOrGuid} SourceType {e.GetScriptType()}, Event {e.EventId} - tries to respawn by spawnId but does not provide a map");

				break;
			}
			case SmartActions.PlayAnimkit:
			{
				foreach (var target in targets)
					if (IsCreature(target))
					{
						if (e.Action.animKit.type == 0)
							target.AsCreature.PlayOneShotAnimKitId((ushort)e.Action.animKit.animKit);
						else if (e.Action.animKit.type == 1)
							target.AsCreature.SetAIAnimKitId((ushort)e.Action.animKit.animKit);
						else if (e.Action.animKit.type == 2)
							target.AsCreature.SetMeleeAnimKitId((ushort)e.Action.animKit.animKit);
						else if (e.Action.animKit.type == 3)
							target.AsCreature.SetMovementAnimKitId((ushort)e.Action.animKit.animKit);

						Log.outDebug(LogFilter.ScriptsAi, $"SmartScript::ProcessAction:: SMART_ACTION_PLAY_ANIMKIT: target: {target.GetName()} ({target.GUID}), AnimKit: {e.Action.animKit.animKit}, Type: {e.Action.animKit.type}");
					}
					else if (IsGameObject(target))
					{
						switch (e.Action.animKit.type)
						{
							case 0:
								target.AsGameObject.SetAnimKitId((ushort)e.Action.animKit.animKit, true);

								break;
							case 1:
								target.AsGameObject.SetAnimKitId((ushort)e.Action.animKit.animKit, false);

								break;
							default:
								break;
						}

						Log.outDebug(LogFilter.ScriptsAi, "SmartScript.ProcessAction:: SMART_ACTION_PLAY_ANIMKIT: target: {0} ({1}), AnimKit: {2}, Type: {3}", target.GetName(), target.GUID.ToString(), e.Action.animKit.animKit, e.Action.animKit.type);
					}

				break;
			}
			case SmartActions.ScenePlay:
			{
				foreach (var target in targets)
				{
					var playerTarget = target.AsPlayer;

					if (playerTarget)
						playerTarget.SceneMgr.PlayScene(e.Action.scene.sceneId);
				}

				break;
			}
			case SmartActions.SceneCancel:
			{
				foreach (var target in targets)
				{
					var playerTarget = target.AsPlayer;

					if (playerTarget)
						playerTarget.SceneMgr.CancelSceneBySceneId(e.Action.scene.sceneId);
				}

				break;
			}
			case SmartActions.PlayCinematic:
			{
				foreach (var target in targets)
				{
					if (!IsPlayer(target))
						continue;

					target.AsPlayer.SendCinematicStart(e.Action.cinematic.entry);
				}

				break;
			}
			case SmartActions.SetMovementSpeed:
			{
				var speedInteger = e.Action.movementSpeed.speedInteger;
				var speedFraction = e.Action.movementSpeed.speedFraction;
				var speed = (float)((float)speedInteger + (float)speedFraction / Math.Pow(10, Math.Floor(Math.Log10((float)(speedFraction != 0 ? speedFraction : 1)) + 1)));

				foreach (var target in targets)
					if (IsCreature(target))
						target.AsCreature.SetSpeed((UnitMoveType)e.Action.movementSpeed.movementType, speed);

				break;
			}
			case SmartActions.PlaySpellVisualKit:
			{
				foreach (var target in targets)
					if (IsUnit(target))
					{
						target.AsUnit.SendPlaySpellVisualKit(e.Action.spellVisualKit.spellVisualKitId,
															e.Action.spellVisualKit.kitType,
															e.Action.spellVisualKit.duration);

						Log.outDebug(LogFilter.ScriptsAi, $"SmartScript::ProcessAction:: SMART_ACTION_PLAY_SPELL_VISUAL_KIT: target: {target.GetName()} ({target.GUID}), SpellVisualKit: {e.Action.spellVisualKit.spellVisualKitId}");
					}

				break;
			}
			case SmartActions.OverrideLight:
			{
				var obj = GetBaseObject();

				if (obj != null)
				{
					obj.Map.SetZoneOverrideLight(e.Action.overrideLight.zoneId, e.Action.overrideLight.areaLightId, e.Action.overrideLight.overrideLightId, TimeSpan.FromMilliseconds(e.Action.overrideLight.transitionMilliseconds));

					Log.outDebug(LogFilter.ScriptsAi,
								$"SmartScript::ProcessAction: SMART_ACTION_OVERRIDE_LIGHT: {obj.GUID} sets zone override light (zoneId: {e.Action.overrideLight.zoneId}, " +
								$"areaLightId: {e.Action.overrideLight.areaLightId}, overrideLightId: {e.Action.overrideLight.overrideLightId}, transitionMilliseconds: {e.Action.overrideLight.transitionMilliseconds})");
				}

				break;
			}
			case SmartActions.OverrideWeather:
			{
				var obj = GetBaseObject();

				if (obj != null)
				{
					obj.Map.SetZoneWeather(e.Action.overrideWeather.zoneId, (WeatherState)e.Action.overrideWeather.weatherId, e.Action.overrideWeather.intensity);

					Log.outDebug(LogFilter.ScriptsAi,
								$"SmartScript::ProcessAction: SMART_ACTION_OVERRIDE_WEATHER: {obj.GUID} sets zone weather (zoneId: {e.Action.overrideWeather.zoneId}, " +
								$"weatherId: {e.Action.overrideWeather.weatherId}, intensity: {e.Action.overrideWeather.intensity})");
				}

				break;
			}
			case SmartActions.SetHover:
			{
				foreach (var target in targets)
					if (IsUnit(target))
						target.AsUnit.SetHover(e.Action.setHover.enable != 0);

				break;
			}
			case SmartActions.SetHealthPct:
			{
				foreach (var target in targets)
				{
					var targetUnit = target.AsUnit;

					if (targetUnit != null)
						targetUnit.SetHealth(targetUnit.CountPctFromMaxHealth((int)e.Action.setHealthPct.percent));
				}

				break;
			}
			case SmartActions.CreateConversation:
			{
				var baseObject = GetBaseObject();

				foreach (var target in targets)
				{
					var playerTarget = target.AsPlayer;

					if (playerTarget != null)
					{
						var conversation = Conversation.CreateConversation(e.Action.conversation.id, playerTarget, playerTarget.Location, playerTarget.GUID, null);

						if (!conversation)
							Log.outWarn(LogFilter.ScriptsAi, $"SmartScript.ProcessAction: SMART_ACTION_CREATE_CONVERSATION: id {e.Action.conversation.id}, baseObject {baseObject?.GetName()}, target {playerTarget.GetName()} - failed to create");
					}
				}

				break;
			}
			case SmartActions.SetImmunePC:
			{
				foreach (var target in targets)
					if (IsUnit(target))
					{
						if (e.Action.setImmunePC.immunePC != 0)
							target.AsUnit.SetUnitFlag(UnitFlags.ImmuneToPc);
						else
							target.AsUnit.RemoveUnitFlag(UnitFlags.ImmuneToPc);
					}

				break;
			}
			case SmartActions.SetImmuneNPC:
			{
				foreach (var target in targets)
					if (IsUnit(target))
					{
						if (e.Action.setImmuneNPC.immuneNPC != 0)
							target.AsUnit.SetUnitFlag(UnitFlags.ImmuneToNpc);
						else
							target.AsUnit.RemoveUnitFlag(UnitFlags.ImmuneToNpc);
					}

				break;
			}
			case SmartActions.SetUninteractible:
			{
				foreach (var target in targets)
					if (IsUnit(target))
					{
						if (e.Action.setUninteractible.uninteractible != 0)
							target.AsUnit.SetUnitFlag(UnitFlags.Uninteractible);
						else
							target.AsUnit.RemoveUnitFlag(UnitFlags.Uninteractible);
					}

				break;
			}
			case SmartActions.ActivateGameobject:
			{
				foreach (var target in targets)
				{
					var targetGo = target.AsGameObject;

					if (targetGo != null)
						targetGo.ActivateObject((GameObjectActions)e.Action.activateGameObject.gameObjectAction, (int)e.Action.activateGameObject.param, GetBaseObject());
				}

				break;
			}
			case SmartActions.AddToStoredTargetList:
			{
				if (!targets.Empty())
				{
					AddToStoredTargetList(targets, e.Action.addToStoredTargets.id);
				}
				else
				{
					var baseObject = GetBaseObject();
					Log.outWarn(LogFilter.ScriptsAi, $"SmartScript::ProcessAction:: SMART_ACTION_ADD_TO_STORED_TARGET_LIST: var {e.Action.addToStoredTargets.id}, baseObject {(baseObject == null ? "" : baseObject.GetName())}, event {e.EventId} - tried to add no targets to stored target list");
				}

				break;
			}
			case SmartActions.BecomePersonalCloneForPlayer:
			{
				var baseObject = GetBaseObject();

				void doCreatePersonalClone(Position position, Player privateObjectOwner)
				{
					Creature summon = GetBaseObject().SummonPersonalClone(position, (TempSummonType)e.Action.becomePersonalClone.type, TimeSpan.FromMilliseconds(e.Action.becomePersonalClone.duration), 0, 0, privateObjectOwner);

					if (summon != null)
						if (IsSmart(summon))
							((SmartAI)summon.AI).SetTimedActionList(e, (uint)e.EntryOrGuid, privateObjectOwner, e.EventId + 1);
				}

				// if target is position then targets container was empty
				if (e.GetTargetType() != SmartTargets.Position)
				{
					foreach (var target in targets)
					{
						var playerTarget = target?.AsPlayer;

						if (playerTarget != null)
							doCreatePersonalClone(baseObject.Location, playerTarget);
					}
				}
				else
				{
					var invoker = GetLastInvoker()?.AsPlayer;

					if (invoker != null)
						doCreatePersonalClone(new Position(e.Target.x, e.Target.y, e.Target.z, e.Target.o), invoker);
				}

				// action list will continue on personal clones
				_timedActionList.RemoveAll(script => { return script.EventId > e.EventId; });

				break;
			}
			case SmartActions.TriggerGameEvent:
			{
				var sourceObject = GetBaseObjectOrUnitInvoker(unit);

				foreach (var target in targets)
					if (e.Action.triggerGameEvent.useSaiTargetAsGameEventSource != 0)
						GameEvents.Trigger(e.Action.triggerGameEvent.eventId, target, sourceObject);
					else
						GameEvents.Trigger(e.Action.triggerGameEvent.eventId, sourceObject, target);

				break;
			}
			case SmartActions.DoAction:
			{
				foreach (var target in targets)
				{
					var unitTarget = target?.AsUnit;

					if (unitTarget != null)
					{
						unitTarget.AI?.DoAction((int)e.Action.doAction.actionId);
					}
					else
					{
						var goTarget = target?.AsGameObject;

						if (goTarget != null)
							goTarget.AI?.DoAction((int)e.Action.doAction.actionId);
					}
				}

				break;
			}
			default:
				Log.outError(LogFilter.Sql, "SmartScript.ProcessAction: Entry {0} SourceType {1}, Event {2}, Unhandled Action type {3}", e.EntryOrGuid, e.GetScriptType(), e.EventId, e.GetActionType());

				break;
		}

		if (e.Link != 0 && e.Link != e.EventId)
		{
			var linked = Global.SmartAIMgr.FindLinkedEvent(_events, e.Link);

			if (linked != null)
				ProcessEvent(linked, unit, var0, var1, bvar, spell, gob, varString);
			else
				Log.outError(LogFilter.Sql, "SmartScript.ProcessAction: Entry {0} SourceType {1}, Event {2}, Link Event {3} not found or invalid, skipped.", e.EntryOrGuid, e.GetScriptType(), e.EventId, e.Link);
		}
	}

	void ProcessTimedAction(SmartScriptHolder e, uint min, uint max, Unit unit = null, uint var0 = 0, uint var1 = 0, bool bvar = false, SpellInfo spell = null, GameObject gob = null, string varString = "")
	{
		// We may want to execute action rarely and because of this if condition is not fulfilled the action will be rechecked in a long time
		if (Global.ConditionMgr.IsObjectMeetingSmartEventConditions(e.EntryOrGuid, e.EventId, e.SourceType, unit, GetBaseObject()))
		{
			RecalcTimer(e, min, max);
			ProcessAction(e, unit, var0, var1, bvar, spell, gob, varString);
		}
		else
		{
			RecalcTimer(e, Math.Min(min, 5000), Math.Min(min, 5000));
		}
	}

	SmartScriptHolder CreateSmartEvent(SmartEvents e, SmartEventFlags event_flags, uint event_param1, uint event_param2, uint event_param3, uint event_param4, uint event_param5,
										SmartActions action, uint action_param1, uint action_param2, uint action_param3, uint action_param4, uint action_param5, uint action_param6, uint action_param7,
										SmartTargets t, uint target_param1, uint target_param2, uint target_param3, uint target_param4, uint phaseMask)
	{
		SmartScriptHolder script = new();
		script.Event.type = e;
		script.Event.raw.param1 = event_param1;
		script.Event.raw.param2 = event_param2;
		script.Event.raw.param3 = event_param3;
		script.Event.raw.param4 = event_param4;
		script.Event.raw.param5 = event_param5;
		script.Event.event_phase_mask = phaseMask;
		script.Event.event_flags = event_flags;
		script.Event.event_chance = 100;

		script.Action.type = action;
		script.Action.raw.param1 = action_param1;
		script.Action.raw.param2 = action_param2;
		script.Action.raw.param3 = action_param3;
		script.Action.raw.param4 = action_param4;
		script.Action.raw.param5 = action_param5;
		script.Action.raw.param6 = action_param6;
		script.Action.raw.param7 = action_param7;

		script.Target.type = t;
		script.Target.raw.param1 = target_param1;
		script.Target.raw.param2 = target_param2;
		script.Target.raw.param3 = target_param3;
		script.Target.raw.param4 = target_param4;

		script.SourceType = SmartScriptType.Creature;
		InitTimer(script);

		return script;
	}

	List<WorldObject> GetTargets(SmartScriptHolder e, WorldObject invoker = null)
	{
		WorldObject scriptTrigger = null;

		if (invoker != null)
		{
			scriptTrigger = invoker;
		}
		else
		{
			var tempLastInvoker = GetLastInvoker();

			if (tempLastInvoker != null)
				scriptTrigger = tempLastInvoker;
		}

		var baseObject = GetBaseObject();

		List<WorldObject> targets = new();

		switch (e.GetTargetType())
		{
			case SmartTargets.Self:
				if (baseObject != null)
					targets.Add(baseObject);

				break;
			case SmartTargets.Victim:
				if (_me != null && _me.Victim != null)
					targets.Add(_me.Victim);

				break;
			case SmartTargets.HostileSecondAggro:
				if (_me != null)
				{
					if (e.Target.hostilRandom.powerType != 0)
					{
						var u = _me.AI.SelectTarget(SelectTargetMethod.MaxThreat, 1, new PowerUsersSelector(_me, (PowerType)(e.Target.hostilRandom.powerType - 1), (float)e.Target.hostilRandom.maxDist, e.Target.hostilRandom.playerOnly != 0));

						if (u != null)
							targets.Add(u);
					}
					else
					{
						var u = _me.AI.SelectTarget(SelectTargetMethod.MaxThreat, 1, (float)e.Target.hostilRandom.maxDist, e.Target.hostilRandom.playerOnly != 0);

						if (u != null)
							targets.Add(u);
					}
				}

				break;
			case SmartTargets.HostileLastAggro:
				if (_me != null)
				{
					if (e.Target.hostilRandom.powerType != 0)
					{
						var u = _me.AI.SelectTarget(SelectTargetMethod.MinThreat, 1, new PowerUsersSelector(_me, (PowerType)(e.Target.hostilRandom.powerType - 1), (float)e.Target.hostilRandom.maxDist, e.Target.hostilRandom.playerOnly != 0));

						if (u != null)
							targets.Add(u);
					}
					else
					{
						var u = _me.AI.SelectTarget(SelectTargetMethod.MinThreat, 1, (float)e.Target.hostilRandom.maxDist, e.Target.hostilRandom.playerOnly != 0);

						if (u != null)
							targets.Add(u);
					}
				}

				break;
			case SmartTargets.HostileRandom:
				if (_me != null)
				{
					if (e.Target.hostilRandom.powerType != 0)
					{
						var u = _me.AI.SelectTarget(SelectTargetMethod.Random, 1, new PowerUsersSelector(_me, (PowerType)(e.Target.hostilRandom.powerType - 1), (float)e.Target.hostilRandom.maxDist, e.Target.hostilRandom.playerOnly != 0));

						if (u != null)
							targets.Add(u);
					}
					else
					{
						var u = _me.AI.SelectTarget(SelectTargetMethod.Random, 1, (float)e.Target.hostilRandom.maxDist, e.Target.hostilRandom.playerOnly != 0);

						if (u != null)
							targets.Add(u);
					}
				}

				break;
			case SmartTargets.HostileRandomNotTop:
				if (_me != null)
				{
					if (e.Target.hostilRandom.powerType != 0)
					{
						var u = _me.AI.SelectTarget(SelectTargetMethod.Random, 1, new PowerUsersSelector(_me, (PowerType)(e.Target.hostilRandom.powerType - 1), (float)e.Target.hostilRandom.maxDist, e.Target.hostilRandom.playerOnly != 0));

						if (u != null)
							targets.Add(u);
					}
					else
					{
						var u = _me.AI.SelectTarget(SelectTargetMethod.Random, 1, (float)e.Target.hostilRandom.maxDist, e.Target.hostilRandom.playerOnly != 0);

						if (u != null)
							targets.Add(u);
					}
				}

				break;
			case SmartTargets.Farthest:
				if (_me)
				{
					var u = _me.AI.SelectTarget(SelectTargetMethod.MaxDistance, 0, new FarthestTargetSelector(_me, (float)e.Target.farthest.maxDist, e.Target.farthest.playerOnly != 0, e.Target.farthest.isInLos != 0));

					if (u != null)
						targets.Add(u);
				}

				break;
			case SmartTargets.ActionInvoker:
				if (scriptTrigger != null)
					targets.Add(scriptTrigger);

				break;
			case SmartTargets.ActionInvokerVehicle:
				if (scriptTrigger != null && scriptTrigger.AsUnit?.Vehicle1 != null && scriptTrigger.AsUnit.Vehicle1.GetBase() != null)
					targets.Add(scriptTrigger.AsUnit.Vehicle1.GetBase());

				break;
			case SmartTargets.InvokerParty:
				if (scriptTrigger != null)
				{
					var player = scriptTrigger.AsPlayer;

					if (player != null)
					{
						var group = player.Group;

						if (group)
							for (var groupRef = group.FirstMember; groupRef != null; groupRef = groupRef.Next())
							{
								var member = groupRef.Source;

								if (member)
									if (member.IsInMap(player))
										targets.Add(member);
							}
						// We still add the player to the list if there is no group. If we do
						// this even if there is a group (thus the else-check), it will add the
						// same player to the list twice. We don't want that to happen.
						else
							targets.Add(scriptTrigger);
					}
				}

				break;
			case SmartTargets.CreatureRange:
			{
				var refObj = baseObject;

				if (refObj == null)
					refObj = scriptTrigger;

				if (refObj == null)
				{
					Log.outError(LogFilter.Sql, $"SMART_TARGET_CREATURE_RANGE: {e} is missing base object or invoker.");

					break;
				}

				var units = GetWorldObjectsInDist(e.Target.unitRange.maxDist);

				foreach (var obj in units)
				{
					if (!IsCreature(obj))
						continue;

					if (_me != null && _me == obj)
						continue;

					if ((e.Target.unitRange.creature == 0 || obj.AsCreature.Entry == e.Target.unitRange.creature) && refObj.IsInRange(obj, e.Target.unitRange.minDist, e.Target.unitRange.maxDist))
						targets.Add(obj);
				}

				if (e.Target.unitRange.maxSize != 0)
					targets.RandomResize(e.Target.unitRange.maxSize);

				break;
			}
			case SmartTargets.CreatureDistance:
			{
				var units = GetWorldObjectsInDist(e.Target.unitDistance.dist);

				foreach (var obj in units)
				{
					if (!IsCreature(obj))
						continue;

					if (_me != null && _me == obj)
						continue;

					if (e.Target.unitDistance.creature == 0 || obj.AsCreature.Entry == e.Target.unitDistance.creature)
						targets.Add(obj);
				}

				if (e.Target.unitDistance.maxSize != 0)
					targets.RandomResize(e.Target.unitDistance.maxSize);

				break;
			}
			case SmartTargets.GameobjectDistance:
			{
				var units = GetWorldObjectsInDist(e.Target.goDistance.dist);

				foreach (var obj in units)
				{
					if (!IsGameObject(obj))
						continue;

					if (_go != null && _go == obj)
						continue;

					if (e.Target.goDistance.entry == 0 || obj.AsGameObject.Entry == e.Target.goDistance.entry)
						targets.Add(obj);
				}

				if (e.Target.goDistance.maxSize != 0)
					targets.RandomResize(e.Target.goDistance.maxSize);

				break;
			}
			case SmartTargets.GameobjectRange:
			{
				var refObj = baseObject;

				if (refObj == null)
					refObj = scriptTrigger;

				if (refObj == null)
				{
					Log.outError(LogFilter.Sql, $"SMART_TARGET_GAMEOBJECT_RANGE: {e} is missing base object or invoker.");

					break;
				}

				var units = GetWorldObjectsInDist(e.Target.goRange.maxDist);

				foreach (var obj in units)
				{
					if (!IsGameObject(obj))
						continue;

					if (_go != null && _go == obj)
						continue;

					if ((e.Target.goRange.entry == 0 || obj.AsGameObject.Entry == e.Target.goRange.entry) && refObj.IsInRange(obj, e.Target.goRange.minDist, e.Target.goRange.maxDist))
						targets.Add(obj);
				}

				if (e.Target.goRange.maxSize != 0)
					targets.RandomResize(e.Target.goRange.maxSize);

				break;
			}
			case SmartTargets.CreatureGuid:
			{
				if (scriptTrigger == null && baseObject == null)
				{
					Log.outError(LogFilter.Sql, $"SMART_TARGET_CREATURE_GUID {e} can not be used without invoker");

					break;
				}

				var target = FindCreatureNear(scriptTrigger != null ? scriptTrigger : baseObject, e.Target.unitGUID.dbGuid);

				if (target)
					if (target != null && (e.Target.unitGUID.entry == 0 || target.Entry == e.Target.unitGUID.entry))
						targets.Add(target);

				break;
			}
			case SmartTargets.GameobjectGuid:
			{
				if (scriptTrigger == null && baseObject == null)
				{
					Log.outError(LogFilter.Sql, $"SMART_TARGET_GAMEOBJECT_GUID {e} can not be used without invoker");

					break;
				}

				var target = FindGameObjectNear(scriptTrigger != null ? scriptTrigger : baseObject, e.Target.goGUID.dbGuid);

				if (target)
					if (target != null && (e.Target.goGUID.entry == 0 || target.Entry == e.Target.goGUID.entry))
						targets.Add(target);

				break;
			}
			case SmartTargets.PlayerRange:
			{
				var units = GetWorldObjectsInDist(e.Target.playerRange.maxDist);

				if (!units.Empty() && baseObject != null)
					foreach (var obj in units)
						if (IsPlayer(obj) && baseObject.IsInRange(obj, e.Target.playerRange.minDist, e.Target.playerRange.maxDist))
							targets.Add(obj);

				break;
			}
			case SmartTargets.PlayerDistance:
			{
				var units = GetWorldObjectsInDist(e.Target.playerDistance.dist);

				foreach (var obj in units)
					if (IsPlayer(obj))
						targets.Add(obj);

				break;
			}
			case SmartTargets.Stored:
			{
				var refObj = baseObject;

				if (refObj == null)
					refObj = scriptTrigger;

				if (refObj == null)
				{
					Log.outError(LogFilter.Sql, $"SMART_TARGET_STORED: {e} is missing base object or invoker.");

					break;
				}

				var stored = GetStoredTargetList(e.Target.stored.id, refObj);

				if (stored != null)
					targets.AddRange(stored);

				break;
			}
			case SmartTargets.ClosestCreature:
			{
				var refObj = baseObject;

				if (refObj == null)
					refObj = scriptTrigger;

				if (refObj == null)
				{
					Log.outError(LogFilter.Sql, $"SMART_TARGET_CLOSEST_CREATURE: {e} is missing base object or invoker.");

					break;
				}

				var target = refObj.FindNearestCreature(e.Target.unitClosest.entry, e.Target.unitClosest.dist != 0 ? e.Target.unitClosest.dist : 100, e.Target.unitClosest.dead == 0);

				if (target)
					targets.Add(target);

				break;
			}
			case SmartTargets.ClosestGameobject:
			{
				var refObj = baseObject;

				if (refObj == null)
					refObj = scriptTrigger;

				if (refObj == null)
				{
					Log.outError(LogFilter.Sql, $"SMART_TARGET_CLOSEST_GAMEOBJECT: {e} is missing base object or invoker.");

					break;
				}

				var target = refObj.FindNearestGameObject(e.Target.goClosest.entry, e.Target.goClosest.dist != 0 ? e.Target.goClosest.dist : 100);

				if (target)
					targets.Add(target);

				break;
			}
			case SmartTargets.ClosestPlayer:
			{
				var refObj = baseObject;

				if (refObj == null)
					refObj = scriptTrigger;

				if (refObj == null)
				{
					Log.outError(LogFilter.Sql, $"SMART_TARGET_CLOSEST_PLAYER: {e} is missing base object or invoker.");

					break;
				}

				var target = refObj.SelectNearestPlayer(e.Target.playerDistance.dist);

				if (target)
					targets.Add(target);

				break;
			}
			case SmartTargets.OwnerOrSummoner:
			{
				if (_me != null)
				{
					var charmerOrOwnerGuid = _me.CharmerOrOwnerGUID;

					if (charmerOrOwnerGuid.IsEmpty)
					{
						var tempSummon = _me.ToTempSummon();

						if (tempSummon)
						{
							var summoner = tempSummon.GetSummoner();

							if (summoner)
								charmerOrOwnerGuid = summoner.GUID;
						}
					}

					if (charmerOrOwnerGuid.IsEmpty)
						charmerOrOwnerGuid = _me.CreatorGUID;

					var owner = Global.ObjAccessor.GetWorldObject(_me, charmerOrOwnerGuid);

					if (owner != null)
						targets.Add(owner);
				}
				else if (_go != null)
				{
					var owner = Global.ObjAccessor.GetUnit(_go, _go.OwnerGUID);

					if (owner)
						targets.Add(owner);
				}

				// Get owner of owner
				if (e.Target.owner.useCharmerOrOwner != 0 && !targets.Empty())
				{
					var owner = targets.First();
					targets.Clear();

					var unitBase = Global.ObjAccessor.GetUnit(owner, owner.CharmerOrOwnerGUID);

					if (unitBase != null)
						targets.Add(unitBase);
				}

				break;
			}
			case SmartTargets.ThreatList:
			{
				if (_me != null && _me.CanHaveThreatList)
					foreach (var refe in _me.GetThreatManager().SortedThreatList)
						if (e.Target.threatList.maxDist == 0 || _me.IsWithinCombatRange(refe.Victim, e.Target.threatList.maxDist))
							targets.Add(refe.Victim);

				break;
			}
			case SmartTargets.ClosestEnemy:
			{
				if (_me != null)
				{
					var target = _me.SelectNearestTarget(e.Target.closestAttackable.maxDist);

					if (target != null)
						targets.Add(target);
				}

				break;
			}
			case SmartTargets.ClosestFriendly:
			{
				if (_me != null)
				{
					var target = DoFindClosestFriendlyInRange(e.Target.closestFriendly.maxDist);

					if (target != null)
						targets.Add(target);
				}

				break;
			}
			case SmartTargets.LootRecipients:
			{
				if (_me)
					foreach (var tapperGuid in _me.TapList)
					{
						var tapper = Global.ObjAccessor.GetPlayer(_me, tapperGuid);

						if (tapper != null)
							targets.Add(tapper);
					}

				break;
			}
			case SmartTargets.VehiclePassenger:
			{
				if (_me && _me.IsVehicle)
					foreach (var pair in _me.VehicleKit1.Seats)
						if (e.Target.vehicle.seatMask == 0 || (e.Target.vehicle.seatMask & (1 << pair.Key)) != 0)
						{
							var u = Global.ObjAccessor.GetUnit(_me, pair.Value.Passenger.Guid);

							if (u != null)
								targets.Add(u);
						}

				break;
			}
			case SmartTargets.ClosestUnspawnedGameobject:
			{
				var target = baseObject.FindNearestUnspawnedGameObject(e.Target.goClosest.entry, (float)(e.Target.goClosest.dist != 0 ? e.Target.goClosest.dist : 100));

				if (target != null)
					targets.Add(target);

				break;
			}
			case SmartTargets.Position:
			default:
				break;
		}

		return targets;
	}

	List<WorldObject> GetWorldObjectsInDist(float dist)
	{
		List<WorldObject> targets = new();
		var obj = GetBaseObject();

		if (obj == null)
			return targets;

		var u_check = new AllWorldObjectsInRange(obj, dist);
		var searcher = new WorldObjectListSearcher(obj, targets, u_check);
		Cell.VisitGrid(obj, searcher, dist);

		return targets;
	}

	void ProcessEvent(SmartScriptHolder e, Unit unit = null, uint var0 = 0, uint var1 = 0, bool bvar = false, SpellInfo spell = null, GameObject gob = null, string varString = "")
	{
		if (!e.Active && e.GetEventType() != SmartEvents.Link)
			return;

		if ((e.Event.event_phase_mask != 0 && !IsInPhase(e.Event.event_phase_mask)) || (e.Event.event_flags.HasAnyFlag(SmartEventFlags.NotRepeatable) && e.RunOnce))
			return;

		if (!e.Event.event_flags.HasAnyFlag(SmartEventFlags.WhileCharmed) && IsCharmedCreature(_me))
			return;

		switch (e.GetEventType())
		{
			case SmartEvents.Link: //special handling
				ProcessAction(e, unit, var0, var1, bvar, spell, gob);

				break;
			//called from Update tick
			case SmartEvents.Update:
				ProcessTimedAction(e, e.Event.minMaxRepeat.repeatMin, e.Event.minMaxRepeat.repeatMax);

				break;
			case SmartEvents.UpdateOoc:
				if (_me != null && _me.IsEngaged)
					return;

				ProcessTimedAction(e, e.Event.minMaxRepeat.repeatMin, e.Event.minMaxRepeat.repeatMax);

				break;
			case SmartEvents.UpdateIc:
				if (_me == null || !_me.IsEngaged)
					return;

				ProcessTimedAction(e, e.Event.minMaxRepeat.repeatMin, e.Event.minMaxRepeat.repeatMax);

				break;
			case SmartEvents.HealthPct:
			{
				if (_me == null || !_me.IsEngaged || _me.MaxHealth == 0)
					return;

				var perc = (uint)_me.HealthPct;

				if (perc > e.Event.minMaxRepeat.max || perc < e.Event.minMaxRepeat.min)
					return;

				ProcessTimedAction(e, e.Event.minMaxRepeat.repeatMin, e.Event.minMaxRepeat.repeatMax);

				break;
			}
			case SmartEvents.ManaPct:
			{
				if (_me == null || !_me.IsEngaged || _me.GetMaxPower(PowerType.Mana) == 0)
					return;

				var perc = (uint)_me.GetPowerPct(PowerType.Mana);

				if (perc > e.Event.minMaxRepeat.max || perc < e.Event.minMaxRepeat.min)
					return;

				ProcessTimedAction(e, e.Event.minMaxRepeat.repeatMin, e.Event.minMaxRepeat.repeatMax);

				break;
			}
			case SmartEvents.Range:
			{
				if (_me == null || !_me.IsEngaged || _me.Victim == null)
					return;

				if (_me.IsInRange(_me.Victim, e.Event.minMaxRepeat.min, e.Event.minMaxRepeat.max))
					ProcessTimedAction(e, e.Event.minMaxRepeat.repeatMin, e.Event.minMaxRepeat.repeatMax, _me.Victim);
				else // make it predictable
					RecalcTimer(e, 500, 500);

				break;
			}
			case SmartEvents.VictimCasting:
			{
				if (_me == null || !_me.IsEngaged)
					return;

				var victim = _me.Victim;

				if (victim == null || !victim.IsNonMeleeSpellCast(false, false, true))
					return;

				if (e.Event.targetCasting.spellId > 0)
				{
					var currSpell = victim.GetCurrentSpell(CurrentSpellTypes.Generic);

					if (currSpell != null)
						if (currSpell.SpellInfo.Id != e.Event.targetCasting.spellId)
							return;
				}

				ProcessTimedAction(e, e.Event.targetCasting.repeatMin, e.Event.targetCasting.repeatMax, _me.Victim);

				break;
			}
			case SmartEvents.FriendlyIsCc:
			{
				if (_me == null || !_me.IsEngaged)
					return;

				List<Creature> creatures = new();
				DoFindFriendlyCC(creatures, e.Event.friendlyCC.radius);

				if (creatures.Empty())
				{
					// if there are at least two same npcs, they will perform the same action immediately even if this is useless...
					RecalcTimer(e, 1000, 3000);

					return;
				}

				ProcessTimedAction(e, e.Event.friendlyCC.repeatMin, e.Event.friendlyCC.repeatMax, creatures.First());

				break;
			}
			case SmartEvents.FriendlyMissingBuff:
			{
				List<Creature> creatures = new();
				DoFindFriendlyMissingBuff(creatures, e.Event.missingBuff.radius, e.Event.missingBuff.spell);

				if (creatures.Empty())
					return;

				ProcessTimedAction(e, e.Event.missingBuff.repeatMin, e.Event.missingBuff.repeatMax, creatures.SelectRandom());

				break;
			}
			case SmartEvents.HasAura:
			{
				if (_me == null)
					return;

				var count = _me.GetAuraCount(e.Event.aura.spell);

				if ((e.Event.aura.count == 0 && count == 0) || (e.Event.aura.count != 0 && count >= e.Event.aura.count))
					ProcessTimedAction(e, e.Event.aura.repeatMin, e.Event.aura.repeatMax);

				break;
			}
			case SmartEvents.TargetBuffed:
			{
				if (_me == null || _me.Victim == null)
					return;

				var count = _me.Victim.GetAuraCount(e.Event.aura.spell);

				if (count < e.Event.aura.count)
					return;

				ProcessTimedAction(e, e.Event.aura.repeatMin, e.Event.aura.repeatMax, _me.Victim);

				break;
			}
			case SmartEvents.Charmed:
			{
				if (bvar == (e.Event.charm.onRemove != 1))
					ProcessAction(e, unit, var0, var1, bvar, spell, gob);

				break;
			}
			case SmartEvents.QuestAccepted:
			case SmartEvents.QuestCompletion:
			case SmartEvents.QuestFail:
			case SmartEvents.QuestRewarded:
			{
				ProcessAction(e, unit);

				break;
			}
			case SmartEvents.QuestObjCompletion:
			{
				if (var0 == (e.Event.questObjective.id))
					ProcessAction(e, unit);

				break;
			}
			//no params
			case SmartEvents.Aggro:
			case SmartEvents.Death:
			case SmartEvents.Evade:
			case SmartEvents.ReachedHome:
			case SmartEvents.CorpseRemoved:
			case SmartEvents.AiInit:
			case SmartEvents.TransportAddplayer:
			case SmartEvents.TransportRemovePlayer:
			case SmartEvents.JustSummoned:
			case SmartEvents.Reset:
			case SmartEvents.JustCreated:
			case SmartEvents.FollowCompleted:
			case SmartEvents.OnSpellclick:
			case SmartEvents.OnDespawn:
				ProcessAction(e, unit, var0, var1, bvar, spell, gob);

				break;
			case SmartEvents.GossipHello:
			{
				switch (e.Event.gossipHello.filter)
				{
					case 0:
						// no filter set, always execute action
						break;
					case 1:
						// OnGossipHello only filter set, skip action if OnReportUse
						if (var0 != 0)
							return;

						break;
					case 2:
						// OnReportUse only filter set, skip action if OnGossipHello
						if (var0 == 0)
							return;

						break;
					default:
						// Ignore any other value
						break;
				}

				ProcessAction(e, unit, var0, var1, bvar, spell, gob);

				break;
			}
			case SmartEvents.ReceiveEmote:
				if (e.Event.emote.emoteId == var0)
				{
					RecalcTimer(e, e.Event.emote.cooldownMin, e.Event.emote.cooldownMax);
					ProcessAction(e, unit);
				}

				break;
			case SmartEvents.Kill:
			{
				if (_me == null || unit == null)
					return;

				if (e.Event.kill.playerOnly != 0 && !unit.IsTypeId(TypeId.Player))
					return;

				if (e.Event.kill.creature != 0 && unit.Entry != e.Event.kill.creature)
					return;

				RecalcTimer(e, e.Event.kill.cooldownMin, e.Event.kill.cooldownMax);
				ProcessAction(e, unit);

				break;
			}
			case SmartEvents.SpellHitTarget:
			case SmartEvents.SpellHit:
			{
				if (spell == null)
					return;

				if ((e.Event.spellHit.spell == 0 || spell.Id == e.Event.spellHit.spell) &&
					(e.Event.spellHit.school == 0 || Convert.ToBoolean((uint)spell.SchoolMask & e.Event.spellHit.school)))
				{
					RecalcTimer(e, e.Event.spellHit.cooldownMin, e.Event.spellHit.cooldownMax);
					ProcessAction(e, unit, 0, 0, bvar, spell, gob);
				}

				break;
			}
			case SmartEvents.OnSpellCast:
			case SmartEvents.OnSpellFailed:
			case SmartEvents.OnSpellStart:
			{
				if (spell == null)
					return;

				if (spell.Id != e.Event.spellCast.spell)
					return;

				RecalcTimer(e, e.Event.spellCast.cooldownMin, e.Event.spellCast.cooldownMax);
				ProcessAction(e, null, 0, 0, bvar, spell);

				break;
			}
			case SmartEvents.OocLos:
			{
				if (_me == null || _me.IsEngaged)
					return;

				//can trigger if closer than fMaxAllowedRange
				float range = e.Event.los.maxDist;

				//if range is ok and we are actually in LOS
				if (_me.IsWithinDistInMap(unit, range) && _me.IsWithinLOSInMap(unit))
				{
					var hostilityMode = (LOSHostilityMode)e.Event.los.hostilityMode;

					//if friendly event&&who is not hostile OR hostile event&&who is hostile
					if ((hostilityMode == LOSHostilityMode.Any) || (hostilityMode == LOSHostilityMode.NotHostile && !_me.IsHostileTo(unit)) || (hostilityMode == LOSHostilityMode.Hostile && _me.IsHostileTo(unit)))
					{
						if (e.Event.los.playerOnly != 0 && !unit.IsTypeId(TypeId.Player))
							return;

						RecalcTimer(e, e.Event.los.cooldownMin, e.Event.los.cooldownMax);
						ProcessAction(e, unit);
					}
				}

				break;
			}
			case SmartEvents.IcLos:
			{
				if (_me == null || !_me.IsEngaged)
					return;

				//can trigger if closer than fMaxAllowedRange
				float range = e.Event.los.maxDist;

				//if range is ok and we are actually in LOS
				if (_me.IsWithinDistInMap(unit, range) && _me.IsWithinLOSInMap(unit))
				{
					var hostilityMode = (LOSHostilityMode)e.Event.los.hostilityMode;

					//if friendly event&&who is not hostile OR hostile event&&who is hostile
					if ((hostilityMode == LOSHostilityMode.Any) || (hostilityMode == LOSHostilityMode.NotHostile && !_me.IsHostileTo(unit)) || (hostilityMode == LOSHostilityMode.Hostile && _me.IsHostileTo(unit)))
					{
						if (e.Event.los.playerOnly != 0 && !unit.IsTypeId(TypeId.Player))
							return;

						RecalcTimer(e, e.Event.los.cooldownMin, e.Event.los.cooldownMax);
						ProcessAction(e, unit);
					}
				}

				break;
			}
			case SmartEvents.Respawn:
			{
				if (GetBaseObject() == null)
					return;

				if (e.Event.respawn.type == (uint)SmartRespawnCondition.Map && GetBaseObject().Location.MapId != e.Event.respawn.map)
					return;

				if (e.Event.respawn.type == (uint)SmartRespawnCondition.Area && GetBaseObject().Zone != e.Event.respawn.area)
					return;

				ProcessAction(e);

				break;
			}
			case SmartEvents.SummonedUnit:
			case SmartEvents.SummonedUnitDies:
			{
				if (!IsCreature(unit))
					return;

				if (e.Event.summoned.creature != 0 && unit.Entry != e.Event.summoned.creature)
					return;

				RecalcTimer(e, e.Event.summoned.cooldownMin, e.Event.summoned.cooldownMax);
				ProcessAction(e, unit);

				break;
			}
			case SmartEvents.ReceiveHeal:
			case SmartEvents.Damaged:
			case SmartEvents.DamagedTarget:
			{
				if (var0 > e.Event.minMaxRepeat.max || var0 < e.Event.minMaxRepeat.min)
					return;

				RecalcTimer(e, e.Event.minMaxRepeat.repeatMin, e.Event.minMaxRepeat.repeatMax);
				ProcessAction(e, unit);

				break;
			}
			case SmartEvents.Movementinform:
			{
				if ((e.Event.movementInform.type != 0 && var0 != e.Event.movementInform.type) || (e.Event.movementInform.id != 0xFFFFFFFF && var1 != e.Event.movementInform.id))
					return;

				ProcessAction(e, unit, var0, var1);

				break;
			}
			case SmartEvents.TransportRelocate:
			{
				if (e.Event.transportRelocate.pointID != 0 && var0 != e.Event.transportRelocate.pointID)
					return;

				ProcessAction(e, unit, var0);

				break;
			}
			case SmartEvents.WaypointReached:
			case SmartEvents.WaypointResumed:
			case SmartEvents.WaypointPaused:
			case SmartEvents.WaypointStopped:
			case SmartEvents.WaypointEnded:
			{
				if (_me == null || (e.Event.waypoint.pointID != 0 && var0 != e.Event.waypoint.pointID) || (e.Event.waypoint.pathID != 0 && var1 != e.Event.waypoint.pathID))
					return;

				ProcessAction(e, unit);

				break;
			}
			case SmartEvents.SummonDespawned:
			{
				if (e.Event.summoned.creature != 0 && e.Event.summoned.creature != var0)
					return;

				RecalcTimer(e, e.Event.summoned.cooldownMin, e.Event.summoned.cooldownMax);
				ProcessAction(e, unit, var0);

				break;
			}
			case SmartEvents.InstancePlayerEnter:
			{
				if (e.Event.instancePlayerEnter.team != 0 && var0 != e.Event.instancePlayerEnter.team)
					return;

				RecalcTimer(e, e.Event.instancePlayerEnter.cooldownMin, e.Event.instancePlayerEnter.cooldownMax);
				ProcessAction(e, unit, var0);

				break;
			}
			case SmartEvents.AcceptedQuest:
			case SmartEvents.RewardQuest:
			{
				if (e.Event.quest.questId != 0 && var0 != e.Event.quest.questId)
					return;

				RecalcTimer(e, e.Event.quest.cooldownMin, e.Event.quest.cooldownMax);
				ProcessAction(e, unit, var0);

				break;
			}
			case SmartEvents.TransportAddcreature:
			{
				if (e.Event.transportAddCreature.creature != 0 && var0 != e.Event.transportAddCreature.creature)
					return;

				ProcessAction(e, unit, var0);

				break;
			}
			case SmartEvents.AreatriggerOntrigger:
			{
				if (e.Event.areatrigger.id != 0 && var0 != e.Event.areatrigger.id)
					return;

				ProcessAction(e, unit, var0);

				break;
			}
			case SmartEvents.TextOver:
			{
				if (var0 != e.Event.textOver.textGroupID || (e.Event.textOver.creatureEntry != 0 && e.Event.textOver.creatureEntry != var1))
					return;

				ProcessAction(e, unit, var0);

				break;
			}
			case SmartEvents.DataSet:
			{
				if (e.Event.dataSet.id != var0 || e.Event.dataSet.value != var1)
					return;

				RecalcTimer(e, e.Event.dataSet.cooldownMin, e.Event.dataSet.cooldownMax);
				ProcessAction(e, unit, var0, var1);

				break;
			}
			case SmartEvents.PassengerRemoved:
			case SmartEvents.PassengerBoarded:
			{
				if (unit == null)
					return;

				RecalcTimer(e, e.Event.minMax.repeatMin, e.Event.minMax.repeatMax);
				ProcessAction(e, unit);

				break;
			}
			case SmartEvents.TimedEventTriggered:
			{
				if (e.Event.timedEvent.id == var0)
					ProcessAction(e, unit);

				break;
			}
			case SmartEvents.GossipSelect:
			{
				Log.outDebug(LogFilter.ScriptsAi, "SmartScript: Gossip Select:  menu {0} action {1}", var0, var1); //little help for scripters

				if (e.Event.gossip.sender != var0 || e.Event.gossip.action != var1)
					return;

				ProcessAction(e, unit, var0, var1);

				break;
			}
			case SmartEvents.GameEventStart:
			case SmartEvents.GameEventEnd:
			{
				if (e.Event.gameEvent.gameEventId != var0)
					return;

				ProcessAction(e, null, var0);

				break;
			}
			case SmartEvents.GoLootStateChanged:
			{
				if (e.Event.goLootStateChanged.lootState != var0)
					return;

				ProcessAction(e, unit, var0, var1);

				break;
			}
			case SmartEvents.GoEventInform:
			{
				if (e.Event.eventInform.eventId != var0)
					return;

				ProcessAction(e, null, var0);

				break;
			}
			case SmartEvents.ActionDone:
			{
				if (e.Event.doAction.eventId != var0)
					return;

				ProcessAction(e, unit, var0);

				break;
			}
			case SmartEvents.FriendlyHealthPCT:
			{
				if (_me == null || !_me.IsEngaged)
					return;

				Unit unitTarget = null;

				switch (e.GetTargetType())
				{
					case SmartTargets.CreatureRange:
					case SmartTargets.CreatureGuid:
					case SmartTargets.CreatureDistance:
					case SmartTargets.ClosestCreature:
					case SmartTargets.ClosestPlayer:
					case SmartTargets.PlayerRange:
					case SmartTargets.PlayerDistance:
					{
						var targets = GetTargets(e);

						foreach (var target in targets)
							if (IsUnit(target) && _me.IsFriendlyTo(target.AsUnit) && target.AsUnit.IsAlive && target.AsUnit.IsInCombat)
							{
								var healthPct = (uint)target.AsUnit.HealthPct;

								if (healthPct > e.Event.friendlyHealthPct.maxHpPct || healthPct < e.Event.friendlyHealthPct.minHpPct)
									continue;

								unitTarget = target.AsUnit;

								break;
							}
					}

						break;
					case SmartTargets.ActionInvoker:
						unitTarget = DoSelectLowestHpPercentFriendly((float)e.Event.friendlyHealthPct.radius, e.Event.friendlyHealthPct.minHpPct, e.Event.friendlyHealthPct.maxHpPct);

						break;
					default:
						return;
				}

				if (unitTarget == null)
					return;

				ProcessTimedAction(e, e.Event.friendlyHealthPct.repeatMin, e.Event.friendlyHealthPct.repeatMax, unitTarget);

				break;
			}
			case SmartEvents.DistanceCreature:
			{
				if (!_me)
					return;

				Creature creature = null;

				if (e.Event.distance.guid != 0)
				{
					creature = FindCreatureNear(_me, e.Event.distance.guid);

					if (!creature)
						return;

					if (!_me.IsInRange(creature, 0, e.Event.distance.dist))
						return;
				}
				else if (e.Event.distance.entry != 0)
				{
					var list = _me.GetCreatureListWithEntryInGrid(e.Event.distance.entry, e.Event.distance.dist);

					if (!list.Empty())
						creature = list.FirstOrDefault();
				}

				if (creature)
					ProcessTimedAction(e, e.Event.distance.repeat, e.Event.distance.repeat, creature);

				break;
			}
			case SmartEvents.DistanceGameobject:
			{
				if (!_me)
					return;

				GameObject gameobject = null;

				if (e.Event.distance.guid != 0)
				{
					gameobject = FindGameObjectNear(_me, e.Event.distance.guid);

					if (!gameobject)
						return;

					if (!_me.IsInRange(gameobject, 0, e.Event.distance.dist))
						return;
				}
				else if (e.Event.distance.entry != 0)
				{
					var list = _me.GetGameObjectListWithEntryInGrid(e.Event.distance.entry, e.Event.distance.dist);

					if (!list.Empty())
						gameobject = list.FirstOrDefault();
				}

				if (gameobject)
					ProcessTimedAction(e, e.Event.distance.repeat, e.Event.distance.repeat, null, 0, 0, false, null, gameobject);

				break;
			}
			case SmartEvents.CounterSet:
				if (e.Event.counter.id != var0 || GetCounterValue(e.Event.counter.id) != e.Event.counter.value)
					return;

				ProcessTimedAction(e, e.Event.counter.cooldownMin, e.Event.counter.cooldownMax);

				break;
			case SmartEvents.SceneStart:
			case SmartEvents.SceneCancel:
			case SmartEvents.SceneComplete:
			{
				ProcessAction(e, unit);

				break;
			}
			case SmartEvents.SceneTrigger:
			{
				if (e.Event.param_string != varString)
					return;

				ProcessAction(e, unit, var0, 0, false, null, null, varString);

				break;
			}
			default:
				Log.outError(LogFilter.Sql, "SmartScript.ProcessEvent: Unhandled Event type {0}", e.GetEventType());

				break;
		}
	}

	void InitTimer(SmartScriptHolder e)
	{
		switch (e.GetEventType())
		{
			//set only events which have initial timers
			case SmartEvents.Update:
			case SmartEvents.UpdateIc:
			case SmartEvents.UpdateOoc:
				RecalcTimer(e, e.Event.minMaxRepeat.min, e.Event.minMaxRepeat.max);

				break;
			case SmartEvents.DistanceCreature:
			case SmartEvents.DistanceGameobject:
				RecalcTimer(e, e.Event.distance.repeat, e.Event.distance.repeat);

				break;
			default:
				e.Active = true;

				break;
		}
	}

	void RecalcTimer(SmartScriptHolder e, uint min, uint max)
	{
		if (e.EntryOrGuid == 15294 && e.Timer != 0)
			Log.outError(LogFilter.Server, "Called RecalcTimer");

		// min/max was checked at loading!
		e.Timer = RandomHelper.URand(min, max);
		e.Active = e.Timer == 0;
	}

	void UpdateTimer(SmartScriptHolder e, uint diff)
	{
		if (e.GetEventType() == SmartEvents.Link)
			return;

		if (e.Event.event_phase_mask != 0 && !IsInPhase(e.Event.event_phase_mask))
			return;

		if (e.GetEventType() == SmartEvents.UpdateIc && (_me == null || !_me.IsEngaged))
			return;

		if (e.GetEventType() == SmartEvents.UpdateOoc && (_me != null && _me.IsEngaged)) //can be used with me=NULL (go script)
			return;

		if (e.Timer < diff)
		{
			// delay spell cast event if another spell is being casted
			if (e.GetActionType() == SmartActions.Cast)
				if (!Convert.ToBoolean(e.Action.cast.castFlags & (uint)SmartCastFlags.InterruptPrevious))
					if (_me != null && _me.HasUnitState(UnitState.Casting))
					{
						RaisePriority(e);

						return;
					}

			// Delay flee for assist event if stunned or rooted
			if (e.GetActionType() == SmartActions.FleeForAssist)
				if (_me && _me.HasUnitState(UnitState.Root | UnitState.LostControl))
				{
					e.Timer = 1;

					return;
				}

			e.Active = true; //activate events with cooldown

			switch (e.GetEventType()) //process ONLY timed events
			{
				case SmartEvents.Update:
				case SmartEvents.UpdateIc:
				case SmartEvents.UpdateOoc:
				case SmartEvents.HealthPct:
				case SmartEvents.ManaPct:
				case SmartEvents.Range:
				case SmartEvents.VictimCasting:
				case SmartEvents.FriendlyIsCc:
				case SmartEvents.FriendlyMissingBuff:
				case SmartEvents.HasAura:
				case SmartEvents.TargetBuffed:
				case SmartEvents.FriendlyHealthPCT:
				case SmartEvents.DistanceCreature:
				case SmartEvents.DistanceGameobject:
				{
					if (e.GetScriptType() == SmartScriptType.TimedActionlist)
					{
						Unit invoker = null;

						if (_me != null && !mTimedActionListInvoker.IsEmpty)
							invoker = Global.ObjAccessor.GetUnit(_me, mTimedActionListInvoker);

						ProcessEvent(e, invoker);
						e.EnableTimed = false; //disable event if it is in an ActionList and was processed once

						foreach (var holder in _timedActionList)
							//find the first event which is not the current one and enable it
							if (holder.EventId > e.EventId)
							{
								holder.EnableTimed = true;

								break;
							}
					}
					else
					{
						ProcessEvent(e);
					}

					break;
				}
			}

			if (e.Priority != SmartScriptHolder.DefaultPriority)
				// Reset priority to default one only if the event hasn't been rescheduled again to next loop
				if (e.Timer > 1)
				{
					// Re-sort events if this was moved to the top of the queue
					_eventSortingRequired = true;
					// Reset priority to default one
					e.Priority = SmartScriptHolder.DefaultPriority;
				}
		}
		else
		{
			e.Timer -= diff;

			if (e.EntryOrGuid == 15294 && _me.GUID.Counter == 55039 && e.Timer != 0)
				Log.outError(LogFilter.Server, "Called UpdateTimer: reduce timer: e.timer: {0}, diff: {1}  current time: {2}", e.Timer, diff, Time.MSTime);
		}
	}

	void InstallEvents()
	{
		if (!_installEvents.Empty())
		{
			lock (_events)
			{
				foreach (var holder in _installEvents)
					_events.Add(holder); //must be before UpdateTimers
			}

			_installEvents.Clear();
		}
	}

	void SortEvents(List<SmartScriptHolder> events)
	{
		events.Sort();
	}

	void RaisePriority(SmartScriptHolder e)
	{
		e.Timer = 1;

		// Change priority only if it's set to default, otherwise keep the current order of events
		if (e.Priority == SmartScriptHolder.DefaultPriority)
		{
			e.Priority = _currentPriority++;
			_eventSortingRequired = true;
		}
	}

	void RetryLater(SmartScriptHolder e, bool ignoreChanceRoll = false)
	{
		RaisePriority(e);

		// This allows to retry the action later without rolling again the chance roll (which might fail and end up not executing the action)
		if (ignoreChanceRoll)
			e.Event.event_flags |= SmartEventFlags.TempIgnoreChanceRoll;

		e.RunOnce = false;
	}

	void FillScript(List<SmartScriptHolder> e, WorldObject obj, AreaTriggerRecord at, SceneTemplate scene, Quest quest)
	{
		if (e.Empty())
		{
			if (obj != null)
				Log.outDebug(LogFilter.ScriptsAi, $"SmartScript: EventMap for Entry {obj.Entry} is empty but is using SmartScript.");

			if (at != null)
				Log.outDebug(LogFilter.ScriptsAi, $"SmartScript: EventMap for AreaTrigger {at.Id} is empty but is using SmartScript.");

			if (scene != null)
				Log.outDebug(LogFilter.ScriptsAi, $"SmartScript: EventMap for SceneId {scene.SceneId} is empty but is using SmartScript.");

			if (quest != null)
				Log.outDebug(LogFilter.ScriptsAi, $"SmartScript: EventMap for Quest {quest.Id} is empty but is using SmartScript.");

			return;
		}

		foreach (var holder in e)
		{
			if (holder.Event.event_flags.HasAnyFlag(SmartEventFlags.DifficultyAll)) //if has instance flag add only if in it
			{
				if (!(obj != null && obj.Map.IsDungeon))
					continue;

				// TODO: fix it for new maps and difficulties
				switch (obj.Map.DifficultyID)
				{
					case Difficulty.Normal:
					case Difficulty.Raid10N:
						if (holder.Event.event_flags.HasAnyFlag(SmartEventFlags.Difficulty0))
							lock (_events)
							{
								_events.Add(holder);
							}

						break;
					case Difficulty.Heroic:
					case Difficulty.Raid25N:
						if (holder.Event.event_flags.HasAnyFlag(SmartEventFlags.Difficulty1))
							lock (_events)
							{
								_events.Add(holder);
							}

						break;
					case Difficulty.Raid10HC:
						if (holder.Event.event_flags.HasAnyFlag(SmartEventFlags.Difficulty2))
							lock (_events)
							{
								_events.Add(holder);
							}

						break;
					case Difficulty.Raid25HC:
						if (holder.Event.event_flags.HasAnyFlag(SmartEventFlags.Difficulty3))
							lock (_events)
							{
								_events.Add(holder);
							}

						break;
					default:
						break;
				}
			}

			_allEventFlags |= holder.Event.event_flags;

			lock (_events)
			{
				_events.Add(holder); //NOTE: 'world(0)' events still get processed in ANY instance mode
			}
		}
	}

	void GetScript()
	{
		List<SmartScriptHolder> e;

		if (_me != null)
		{
			e = Global.SmartAIMgr.GetScript(-((int)_me.SpawnId), _scriptType);

			if (e.Empty())
				e = Global.SmartAIMgr.GetScript((int)_me.Entry, _scriptType);

			FillScript(e, _me, null, null, null);
		}
		else if (_go != null)
		{
			e = Global.SmartAIMgr.GetScript(-((int)_go.SpawnId), _scriptType);

			if (e.Empty())
				e = Global.SmartAIMgr.GetScript((int)_go.Entry, _scriptType);

			FillScript(e, _go, null, null, null);
		}
		else if (_trigger != null)
		{
			e = Global.SmartAIMgr.GetScript((int)_trigger.Id, _scriptType);
			FillScript(e, null, _trigger, null, null);
		}
		else if (_areaTrigger != null)
		{
			e = Global.SmartAIMgr.GetScript((int)_areaTrigger.Entry, _scriptType);
			FillScript(e, _areaTrigger, null, null, null);
		}
		else if (_sceneTemplate != null)
		{
			e = Global.SmartAIMgr.GetScript((int)_sceneTemplate.SceneId, _scriptType);
			FillScript(e, null, null, _sceneTemplate, null);
		}
		else if (_quest != null)
		{
			e = Global.SmartAIMgr.GetScript((int)_quest.Id, _scriptType);
			FillScript(e, null, null, null, _quest);
		}
	}

	Unit DoSelectLowestHpFriendly(float range, uint MinHPDiff)
	{
		if (!_me)
			return null;

		var u_check = new MostHPMissingInRange<Unit>(_me, range, MinHPDiff);
		var searcher = new UnitLastSearcher(_me, u_check, GridType.Grid);
		Cell.VisitGrid(_me, searcher, range);

		return searcher.GetTarget();
	}

	Unit DoSelectLowestHpPercentFriendly(float range, uint minHpPct, uint maxHpPct)
	{
		if (_me == null)
			return null;

		MostHPPercentMissingInRange u_check = new(_me, range, minHpPct, maxHpPct);
		UnitLastSearcher searcher = new(_me, u_check, GridType.Grid);
		Cell.VisitGrid(_me, searcher, range);

		return searcher.GetTarget();
	}

	void DoFindFriendlyCC(List<Creature> creatures, float range)
	{
		if (_me == null)
			return;

		var u_check = new FriendlyCCedInRange(_me, range);
		var searcher = new CreatureListSearcher(_me, creatures, u_check, GridType.Grid);
		Cell.VisitGrid(_me, searcher, range);
	}

	void DoFindFriendlyMissingBuff(List<Creature> creatures, float range, uint spellid)
	{
		if (_me == null)
			return;

		var u_check = new FriendlyMissingBuffInRange(_me, range, spellid);
		var searcher = new CreatureListSearcher(_me, creatures, u_check, GridType.Grid);
		Cell.VisitGrid(_me, searcher, range);
	}

	Unit DoFindClosestFriendlyInRange(float range)
	{
		if (!_me)
			return null;

		var u_check = new AnyFriendlyUnitInObjectRangeCheck(_me, _me, range);
		var searcher = new UnitLastSearcher(_me, u_check, GridType.All);
		Cell.VisitGrid(_me, searcher, range);

		return searcher.GetTarget();
	}

	Unit GetLastInvoker(Unit invoker = null)
	{
		// Look for invoker only on map of base object... Prevents multithreaded crashes
		var baseObject = GetBaseObject();

		if (baseObject != null)
			return Global.ObjAccessor.GetUnit(baseObject, LastInvoker);
		// used for area triggers invoker cast
		else if (invoker != null)
			return Global.ObjAccessor.GetUnit(invoker, LastInvoker);

		return null;
	}

	WorldObject GetBaseObject()
	{
		WorldObject obj = null;

		if (_me != null)
			obj = _me;
		else if (_go != null)
			obj = _go;
		else if (_areaTrigger != null)
			obj = _areaTrigger;
		else if (_player != null)
			obj = _player;

		return obj;
	}

	WorldObject GetBaseObjectOrUnitInvoker(Unit invoker)
	{
		return GetBaseObject() ?? invoker;
	}

	bool IsSmart(Creature creature, bool silent = false)
	{
		if (creature == null)
			return false;

		var smart = true;

		if (creature.GetAI<SmartAI>() == null)
			smart = false;

		if (!smart && !silent)
			Log.outError(LogFilter.Sql, "SmartScript: Action target Creature (GUID: {0} Entry: {1}) is not using SmartAI, action skipped to prevent crash.", creature != null ? creature.SpawnId : (_me != null ? _me.SpawnId : 0), creature != null ? creature.Entry : (_me != null ? _me.Entry : 0));

		return smart;
	}

	bool IsSmart(GameObject gameObject, bool silent = false)
	{
		if (gameObject == null)
			return false;

		var smart = true;

		if (gameObject.GetAI<SmartGameObjectAI>() == null)
			smart = false;

		if (!smart && !silent)
			Log.outError(LogFilter.Sql, "SmartScript: Action target GameObject (GUID: {0} Entry: {1}) is not using SmartGameObjectAI, action skipped to prevent crash.", gameObject != null ? gameObject.SpawnId : (_go != null ? _go.SpawnId : 0), gameObject != null ? gameObject.Entry : (_go != null ? _go.Entry : 0));

		return smart;
	}

	bool IsSmart(bool silent = false)
	{
		if (_me != null)
			return IsSmart(_me, silent);

		if (_go != null)
			return IsSmart(_go, silent);

		return false;
	}

	void StoreTargetList(List<WorldObject> targets, uint id)
	{
		// insert or replace
		_storedTargets.Remove(id);
		_storedTargets.Add(id, new ObjectGuidList(targets));
	}

	void AddToStoredTargetList(List<WorldObject> targets, uint id)
	{
		var inserted = _storedTargets.TryAdd(id, new ObjectGuidList(targets));

		if (!inserted)
			foreach (var obj in targets)
				_storedTargets[id].AddGuid(obj.GUID);
	}

	void StoreCounter(uint id, uint value, uint reset)
	{
		if (_counterList.ContainsKey(id))
		{
			if (reset == 0)
				_counterList[id] += value;
			else
				_counterList[id] = value;
		}
		else
		{
			_counterList.Add(id, value);
		}

		ProcessEventsFor(SmartEvents.CounterSet, null, id);
	}

	uint GetCounterValue(uint id)
	{
		if (_counterList.ContainsKey(id))
			return _counterList[id];

		return 0;
	}

	GameObject FindGameObjectNear(WorldObject searchObject, ulong guid)
	{
		var bounds = searchObject.Map.GameObjectBySpawnIdStore.LookupByKey(guid);

		if (bounds.Empty())
			return null;

		return bounds[0];
	}

	Creature FindCreatureNear(WorldObject searchObject, ulong guid)
	{
		var bounds = searchObject.Map.CreatureBySpawnIdStore.LookupByKey(guid);

		if (bounds.Empty())
			return null;

		var foundCreature = bounds.Find(creature => creature.IsAlive);

		return foundCreature ?? bounds[0];
	}

	void ResetBaseObject()
	{
		WorldObject lookupRoot = _me;

		if (!lookupRoot)
			lookupRoot = _go;

		if (lookupRoot)
		{
			if (!_meOrigGUID.IsEmpty)
			{
				var m = ObjectAccessor.GetCreature(lookupRoot, _meOrigGUID);

				if (m != null)
				{
					_me = m;
					_go = null;
					_areaTrigger = null;
				}
			}

			if (!_goOrigGUID.IsEmpty)
			{
				var o = ObjectAccessor.GetGameObject(lookupRoot, _goOrigGUID);

				if (o != null)
				{
					_me = null;
					_go = o;
					_areaTrigger = null;
				}
			}
		}

		_goOrigGUID.Clear();
		_meOrigGUID.Clear();
	}

	void IncPhase(uint p)
	{
		// protect phase from overflowing
		SetPhase(Math.Min((uint)SmartPhase.Phase12, _eventPhase + p));
	}

	void DecPhase(uint p)
	{
		if (p >= _eventPhase)
			SetPhase(0);
		else
			SetPhase(_eventPhase - p);
	}

	void SetPhase(uint p)
	{
		_eventPhase = p;
	}

	bool IsInPhase(uint p)
	{
		if (_eventPhase == 0)
			return false;

		return ((1 << (int)(_eventPhase - 1)) & p) != 0;
	}

	void RemoveStoredEvent(uint id)
	{
		if (!_storedEvents.Empty())
			foreach (var holder in _storedEvents)
				if (holder.EventId == id)
				{
					_storedEvents.Remove(holder);

					return;
				}
	}
}