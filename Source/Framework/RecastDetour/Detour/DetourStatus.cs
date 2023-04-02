// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using dtStatus = System.UInt32;

public static partial class Detour
{
    public const uint DT_ALREADY_OCCUPIED = 1 << 7;

    public const uint DT_BUFFER_TOO_SMALL = 1 << 4;

    // High level status.
    public const uint DT_FAILURE = 1u << 31; // Operation failed.

    public const uint DT_IN_PROGRESS = 1u << 29;
    public const uint DT_INVALID_PARAM = 1 << 3;
    public const uint DT_OUT_OF_MEMORY = 1 << 2;

    // Operation ran out of memory.
    // An input parameter was invalid.
    // Result buffer for the query was too small to store all results.
    public const uint DT_OUT_OF_NODES = 1 << 5;

    // Query ran out of nodes during search.
    public const uint DT_PARTIAL_RESULT = 1 << 6;

    // Detail information for status.
    public const uint DT_STATUS_DETAIL_MASK = 0x0ffffff;

    public const uint DT_SUCCESS = 1u << 30; // Operation succeed.

    // Operation still in progress.
    public const uint DT_WRONG_MAGIC = 1 << 0; // Input data is not recognized.

    public const uint DT_WRONG_VERSION = 1 << 1; // Input data is in wrong version.
    // Query did not reach the end location, returning best guess.
    // A tile has already been assigned to the given x,y coordinate

    // Returns true if specific detail is set.
    public static bool dtStatusDetail(uint status, uint detail)
    {
        return (status & detail) != 0;
    }

    // Returns true of status is failure.
    public static bool dtStatusFailed(uint status)
    {
        return (status & DT_FAILURE) != 0;
    }

    // Returns true of status is in progress.
    public static bool dtStatusInProgress(uint status)
    {
        return (status & DT_IN_PROGRESS) != 0;
    }

    // Returns true of status is success.
    public static bool dtStatusSucceed(uint status)
    {
        return (status & DT_SUCCESS) != 0;
    }
}