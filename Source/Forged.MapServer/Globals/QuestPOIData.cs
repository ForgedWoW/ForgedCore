﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.IO;

namespace Forged.MapServer.Globals;

public class QuestPOIData
{
    public uint QuestID;
    public List<QuestPOIBlobData> Blobs;
    public ByteBuffer QueryDataBuffer;

    public QuestPOIData(uint questId)
    {
        QuestID = questId;
        Blobs = new List<QuestPOIBlobData>();
        QueryDataBuffer = new ByteBuffer();
    }

    public void InitializeQueryData()
    {
        Write(QueryDataBuffer);
    }

    public void Write(ByteBuffer data)
    {
        data.WriteUInt32(QuestID);
        data.WriteInt32(Blobs.Count);

        foreach (var questPOIBlobData in Blobs)
        {
            data.WriteInt32(questPOIBlobData.BlobIndex);
            data.WriteInt32(questPOIBlobData.ObjectiveIndex);
            data.WriteInt32(questPOIBlobData.QuestObjectiveID);
            data.WriteInt32(questPOIBlobData.QuestObjectID);
            data.WriteInt32(questPOIBlobData.MapID);
            data.WriteInt32(questPOIBlobData.UiMapID);
            data.WriteInt32(questPOIBlobData.Priority);
            data.WriteInt32(questPOIBlobData.Flags);
            data.WriteInt32(questPOIBlobData.WorldEffectID);
            data.WriteInt32(questPOIBlobData.PlayerConditionID);
            data.WriteInt32(questPOIBlobData.NavigationPlayerConditionID);
            data.WriteInt32(questPOIBlobData.SpawnTrackingID);
            data.WriteInt32(questPOIBlobData.Points.Count);

            foreach (var questPOIBlobPoint in questPOIBlobData.Points)
            {
                data.WriteInt16((short)questPOIBlobPoint.X);
                data.WriteInt16((short)questPOIBlobPoint.Y);
                data.WriteInt16((short)questPOIBlobPoint.Z);
            }

            data.WriteBit(questPOIBlobData.AlwaysAllowMergingBlobs);
            data.FlushBits();
        }
    }
}