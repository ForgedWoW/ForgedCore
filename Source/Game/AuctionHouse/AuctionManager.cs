﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Framework.Database;
using Framework.IO;
using Game.BattlePets;
using Game.DataStorage;
using Game.Entities;
using Game.Mails;
using Game.Networking.Packets;
using Game.Scripting.Interfaces.IAuctionHouse;

namespace Game
{
    public class AuctionManager : Singleton<AuctionManager>
    {
        private const int MIN_AUCTION_TIME = 12 * Time.Hour;

        private readonly Dictionary<ObjectGuid, Item> _itemsByGuid = new();

        private readonly Dictionary<ObjectGuid, PlayerPendingAuctions> _pendingAuctionsByPlayer = new();

        private readonly Dictionary<ObjectGuid, PlayerThrottleObject> _playerThrottleObjects = new();
        private DateTime _playerThrottleObjectsCleanupTime;

        private uint _replicateIdGenerator;
        private readonly AuctionHouseObject _allianceAuctions;
        private readonly AuctionHouseObject _goblinAuctions;

        private readonly AuctionHouseObject _hordeAuctions;
        private readonly AuctionHouseObject _neutralAuctions;

        private AuctionManager()
        {
            _hordeAuctions = new AuctionHouseObject(6);
            _allianceAuctions = new AuctionHouseObject(2);
            _neutralAuctions = new AuctionHouseObject(1);
            _goblinAuctions = new AuctionHouseObject(7);
            _replicateIdGenerator = 0;
            _playerThrottleObjectsCleanupTime = GameTime.Now() + TimeSpan.FromHours(1);
        }

        public AuctionHouseObject GetAuctionsMap(uint factionTemplateId)
        {
            if (WorldConfig.GetBoolValue(WorldCfg.AllowTwoSideInteractionAuction))
                return _neutralAuctions;

            // teams have linked auction houses
            FactionTemplateRecord uEntry = CliDB.FactionTemplateStorage.LookupByKey(factionTemplateId);

            if (uEntry == null)
                return _neutralAuctions;
            else if (uEntry.FactionGroup.HasAnyFlag((byte)FactionMasks.Alliance))
                return _allianceAuctions;
            else if (uEntry.FactionGroup.HasAnyFlag((byte)FactionMasks.Horde))
                return _hordeAuctions;
            else
                return _neutralAuctions;
        }

        public AuctionHouseObject GetAuctionsById(uint auctionHouseId)
        {
            switch (auctionHouseId)
            {
                case 1:
                    return _neutralAuctions;
                case 2:
                    return _allianceAuctions;
                case 6:
                    return _hordeAuctions;
                case 7:
                    return _goblinAuctions;
                default:
                    break;
            }

            return _neutralAuctions;
        }

        public Item GetAItem(ObjectGuid itemGuid)
        {
            return _itemsByGuid.LookupByKey(itemGuid);
        }

        public ulong GetCommodityAuctionDeposit(ItemTemplate item, TimeSpan time, uint quantity)
        {
            uint sellPrice = item.GetSellPrice();

            return (ulong)((Math.Ceiling(Math.Floor(Math.Max(0.15 * quantity * sellPrice, 100.0)) / MoneyConstants.Silver) * MoneyConstants.Silver) * (time.Minutes / (MIN_AUCTION_TIME / Time.Minute)));
        }

        public ulong GetItemAuctionDeposit(Player player, Item item, TimeSpan time)
        {
            uint sellPrice = item.GetSellPrice(player);

            return (ulong)((Math.Ceiling(Math.Floor(Math.Max(sellPrice * 0.15, 100.0)) / MoneyConstants.Silver) * MoneyConstants.Silver) * (time.Minutes / (MIN_AUCTION_TIME / Time.Minute)));
        }

        public string BuildItemAuctionMailSubject(AuctionMailType type, AuctionPosting auction)
        {
            return BuildAuctionMailSubject(auction.Items[0].GetEntry(),
                                           type,
                                           auction.Id,
                                           auction.GetTotalItemCount(),
                                           auction.Items[0].GetModifier(ItemModifier.BattlePetSpeciesId),
                                           auction.Items[0].GetContext(),
                                           auction.Items[0].GetBonusListIDs());
        }

        public string BuildCommodityAuctionMailSubject(AuctionMailType type, uint itemId, uint itemCount)
        {
            return BuildAuctionMailSubject(itemId, type, 0, itemCount, 0, ItemContext.None, null);
        }

        public string BuildAuctionMailSubject(uint itemId, AuctionMailType type, uint auctionId, uint itemCount, uint battlePetSpeciesId, ItemContext context, List<uint> bonusListIds)
        {
            string str = $"{itemId}:0:{(uint)type}:{auctionId}:{itemCount}:{battlePetSpeciesId}:0:0:0:0:{(uint)context}:{bonusListIds.Count}";

            foreach (var bonusListId in bonusListIds)
                str += ':' + bonusListId;

            return str;
        }

        public string BuildAuctionWonMailBody(ObjectGuid guid, ulong bid, ulong buyout)
        {
            return $"{guid}:{bid}:{buyout}:0";
        }

        public string BuildAuctionSoldMailBody(ObjectGuid guid, ulong bid, ulong buyout, uint deposit, ulong consignment)
        {
            return $"{guid}:{bid}:{buyout}:{deposit}:{consignment}:0";
        }

        public string BuildAuctionInvoiceMailBody(ObjectGuid guid, ulong bid, ulong buyout, uint deposit, ulong consignment, uint moneyDelay, uint eta)
        {
            return $"{guid}:{bid}:{buyout}:{deposit}:{consignment}:{moneyDelay}:{eta}:0";
        }

        public void LoadAuctions()
        {
            uint oldMSTime = Time.GetMSTime();

            // need to clear in case we are reloading
            _itemsByGuid.Clear();

            SQLResult result = DB.Characters.Query(DB.Characters.GetPreparedStatement(CharStatements.SEL_AUCTION_ITEMS));

            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 auctions. DB table `auctionhouse` is empty.");

                return;
            }

            // _data needs to be at first place for Item.LoadFromDB
            uint count = 0;
            MultiMap<uint, Item> itemsByAuction = new();
            MultiMap<uint, ObjectGuid> biddersByAuction = new();

            do
            {
                ulong itemGuid = result.Read<ulong>(0);
                uint itemEntry = result.Read<uint>(1);

                ItemTemplate proto = Global.ObjectMgr.GetItemTemplate(itemEntry);

                if (proto == null)
                {
                    Log.outError(LogFilter.Misc, $"AuctionHouseMgr.LoadAuctionItems: Unknown Item (GUID: {itemGuid} Item entry: #{itemEntry}) in auction, skipped.");

                    continue;
                }

                Item item = Item.NewItemOrBag(proto);

                if (!item.LoadFromDB(itemGuid, ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(51)), result.GetFields(), itemEntry))
                {
                    item.Dispose();

                    continue;
                }

                uint auctionId = result.Read<uint>(52);
                itemsByAuction.Add(auctionId, item);

                ++count;
            } while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, $"Loaded {count} auction items in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");

            oldMSTime = Time.GetMSTime();
            count = 0;

            result = DB.Characters.Query(DB.Characters.GetPreparedStatement(CharStatements.SEL_AUCTION_BIDDERS));

            if (!result.IsEmpty())
                do
                {
                    biddersByAuction.Add(result.Read<uint>(0), ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(1)));
                } while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, $"Loaded {count} auction bidders in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");

            oldMSTime = Time.GetMSTime();
            count = 0;

            result = DB.Characters.Query(DB.Characters.GetPreparedStatement(CharStatements.SEL_AUCTIONS));

            if (!result.IsEmpty())
            {
                SQLTransaction trans = new();

                do
                {
                    AuctionPosting auction = new();
                    auction.Id = result.Read<uint>(0);
                    uint auctionHouseId = result.Read<uint>(1);

                    AuctionHouseObject auctionHouse = GetAuctionsById(auctionHouseId);

                    if (auctionHouse == null)
                    {
                        Log.outError(LogFilter.Misc, $"Auction {auction.Id} has wrong auctionHouseId {auctionHouseId}");
                        PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_AUCTION);
                        stmt.AddValue(0, auction.Id);
                        trans.Append(stmt);

                        continue;
                    }

                    if (!itemsByAuction.ContainsKey(auction.Id))
                    {
                        Log.outError(LogFilter.Misc, $"Auction {auction.Id} has no items");
                        PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_AUCTION);
                        stmt.AddValue(0, auction.Id);
                        trans.Append(stmt);

                        continue;
                    }

                    auction.Items = itemsByAuction[auction.Id];
                    auction.Owner = ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(2));
                    auction.OwnerAccount = ObjectGuid.Create(HighGuid.WowAccount, Global.CharacterCacheStorage.GetCharacterAccountIdByGuid(auction.Owner));
                    ulong bidder = result.Read<ulong>(3);

                    if (bidder != 0)
                        auction.Bidder = ObjectGuid.Create(HighGuid.Player, bidder);

                    auction.MinBid = result.Read<ulong>(4);
                    auction.BuyoutOrUnitPrice = result.Read<ulong>(5);
                    auction.Deposit = result.Read<ulong>(6);
                    auction.BidAmount = result.Read<ulong>(7);
                    auction.StartTime = Time.UnixTimeToDateTime(result.Read<long>(8));
                    auction.EndTime = Time.UnixTimeToDateTime(result.Read<long>(9));
                    auction.ServerFlags = (AuctionPostingServerFlag)result.Read<byte>(10);

                    if (biddersByAuction.ContainsKey(auction.Id))
                        auction.BidderHistory = biddersByAuction[auction.Id];

                    auctionHouse.AddAuction(null, auction);

                    ++count;
                } while (result.NextRow());

                DB.Characters.CommitTransaction(trans);
            }

            Log.outInfo(LogFilter.ServerLoading, $"Loaded {count} auctions in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
        }

        public void AddAItem(Item item)
        {
            Cypher.Assert(item);
            Cypher.Assert(!_itemsByGuid.ContainsKey(item.GetGUID()));
            _itemsByGuid[item.GetGUID()] = item;
        }

        public bool RemoveAItem(ObjectGuid guid, bool deleteItem = false, SQLTransaction trans = null)
        {
            var item = _itemsByGuid.LookupByKey(guid);

            if (item == null)
                return false;

            if (deleteItem)
            {
                item.FSetState(ItemUpdateState.Removed);
                item.SaveToDB(trans);
            }

            _itemsByGuid.Remove(guid);

            return true;
        }

        public bool PendingAuctionAdd(Player player, uint auctionHouseId, uint auctionId, ulong deposit)
        {
            if (!_pendingAuctionsByPlayer.ContainsKey(player.GetGUID()))
                _pendingAuctionsByPlayer[player.GetGUID()] = new PlayerPendingAuctions();


            var pendingAuction = _pendingAuctionsByPlayer[player.GetGUID()];
            // Get deposit so far
            ulong totalDeposit = 0;

            foreach (PendingAuctionInfo thisAuction in pendingAuction.Auctions)
                totalDeposit += thisAuction.Deposit;

            // Add this deposit
            totalDeposit += deposit;

            if (!player.HasEnoughMoney(totalDeposit))
                return false;

            pendingAuction.Auctions.Add(new PendingAuctionInfo(auctionId, auctionHouseId, deposit));

            return true;
        }

        public int PendingAuctionCount(Player player)
        {
            var itr = _pendingAuctionsByPlayer.LookupByKey(player.GetGUID());

            if (itr != null)
                return itr.Auctions.Count;

            return 0;
        }

        public void PendingAuctionProcess(Player player)
        {
            var playerPendingAuctions = _pendingAuctionsByPlayer.LookupByKey(player.GetGUID());

            if (playerPendingAuctions == null)
                return;

            ulong totaldeposit = 0;
            var auctionIndex = 0;

            for (; auctionIndex < playerPendingAuctions.Auctions.Count; ++auctionIndex)
            {
                var pendingAuction = playerPendingAuctions.Auctions[auctionIndex];

                if (!player.HasEnoughMoney(totaldeposit + pendingAuction.Deposit))
                    break;

                totaldeposit += pendingAuction.Deposit;
            }

            // expire auctions we cannot afford
            if (auctionIndex < playerPendingAuctions.Auctions.Count)
            {
                SQLTransaction trans = new();

                do
                {
                    PendingAuctionInfo pendingAuction = playerPendingAuctions.Auctions[auctionIndex];
                    AuctionPosting auction = GetAuctionsById(pendingAuction.AuctionHouseId).GetAuction(pendingAuction.AuctionId);

                    if (auction != null)
                        auction.EndTime = GameTime.GetSystemTime();

                    PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_AUCTION_EXPIRATION);
                    stmt.AddValue(0, (uint)GameTime.GetGameTime());
                    stmt.AddValue(1, pendingAuction.AuctionId);
                    trans.Append(stmt);
                    ++auctionIndex;
                } while (auctionIndex < playerPendingAuctions.Auctions.Count);

                DB.Characters.CommitTransaction(trans);
            }

            _pendingAuctionsByPlayer.Remove(player.GetGUID());
            player.ModifyMoney(-(long)totaldeposit);
        }

        public void UpdatePendingAuctions()
        {
            foreach (var pair in _pendingAuctionsByPlayer)
            {
                ObjectGuid playerGUID = pair.Key;
                Player player = Global.ObjAccessor.FindConnectedPlayer(playerGUID);

                if (player != null)
                {
                    // Check if there were auctions since last update process if not
                    if (PendingAuctionCount(player) == pair.Value.LastAuctionsSize)
                        PendingAuctionProcess(player);
                    else
                        _pendingAuctionsByPlayer[playerGUID].LastAuctionsSize = PendingAuctionCount(player);
                }
                else
                {
                    // Expire any auctions that we couldn't get a deposit for
                    Log.outWarn(LogFilter.Auctionhouse, $"Player {playerGUID} was offline, unable to retrieve deposit!");

                    SQLTransaction trans = new();

                    foreach (PendingAuctionInfo pendingAuction in pair.Value.Auctions)
                    {
                        AuctionPosting auction = GetAuctionsById(pendingAuction.AuctionHouseId).GetAuction(pendingAuction.AuctionId);

                        if (auction != null)
                            auction.EndTime = GameTime.GetSystemTime();

                        PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_AUCTION_EXPIRATION);
                        stmt.AddValue(0, (uint)GameTime.GetGameTime());
                        stmt.AddValue(1, pendingAuction.AuctionId);
                        trans.Append(stmt);
                    }

                    DB.Characters.CommitTransaction(trans);
                    _pendingAuctionsByPlayer.Remove(playerGUID);
                }
            }
        }

        public void Update()
        {
            _hordeAuctions.Update();
            _allianceAuctions.Update();
            _neutralAuctions.Update();
            _goblinAuctions.Update();

            DateTime now = GameTime.Now();

            if (now >= _playerThrottleObjectsCleanupTime)
            {
                foreach (var pair in _playerThrottleObjects.ToList())
                    if (pair.Value.PeriodEnd < now)
                        _playerThrottleObjects.Remove(pair.Key);

                _playerThrottleObjectsCleanupTime = now + TimeSpan.FromHours(1);
            }
        }

        public uint GenerateReplicationId()
        {
            return ++_replicateIdGenerator;
        }

        public AuctionThrottleResult CheckThrottle(Player player, bool addonTainted, AuctionCommand command = AuctionCommand.SellItem)
        {
            DateTime now = GameTime.Now();

            if (!_playerThrottleObjects.ContainsKey(player.GetGUID()))
                _playerThrottleObjects[player.GetGUID()] = new PlayerThrottleObject();

            var throttleObject = _playerThrottleObjects[player.GetGUID()];

            if (now > throttleObject.PeriodEnd)
            {
                throttleObject.PeriodEnd = now + TimeSpan.FromMinutes(1);
                throttleObject.QueriesRemaining = 100;
            }

            if (throttleObject.QueriesRemaining == 0)
            {
                player.GetSession().SendAuctionCommandResult(0, command, AuctionResult.AuctionHouseBusy, throttleObject.PeriodEnd - now);

                return new AuctionThrottleResult(TimeSpan.Zero, true);
            }

            if ((--throttleObject.QueriesRemaining) == 0)
                return new AuctionThrottleResult(throttleObject.PeriodEnd - now, false);
            else
                return new AuctionThrottleResult(TimeSpan.FromMilliseconds(WorldConfig.GetIntValue(addonTainted ? WorldCfg.AuctionTaintedSearchDelay : WorldCfg.AuctionSearchDelay)), false);
        }

        public AuctionHouseRecord GetAuctionHouseEntry(uint factionTemplateId)
        {
            uint houseId = 0;

            return GetAuctionHouseEntry(factionTemplateId, ref houseId);
        }

        public AuctionHouseRecord GetAuctionHouseEntry(uint factionTemplateId, ref uint houseId)
        {
            uint houseid = 1; // Auction House

            if (!WorldConfig.GetBoolValue(WorldCfg.AllowTwoSideInteractionAuction))
                // FIXME: found way for proper auctionhouse selection by another way
                // AuctionHouse.dbc have faction field with _player_ factions associated with auction house races.
                // but no easy way convert creature faction to player race faction for specific city
                switch (factionTemplateId)
                {
                    case 120:
                        houseid = 7;

                        break; // booty bay, Blackwater Auction House
                    case 474:
                        houseid = 7;

                        break; // gadgetzan, Blackwater Auction House
                    case 855:
                        houseid = 7;

                        break; // everlook, Blackwater Auction House
                    default:   // default
                        {
                            FactionTemplateRecord u_entry = CliDB.FactionTemplateStorage.LookupByKey(factionTemplateId);

                            if (u_entry == null)
                                houseid = 1; // Auction House
                            else if ((u_entry.FactionGroup & (int)FactionMasks.Alliance) != 0)
                                houseid = 2; // Alliance Auction House
                            else if ((u_entry.FactionGroup & (int)FactionMasks.Horde) != 0)
                                houseid = 6; // Horde Auction House
                            else
                                houseid = 1; // Auction House

                            break;
                        }
                }

            houseId = houseid;

            return CliDB.AuctionHouseStorage.LookupByKey(houseid);
        }

        private class PendingAuctionInfo
        {
            public uint AuctionHouseId;
            public uint AuctionId;
            public ulong Deposit;

            public PendingAuctionInfo(uint auctionId, uint auctionHouseId, ulong deposit)
            {
                AuctionId = auctionId;
                AuctionHouseId = auctionHouseId;
                Deposit = deposit;
            }
        }

        private class PlayerPendingAuctions
        {
            public List<PendingAuctionInfo> Auctions = new();
            public int LastAuctionsSize;
        }

        private class PlayerThrottleObject
        {
            public DateTime PeriodEnd;
            public byte QueriesRemaining = 100;
        }
    }

    public class AuctionHouseObject
    {
        private readonly AuctionHouseRecord _auctionHouse;
        private readonly SortedDictionary<AuctionsBucketKey, AuctionsBucketData> _buckets = new(); // ordered for search by itemid only
        private readonly Dictionary<ObjectGuid, CommodityQuote> _commodityQuotes = new();

        private readonly SortedList<uint, AuctionPosting> _itemsByAuctionId = new(); // ordered for replicate
        private readonly MultiMap<ObjectGuid, uint> _playerBidderAuctions = new();

        private readonly MultiMap<ObjectGuid, uint> _playerOwnedAuctions = new();

        // Map of throttled players for GetAll, and throttle expiry Time
        // Stored here, rather than player object to maintain persistence after logout
        private readonly Dictionary<ObjectGuid, PlayerReplicateThrottleData> _replicateThrottleMap = new();

        public AuctionHouseObject(uint auctionHouseId)
        {
            _auctionHouse = CliDB.AuctionHouseStorage.LookupByKey(auctionHouseId);
        }

        public uint GetAuctionHouseId()
        {
            return _auctionHouse.Id;
        }

        public AuctionPosting GetAuction(uint auctionId)
        {
            return _itemsByAuctionId.LookupByKey(auctionId);
        }

        public void AddAuction(SQLTransaction trans, AuctionPosting auction)
        {
            AuctionsBucketKey key = AuctionsBucketKey.ForItem(auction.Items[0]);

            AuctionsBucketData bucket = _buckets.LookupByKey(key);

            if (bucket == null)
            {
                // we don't have any Item for this key yet, create new bucket
                bucket = new AuctionsBucketData();
                bucket.Key = key;

                ItemTemplate itemTemplate = auction.Items[0].GetTemplate();
                bucket.ItemClass = (byte)itemTemplate.GetClass();
                bucket.ItemSubClass = (byte)itemTemplate.GetSubClass();
                bucket.InventoryType = (byte)itemTemplate.GetInventoryType();
                bucket.RequiredLevel = (byte)auction.Items[0].GetRequiredLevel();

                switch (itemTemplate.GetClass())
                {
                    case ItemClass.Weapon:
                    case ItemClass.Armor:
                        bucket.SortLevel = (byte)key.ItemLevel;

                        break;
                    case ItemClass.Container:
                        bucket.SortLevel = (byte)itemTemplate.GetContainerSlots();

                        break;
                    case ItemClass.Gem:
                    case ItemClass.ItemEnhancement:
                        bucket.SortLevel = (byte)itemTemplate.GetBaseItemLevel();

                        break;
                    case ItemClass.Consumable:
                        bucket.SortLevel = Math.Max((byte)1, bucket.RequiredLevel);

                        break;
                    case ItemClass.Miscellaneous:
                    case ItemClass.BattlePets:
                        bucket.SortLevel = 1;

                        break;
                    case ItemClass.Recipe:
                        bucket.SortLevel = (byte)((ItemSubClassRecipe)itemTemplate.GetSubClass() != ItemSubClassRecipe.Book ? itemTemplate.GetRequiredSkillRank() : (uint)itemTemplate.GetBaseRequiredLevel());

                        break;
                    default:
                        break;
                }

                for (Locale locale = Locale.enUS; locale < Locale.Total; ++locale)
                {
                    if (locale == Locale.None)
                        continue;

                    bucket.FullName[(int)locale] = auction.Items[0].GetName(locale);
                }

                _buckets.Add(key, bucket);
            }

            // update cache fields
            ulong priceToDisplay = auction.BuyoutOrUnitPrice != 0 ? auction.BuyoutOrUnitPrice : auction.BidAmount;

            if (bucket.MinPrice == 0 ||
                priceToDisplay < bucket.MinPrice)
                bucket.MinPrice = priceToDisplay;

            var itemModifiedAppearance = auction.Items[0].GetItemModifiedAppearance();

            if (itemModifiedAppearance != null)
            {
                int index = 0;

                for (var i = 0; i < bucket.ItemModifiedAppearanceId.Length; ++i)
                    if (bucket.ItemModifiedAppearanceId[i].Id == itemModifiedAppearance.Id)
                    {
                        index = i;

                        break;
                    }

                bucket.ItemModifiedAppearanceId[index] = (itemModifiedAppearance.Id, bucket.ItemModifiedAppearanceId[index].Count + 1);
            }

            uint quality;

            if (auction.Items[0].GetModifier(ItemModifier.BattlePetSpeciesId) == 0)
            {
                quality = (byte)auction.Items[0].GetQuality();
            }
            else
            {
                quality = (auction.Items[0].GetModifier(ItemModifier.BattlePetBreedData) >> 24) & 0xFF;

                foreach (Item item in auction.Items)
                {
                    byte battlePetLevel = (byte)item.GetModifier(ItemModifier.BattlePetLevel);

                    if (bucket.MinBattlePetLevel == 0)
                        bucket.MinBattlePetLevel = battlePetLevel;
                    else if (bucket.MinBattlePetLevel > battlePetLevel)
                        bucket.MinBattlePetLevel = battlePetLevel;

                    bucket.MaxBattlePetLevel = Math.Max(bucket.MaxBattlePetLevel, battlePetLevel);
                    bucket.SortLevel = bucket.MaxBattlePetLevel;
                }
            }

            bucket.QualityMask |= (AuctionHouseFilterMask)(1 << ((int)quality + 4));
            ++bucket.QualityCounts[quality];

            if (trans != null)
            {
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_AUCTION);
                stmt.AddValue(0, auction.Id);
                stmt.AddValue(1, _auctionHouse.Id);
                stmt.AddValue(2, auction.Owner.GetCounter());
                stmt.AddValue(3, ObjectGuid.Empty.GetCounter());
                stmt.AddValue(4, auction.MinBid);
                stmt.AddValue(5, auction.BuyoutOrUnitPrice);
                stmt.AddValue(6, auction.Deposit);
                stmt.AddValue(7, auction.BidAmount);
                stmt.AddValue(8, Time.DateTimeToUnixTime(auction.StartTime));
                stmt.AddValue(9, Time.DateTimeToUnixTime(auction.EndTime));
                stmt.AddValue(10, (byte)auction.ServerFlags);
                trans.Append(stmt);

                foreach (Item item in auction.Items)
                {
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_AUCTION_ITEMS);
                    stmt.AddValue(0, auction.Id);
                    stmt.AddValue(1, item.GetGUID().GetCounter());
                    trans.Append(stmt);
                }
            }

            foreach (Item item in auction.Items)
                Global.AuctionHouseMgr.AddAItem(item);

            auction.Bucket = bucket;
            _playerOwnedAuctions.Add(auction.Owner, auction.Id);

            foreach (ObjectGuid bidder in auction.BidderHistory)
                _playerBidderAuctions.Add(bidder, auction.Id);

            _itemsByAuctionId[auction.Id] = auction;

            AuctionPosting.Sorter insertSorter = new(Locale.enUS,
                                                     new AuctionSortDef[]
                                                     {
                                                         new(AuctionHouseSortOrder.Price, false)
                                                     },
                                                     1);

            var auctionIndex = bucket.Auctions.BinarySearch(auction, insertSorter);

            if (auctionIndex < 0)
                auctionIndex = ~auctionIndex;

            bucket.Auctions.Insert(auctionIndex, auction);

            Global.ScriptMgr.ForEach<IAuctionHouseOnAuctionAdd>(p => p.OnAuctionAdd(this, auction));
        }

        public void RemoveAuction(SQLTransaction trans, AuctionPosting auction, AuctionPosting auctionPosting = null)
        {
            AuctionsBucketData bucket = auction.Bucket;

            bucket.Auctions.RemoveAll(auct => auct.Id == auction.Id);

            if (!bucket.Auctions.Empty())
            {
                // update cache fields
                ulong priceToDisplay = auction.BuyoutOrUnitPrice != 0 ? auction.BuyoutOrUnitPrice : auction.BidAmount;

                if (bucket.MinPrice == priceToDisplay)
                {
                    bucket.MinPrice = ulong.MaxValue;

                    foreach (AuctionPosting remainingAuction in bucket.Auctions)
                        bucket.MinPrice = Math.Min(bucket.MinPrice, remainingAuction.BuyoutOrUnitPrice != 0 ? remainingAuction.BuyoutOrUnitPrice : remainingAuction.BidAmount);
                }

                var itemModifiedAppearance = auction.Items[0].GetItemModifiedAppearance();

                if (itemModifiedAppearance != null)
                {
                    int index = -1;

                    for (var i = 0; i < bucket.ItemModifiedAppearanceId.Length; ++i)
                        if (bucket.ItemModifiedAppearanceId[i].Id == itemModifiedAppearance.Id)
                        {
                            index = i;

                            break;
                        }

                    if (index != -1)
                        if (--bucket.ItemModifiedAppearanceId[index].Count == 0)
                            bucket.ItemModifiedAppearanceId[index].Id = 0;
                }

                uint quality;

                if (auction.Items[0].GetModifier(ItemModifier.BattlePetSpeciesId) == 0)
                {
                    quality = (uint)auction.Items[0].GetQuality();
                }
                else
                {
                    quality = (auction.Items[0].GetModifier(ItemModifier.BattlePetBreedData) >> 24) & 0xFF;
                    bucket.MinBattlePetLevel = 0;
                    bucket.MaxBattlePetLevel = 0;

                    foreach (AuctionPosting remainingAuction in bucket.Auctions)
                    {
                        foreach (Item item in remainingAuction.Items)
                        {
                            byte battlePetLevel = (byte)item.GetModifier(ItemModifier.BattlePetLevel);

                            if (bucket.MinBattlePetLevel == 0)
                                bucket.MinBattlePetLevel = battlePetLevel;
                            else if (bucket.MinBattlePetLevel > battlePetLevel)
                                bucket.MinBattlePetLevel = battlePetLevel;

                            bucket.MaxBattlePetLevel = Math.Max(bucket.MaxBattlePetLevel, battlePetLevel);
                        }
                    }
                }

                if (--bucket.QualityCounts[quality] == 0)
                    bucket.QualityMask &= (AuctionHouseFilterMask)(~(1 << ((int)quality + 4)));
            }
            else
            {
                _buckets.Remove(bucket.Key);
            }

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_AUCTION);
            stmt.AddValue(0, auction.Id);
            trans.Append(stmt);

            foreach (Item item in auction.Items)
                Global.AuctionHouseMgr.RemoveAItem(item.GetGUID());

            Global.ScriptMgr.ForEach<IAuctionHouseOnAcutionRemove>(p => p.OnAuctionRemove(this, auction));

            _playerOwnedAuctions.Remove(auction.Owner, auction.Id);

            foreach (ObjectGuid bidder in auction.BidderHistory)
                _playerBidderAuctions.Remove(bidder, auction.Id);

            _itemsByAuctionId.Remove(auction.Id);
        }

        public void Update()
        {
            DateTime curTime = GameTime.GetSystemTime();
            DateTime curTimeSteady = GameTime.Now();
            ///- Handle expired auctions

            // Clear expired throttled players
            foreach (var key in _replicateThrottleMap.Keys.ToList())
                if (_replicateThrottleMap[key].NextAllowedReplication <= curTimeSteady)
                    _replicateThrottleMap.Remove(key);

            foreach (var key in _commodityQuotes.Keys.ToList())
                if (_commodityQuotes[key].ValidTo < curTimeSteady)
                    _commodityQuotes.Remove(key);

            if (_itemsByAuctionId.Empty())
                return;

            SQLTransaction trans = new();

            foreach (var auction in _itemsByAuctionId.Values.ToList())
            {
                ///- filter auctions expired on next update
                if (auction.EndTime > curTime.AddMinutes(1))
                    continue;

                ///- Either cancel the auction if there was no bidder
                if (auction.Bidder.IsEmpty())
                {
                    SendAuctionExpired(auction, trans);
                    Global.ScriptMgr.ForEach<IAuctionHouseOnAuctionExpire>(p => p.OnAuctionExpire(this, auction));
                }
                ///- Or perform the transaction
                else
                {
                    //we should send an "Item sold" message if the seller is online
                    //we send the Item to the winner
                    //we send the money to the seller
                    SendAuctionWon(auction, null, trans);
                    SendAuctionSold(auction, null, trans);
                    Global.ScriptMgr.ForEach<IAuctionHouseOnAuctionSuccessful>(p => p.OnAuctionSuccessful(this, auction));
                }

                ///- In any case clear the auction
                RemoveAuction(trans, auction);
            }

            // Run DB changes
            DB.Characters.CommitTransaction(trans);
        }

        public void BuildListBuckets(AuctionListBucketsResult listBucketsResult, Player player, string name, byte minLevel, byte maxLevel, AuctionHouseFilterMask filters, AuctionSearchClassFilters classFilters,
                                     byte[] knownPetBits, int knownPetBitsCount, byte maxKnownPetLevel, uint offset, AuctionSortDef[] sorts, int sortCount)
        {
            List<uint> knownAppearanceIds = new();
            BitArray knownPetSpecies = new(knownPetBits);

            // prepare uncollected filter for more efficient searches
            if (filters.HasFlag(AuctionHouseFilterMask.UncollectedOnly))
                knownAppearanceIds = player.GetSession().GetCollectionMgr().GetAppearanceIds();

            //todo fix me
            //if (knownPetSpecies.Length < CliDB.BattlePetSpeciesStorage.GetNumRows())
            //knownPetSpecies.resize(CliDB.BattlePetSpeciesStorage.GetNumRows());
            var sorter = new AuctionsBucketData.Sorter(player.GetSession().GetSessionDbcLocale(), sorts, sortCount);
            var builder = new AuctionsResultBuilder<AuctionsBucketData>(offset, sorter, AuctionHouseResultLimits.Browse);

            foreach (var bucket in _buckets)
            {
                AuctionsBucketData bucketData = bucket.Value;

                if (!name.IsEmpty())
                {
                    if (filters.HasFlag(AuctionHouseFilterMask.ExactMatch))
                    {
                        if (bucketData.FullName[(int)player.GetSession().GetSessionDbcLocale()] != name)
                            continue;
                    }
                    else
                    {
                        if (!bucketData.FullName[(int)player.GetSession().GetSessionDbcLocale()].Contains(name))
                            continue;
                    }
                }

                if (minLevel != 0 &&
                    bucketData.RequiredLevel < minLevel)
                    continue;

                if (maxLevel != 0 &&
                    bucketData.RequiredLevel > maxLevel)
                    continue;

                if (!filters.HasFlag(bucketData.QualityMask))
                    continue;

                if (classFilters != null)
                {
                    // if we dont want any class filters, Optional is not initialized
                    // if we dont want this class included, SubclassMask is set to FILTER_SKIP_CLASS
                    // if we want this class and did not specify and subclasses, its set to FILTER_SKIP_SUBCLASS
                    // otherwise full restrictions apply
                    if (classFilters.Classes[bucketData.ItemClass].SubclassMask == AuctionSearchClassFilters.FilterType.SkipClass)
                        continue;

                    if (classFilters.Classes[bucketData.ItemClass].SubclassMask != AuctionSearchClassFilters.FilterType.SkipSubclass)
                    {
                        if (!classFilters.Classes[bucketData.ItemClass].SubclassMask.HasAnyFlag((AuctionSearchClassFilters.FilterType)(1 << bucketData.ItemSubClass)))
                            continue;

                        if (!classFilters.Classes[bucketData.ItemClass].InvTypes[bucketData.ItemSubClass].HasAnyFlag(1u << bucketData.InventoryType))
                            continue;
                    }
                }

                if (filters.HasFlag(AuctionHouseFilterMask.UncollectedOnly))
                {
                    // appearances - by ItemAppearanceId, not ItemModifiedAppearanceId
                    if (bucketData.InventoryType != (byte)InventoryType.NonEquip &&
                        bucketData.InventoryType != (byte)InventoryType.Bag)
                    {
                        bool hasAll = true;

                        foreach (var bucketAppearance in bucketData.ItemModifiedAppearanceId)
                        {
                            var itemModifiedAppearance = CliDB.ItemModifiedAppearanceStorage.LookupByKey(bucketAppearance.Id);

                            if (itemModifiedAppearance != null)
                                if (!knownAppearanceIds.Contains((uint)itemModifiedAppearance.ItemAppearanceID))
                                {
                                    hasAll = false;

                                    break;
                                }
                        }

                        if (hasAll)
                            continue;
                    }
                    // caged pets
                    else if (bucket.Key.BattlePetSpeciesId != 0)
                    {
                        if (knownPetSpecies.Get(bucket.Key.BattlePetSpeciesId))
                            continue;
                    }
                    // toys
                    else if (Global.DB2Mgr.IsToyItem(bucket.Key.ItemId))
                    {
                        if (player.GetSession().GetCollectionMgr().HasToy(bucket.Key.ItemId))
                            continue;
                    }
                    // mounts
                    // recipes
                    // pet items
                    else if (bucketData.ItemClass == (int)ItemClass.Consumable ||
                             bucketData.ItemClass == (int)ItemClass.Recipe ||
                             bucketData.ItemClass == (int)ItemClass.Miscellaneous)
                    {
                        ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(bucket.Key.ItemId);

                        if (itemTemplate.Effects.Count >= 2 &&
                            (itemTemplate.Effects[0].SpellID == 483 || itemTemplate.Effects[0].SpellID == 55884))
                        {
                            if (player.HasSpell((uint)itemTemplate.Effects[1].SpellID))
                                continue;

                            var battlePetSpecies = BattlePetMgr.GetBattlePetSpeciesBySpell((uint)itemTemplate.Effects[1].SpellID);

                            if (battlePetSpecies != null)
                                if (knownPetSpecies.Get((int)battlePetSpecies.Id))
                                    continue;
                        }
                    }
                }

                if (filters.HasFlag(AuctionHouseFilterMask.UsableOnly))
                {
                    if (bucketData.RequiredLevel != 0 &&
                        player.GetLevel() < bucketData.RequiredLevel)
                        continue;

                    if (player.CanUseItem(Global.ObjectMgr.GetItemTemplate(bucket.Key.ItemId), true) != InventoryResult.Ok)
                        continue;

                    // cannot learn caged pets whose level exceeds highest level of currently owned pet
                    if (bucketData.MinBattlePetLevel != 0 &&
                        bucketData.MinBattlePetLevel > maxKnownPetLevel)
                        continue;
                }

                // TODO: this one needs to access loot history to know highest Item level for every inventory Type
                //if (filters.HasFlag(AuctionHouseFilterMask.UpgradesOnly))
                //{
                //}

                builder.AddItem(bucketData);
            }

            foreach (AuctionsBucketData resultBucket in builder.GetResultRange())
            {
                BucketInfo bucketInfo = new();
                resultBucket.BuildBucketInfo(bucketInfo, player);
                listBucketsResult.Buckets.Add(bucketInfo);
            }

            listBucketsResult.HasMoreResults = builder.HasMoreResults();
        }

        public void BuildListBuckets(AuctionListBucketsResult listBucketsResult, Player player, AuctionBucketKey[] keys, int keysCount, AuctionSortDef[] sorts, int sortCount)
        {
            List<AuctionsBucketData> buckets = new();

            for (int i = 0; i < keysCount; ++i)
            {
                var bucketData = _buckets.LookupByKey(new AuctionsBucketKey(keys[i]));

                if (bucketData != null)
                    buckets.Add(bucketData);
            }

            AuctionsBucketData.Sorter sorter = new(player.GetSession().GetSessionDbcLocale(), sorts, sortCount);
            buckets.Sort(sorter);

            foreach (AuctionsBucketData resultBucket in buckets)
            {
                BucketInfo bucketInfo = new();
                resultBucket.BuildBucketInfo(bucketInfo, player);
                listBucketsResult.Buckets.Add(bucketInfo);
            }

            listBucketsResult.HasMoreResults = false;
        }

        public void BuildListBiddedItems(AuctionListBiddedItemsResult listBidderItemsResult, Player player, uint offset, AuctionSortDef[] sorts, int sortCount)
        {
            // always full list
            List<AuctionPosting> auctions = new();

            foreach (var auctionId in _playerBidderAuctions.LookupByKey(player.GetGUID()))
            {
                AuctionPosting auction = _itemsByAuctionId.LookupByKey(auctionId);

                if (auction != null)
                    auctions.Add(auction);
            }

            AuctionPosting.Sorter sorter = new(player.GetSession().GetSessionDbcLocale(), sorts, sortCount);
            auctions.Sort(sorter);

            foreach (var resultAuction in auctions)
            {
                AuctionItem auctionItem = new();
                resultAuction.BuildAuctionItem(auctionItem, true, true, true, false);
                listBidderItemsResult.Items.Add(auctionItem);
            }

            listBidderItemsResult.HasMoreResults = false;
        }

        public void BuildListAuctionItems(AuctionListItemsResult listItemsResult, Player player, AuctionsBucketKey bucketKey, uint offset, AuctionSortDef[] sorts, int sortCount)
        {
            listItemsResult.TotalCount = 0;
            AuctionsBucketData bucket = _buckets.LookupByKey(bucketKey);

            if (bucket != null)
            {
                var sorter = new AuctionPosting.Sorter(player.GetSession().GetSessionDbcLocale(), sorts, sortCount);
                var builder = new AuctionsResultBuilder<AuctionPosting>(offset, sorter, AuctionHouseResultLimits.Items);

                foreach (var auction in bucket.Auctions)
                {
                    builder.AddItem(auction);

                    foreach (Item item in auction.Items)
                        listItemsResult.TotalCount += item.GetCount();
                }

                foreach (AuctionPosting resultAuction in builder.GetResultRange())
                {
                    AuctionItem auctionItem = new();
                    resultAuction.BuildAuctionItem(auctionItem, false, false, resultAuction.OwnerAccount != player.GetSession().GetAccountGUID(), resultAuction.Bidder.IsEmpty());
                    listItemsResult.Items.Add(auctionItem);
                }

                listItemsResult.HasMoreResults = builder.HasMoreResults();
            }
        }

        public void BuildListAuctionItems(AuctionListItemsResult listItemsResult, Player player, uint itemId, uint offset, AuctionSortDef[] sorts, int sortCount)
        {
            var sorter = new AuctionPosting.Sorter(player.GetSession().GetSessionDbcLocale(), sorts, sortCount);
            var builder = new AuctionsResultBuilder<AuctionPosting>(offset, sorter, AuctionHouseResultLimits.Items);

            listItemsResult.TotalCount = 0;
            var bucketData = _buckets.LookupByKey(new AuctionsBucketKey(itemId, 0, 0, 0));

            if (bucketData != null)
                foreach (AuctionPosting auction in bucketData.Auctions)
                {
                    builder.AddItem(auction);

                    foreach (Item item in auction.Items)
                        listItemsResult.TotalCount += item.GetCount();
                }

            foreach (AuctionPosting resultAuction in builder.GetResultRange())
            {
                AuctionItem auctionItem = new();

                resultAuction.BuildAuctionItem(auctionItem,
                                               false,
                                               true,
                                               resultAuction.OwnerAccount != player.GetSession().GetAccountGUID(),
                                               resultAuction.Bidder.IsEmpty());

                listItemsResult.Items.Add(auctionItem);
            }

            listItemsResult.HasMoreResults = builder.HasMoreResults();
        }

        public void BuildListOwnedItems(AuctionListOwnedItemsResult listOwnerItemsResult, Player player, uint offset, AuctionSortDef[] sorts, int sortCount)
        {
            // always full list
            List<AuctionPosting> auctions = new();

            foreach (var auctionId in _playerOwnedAuctions.LookupByKey(player.GetGUID()))
            {
                AuctionPosting auction = _itemsByAuctionId.LookupByKey(auctionId);

                if (auction != null)
                    auctions.Add(auction);
            }

            AuctionPosting.Sorter sorter = new(player.GetSession().GetSessionDbcLocale(), sorts, sortCount);
            auctions.Sort(sorter);

            foreach (var resultAuction in auctions)
            {
                AuctionItem auctionItem = new();
                resultAuction.BuildAuctionItem(auctionItem, true, true, false, false);
                listOwnerItemsResult.Items.Add(auctionItem);
            }

            listOwnerItemsResult.HasMoreResults = false;
        }

        public void BuildReplicate(AuctionReplicateResponse replicateResponse, Player player, uint global, uint cursor, uint tombstone, uint count)
        {
            DateTime curTime = GameTime.Now();

            var throttleData = _replicateThrottleMap.LookupByKey(player.GetGUID());

            if (throttleData == null)
            {
                throttleData = new PlayerReplicateThrottleData();
                throttleData.NextAllowedReplication = curTime + TimeSpan.FromSeconds(WorldConfig.GetIntValue(WorldCfg.AuctionReplicateDelay));
                throttleData.Global = Global.AuctionHouseMgr.GenerateReplicationId();
            }
            else
            {
                if (throttleData.Global != global ||
                    throttleData.Cursor != cursor ||
                    throttleData.Tombstone != tombstone)
                    return;

                if (!throttleData.IsReplicationInProgress() &&
                    throttleData.NextAllowedReplication > curTime)
                    return;
            }

            if (_itemsByAuctionId.Empty() ||
                count == 0)
                return;

            var keyIndex = _itemsByAuctionId.IndexOfKey(cursor);

            foreach (var pair in _itemsByAuctionId.Skip(keyIndex))
            {
                AuctionItem auctionItem = new();
                pair.Value.BuildAuctionItem(auctionItem, false, true, true, pair.Value.Bidder.IsEmpty());
                replicateResponse.Items.Add(auctionItem);

                if (--count == 0)
                    break;
            }

            replicateResponse.ChangeNumberGlobal = throttleData.Global;
            replicateResponse.ChangeNumberCursor = throttleData.Cursor = !replicateResponse.Items.Empty() ? replicateResponse.Items.Last().AuctionID : 0;
            replicateResponse.ChangeNumberTombstone = throttleData.Tombstone = count == 0 ? _itemsByAuctionId.First().Key : 0;
            _replicateThrottleMap[player.GetGUID()] = throttleData;
        }

        public ulong CalculateAuctionHouseCut(ulong bidAmount)
        {
            return (ulong)Math.Max((long)(MathFunctions.CalculatePct(bidAmount, _auctionHouse.ConsignmentRate) * WorldConfig.GetFloatValue(WorldCfg.RateAuctionCut)), 0);
        }

        public CommodityQuote CreateCommodityQuote(Player player, uint itemId, uint quantity)
        {
            ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(itemId);

            if (itemTemplate == null)
                return null;

            var bucketData = _buckets.LookupByKey(AuctionsBucketKey.ForCommodity(itemTemplate));

            if (bucketData == null)
                return null;

            ulong totalPrice = 0;
            uint remainingQuantity = quantity;

            foreach (AuctionPosting auction in bucketData.Auctions)
            {
                foreach (Item auctionItem in auction.Items)
                {
                    if (auctionItem.GetCount() >= remainingQuantity)
                    {
                        totalPrice += auction.BuyoutOrUnitPrice * remainingQuantity;
                        remainingQuantity = 0;

                        break;
                    }

                    totalPrice += auction.BuyoutOrUnitPrice * auctionItem.GetCount();
                    remainingQuantity -= auctionItem.GetCount();
                }
            }

            // not enough items on auction house
            if (remainingQuantity != 0)
                return null;

            if (!player.HasEnoughMoney(totalPrice))
                return null;

            CommodityQuote quote = _commodityQuotes[player.GetGUID()];
            quote.TotalPrice = totalPrice;
            quote.Quantity = quantity;
            quote.ValidTo = GameTime.Now() + TimeSpan.FromSeconds(30);

            return quote;
        }

        public void CancelCommodityQuote(ObjectGuid guid)
        {
            _commodityQuotes.Remove(guid);
        }

        public bool BuyCommodity(SQLTransaction trans, Player player, uint itemId, uint quantity, TimeSpan delayForNextAction)
        {
            ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(itemId);

            if (itemTemplate == null)
                return false;

            var bucketItr = _buckets.LookupByKey(AuctionsBucketKey.ForCommodity(itemTemplate));

            if (bucketItr == null)
            {
                player.GetSession().SendAuctionCommandResult(0, AuctionCommand.PlaceBid, AuctionResult.CommodityPurchaseFailed, delayForNextAction);

                return false;
            }

            var quote = _commodityQuotes.LookupByKey(player.GetGUID());

            if (quote == null)
            {
                player.GetSession().SendAuctionCommandResult(0, AuctionCommand.PlaceBid, AuctionResult.CommodityPurchaseFailed, delayForNextAction);

                return false;
            }

            ulong totalPrice = 0;
            uint remainingQuantity = quantity;
            List<AuctionPosting> auctions = new();

            for (var i = 0; i < bucketItr.Auctions.Count;)
            {
                AuctionPosting auction = bucketItr.Auctions[i++];
                auctions.Add(auction);

                foreach (Item auctionItem in auction.Items)
                {
                    if (auctionItem.GetCount() >= remainingQuantity)
                    {
                        totalPrice += auction.BuyoutOrUnitPrice * remainingQuantity;
                        remainingQuantity = 0;
                        i = bucketItr.Auctions.Count;

                        break;
                    }

                    totalPrice += auction.BuyoutOrUnitPrice * auctionItem.GetCount();
                    remainingQuantity -= auctionItem.GetCount();
                }
            }

            // not enough items on auction house
            if (remainingQuantity != 0)
            {
                player.GetSession().SendAuctionCommandResult(0, AuctionCommand.PlaceBid, AuctionResult.CommodityPurchaseFailed, delayForNextAction);

                return false;
            }

            // something was bought between creating quote and finalizing transaction
            // but we allow lower price if new items were posted at lower price
            if (totalPrice > quote.TotalPrice)
            {
                player.GetSession().SendAuctionCommandResult(0, AuctionCommand.PlaceBid, AuctionResult.CommodityPurchaseFailed, delayForNextAction);

                return false;
            }

            if (!player.HasEnoughMoney(totalPrice))
            {
                player.GetSession().SendAuctionCommandResult(0, AuctionCommand.PlaceBid, AuctionResult.CommodityPurchaseFailed, delayForNextAction);

                return false;
            }

            ObjectGuid? uniqueSeller = new();

            // prepare items
            List<MailedItemsBatch> items = new();
            items.Add(new MailedItemsBatch());

            remainingQuantity = quantity;
            List<int> removedItemsFromAuction = new();

            for (var i = 0; i < bucketItr.Auctions.Count;)
            {
                AuctionPosting auction = bucketItr.Auctions[i++];

                if (!uniqueSeller.HasValue)
                    uniqueSeller = auction.Owner;
                else if (uniqueSeller != auction.Owner)
                    uniqueSeller = ObjectGuid.Empty;

                uint boughtFromAuction = 0;
                int removedItems = 0;

                foreach (Item auctionItem in auction.Items)
                {
                    MailedItemsBatch itemsBatch = items.Last();

                    if (itemsBatch.IsFull())
                    {
                        items.Add(new MailedItemsBatch());
                        itemsBatch = items.Last();
                    }

                    if (auctionItem.GetCount() >= remainingQuantity)
                    {
                        Item clonedItem = auctionItem.CloneItem(remainingQuantity, player);

                        if (!clonedItem)
                        {
                            player.GetSession().SendAuctionCommandResult(0, AuctionCommand.PlaceBid, AuctionResult.CommodityPurchaseFailed, delayForNextAction);

                            return false;
                        }

                        auctionItem.SetCount(auctionItem.GetCount() - remainingQuantity);
                        auctionItem.FSetState(ItemUpdateState.Changed);
                        auctionItem.SaveToDB(trans);
                        itemsBatch.AddItem(clonedItem, auction.BuyoutOrUnitPrice);
                        boughtFromAuction += remainingQuantity;
                        remainingQuantity = 0;
                        i = bucketItr.Auctions.Count;

                        break;
                    }

                    itemsBatch.AddItem(auctionItem, auction.BuyoutOrUnitPrice);
                    boughtFromAuction += auctionItem.GetCount();
                    remainingQuantity -= auctionItem.GetCount();
                    ++removedItems;
                }

                removedItemsFromAuction.Add(removedItems);

                if (player.GetSession().HasPermission(RBACPermissions.LogGmTrade))
                {
                    uint bidderAccId = player.GetSession().GetAccountId();

                    if (!Global.CharacterCacheStorage.GetCharacterNameByGuid(auction.Owner, out string ownerName))
                        ownerName = Global.ObjectMgr.GetCypherString(CypherStrings.Unknown);

                    Log.outCommand(bidderAccId,
                                   $"GM {player.GetName()} (Account: {bidderAccId}) bought commodity in auction: {items[0].Items[0].GetName(Global.WorldMgr.GetDefaultDbcLocale())} (Entry: {items[0].Items[0].GetEntry()} " +
                                   $"Count: {boughtFromAuction}) and pay money: {auction.BuyoutOrUnitPrice * boughtFromAuction}. Original owner {ownerName} (Account: {Global.CharacterCacheStorage.GetCharacterAccountIdByGuid(auction.Owner)})");
                }

                ulong auctionHouseCut = CalculateAuctionHouseCut(auction.BuyoutOrUnitPrice * boughtFromAuction);
                ulong depositPart = Global.AuctionHouseMgr.GetCommodityAuctionDeposit(items[0].Items[0].GetTemplate(), (auction.EndTime - auction.StartTime), boughtFromAuction);
                ulong profit = auction.BuyoutOrUnitPrice * boughtFromAuction + depositPart - auctionHouseCut;

                Player owner = Global.ObjAccessor.FindConnectedPlayer(auction.Owner);

                if (owner != null)
                {
                    owner.UpdateCriteria(CriteriaType.MoneyEarnedFromAuctions, profit);
                    owner.UpdateCriteria(CriteriaType.HighestAuctionSale, profit);
                    owner.GetSession().SendAuctionClosedNotification(auction, (float)WorldConfig.GetIntValue(WorldCfg.MailDeliveryDelay), true);
                }

                new MailDraft(Global.AuctionHouseMgr.BuildCommodityAuctionMailSubject(AuctionMailType.Sold, itemId, boughtFromAuction),
                              Global.AuctionHouseMgr.BuildAuctionSoldMailBody(player.GetGUID(), auction.BuyoutOrUnitPrice * boughtFromAuction, boughtFromAuction, (uint)depositPart, auctionHouseCut))
                    .AddMoney(profit)
                    .SendMailTo(trans, new MailReceiver(Global.ObjAccessor.FindConnectedPlayer(auction.Owner), auction.Owner), new MailSender(this), MailCheckMask.Copied, WorldConfig.GetUIntValue(WorldCfg.MailDeliveryDelay));
            }

            player.ModifyMoney(-(long)totalPrice);
            player.SaveGoldToDB(trans);

            foreach (MailedItemsBatch batch in items)
            {
                MailDraft mail = new(Global.AuctionHouseMgr.BuildCommodityAuctionMailSubject(AuctionMailType.Won, itemId, batch.Quantity),
                                     Global.AuctionHouseMgr.BuildAuctionWonMailBody(uniqueSeller.Value, batch.TotalPrice, batch.Quantity));

                for (int i = 0; i < batch.ItemsCount; ++i)
                {
                    PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_AUCTION_ITEMS_BY_ITEM);
                    stmt.AddValue(0, batch.Items[i].GetGUID().GetCounter());
                    trans.Append(stmt);

                    batch.Items[i].SetOwnerGUID(player.GetGUID());
                    batch.Items[i].SaveToDB(trans);
                    mail.AddItem(batch.Items[i]);
                }

                mail.SendMailTo(trans, player, new MailSender(this), MailCheckMask.Copied);
            }

            AuctionWonNotification packet = new();
            packet.Info.Initialize(auctions[0], items[0].Items[0]);
            player.SendPacket(packet);

            for (int i = 0; i < auctions.Count; ++i)
                if (removedItemsFromAuction[i] == auctions[i].Items.Count)
                {
                    RemoveAuction(trans, auctions[i]); // bought all items
                }
                else if (removedItemsFromAuction[i] != 0)
                {
                    var lastRemovedItemIndex = removedItemsFromAuction[i];

                    for (var c = 0; c != removedItemsFromAuction[i]; ++c)
                        Global.AuctionHouseMgr.RemoveAItem(auctions[i].Items[c].GetGUID());

                    auctions[i].Items.RemoveRange(0, lastRemovedItemIndex);
                }

            return true;
        }

        // this function notified old bidder that his bid is no longer highest
        public void SendAuctionOutbid(AuctionPosting auction, ObjectGuid newBidder, ulong newBidAmount, SQLTransaction trans)
        {
            Player oldBidder = Global.ObjAccessor.FindConnectedPlayer(auction.Bidder);

            // old bidder exist
            if ((oldBidder || Global.CharacterCacheStorage.HasCharacterCacheEntry(auction.Bidder))) // && !sAuctionBotConfig.IsBotChar(auction.Bidder))
            {
                if (oldBidder)
                {
                    AuctionOutbidNotification packet = new();
                    packet.BidAmount = newBidAmount;
                    packet.MinIncrement = AuctionPosting.CalculateMinIncrement(newBidAmount);
                    packet.Info.AuctionID = auction.Id;
                    packet.Info.Bidder = newBidder;
                    packet.Info.Item = new ItemInstance(auction.Items[0]);
                    oldBidder.SendPacket(packet);
                }

                new MailDraft(Global.AuctionHouseMgr.BuildItemAuctionMailSubject(AuctionMailType.Outbid, auction), "")
                    .AddMoney(auction.BidAmount)
                    .SendMailTo(trans, new MailReceiver(oldBidder, auction.Bidder), new MailSender(this), MailCheckMask.Copied);
            }
        }

        public void SendAuctionWon(AuctionPosting auction, Player bidder, SQLTransaction trans)
        {
            uint bidderAccId;

            if (!bidder)
                bidder = Global.ObjAccessor.FindConnectedPlayer(auction.Bidder); // try lookup bidder when called from .Update

            // _data for gm.log
            string bidderName = "";
            bool logGmTrade = auction.ServerFlags.HasFlag(AuctionPostingServerFlag.GmLogBuyer);

            if (bidder)
            {
                bidderAccId = bidder.GetSession().GetAccountId();
                bidderName = bidder.GetName();
            }
            else
            {
                bidderAccId = Global.CharacterCacheStorage.GetCharacterAccountIdByGuid(auction.Bidder);

                if (logGmTrade && !Global.CharacterCacheStorage.GetCharacterNameByGuid(auction.Bidder, out bidderName))
                    bidderName = Global.ObjectMgr.GetCypherString(CypherStrings.Unknown);
            }

            if (logGmTrade)
            {
                if (!Global.CharacterCacheStorage.GetCharacterNameByGuid(auction.Owner, out string ownerName))
                    ownerName = Global.ObjectMgr.GetCypherString(CypherStrings.Unknown);

                uint ownerAccId = Global.CharacterCacheStorage.GetCharacterAccountIdByGuid(auction.Owner);

                Log.outCommand(bidderAccId,
                               $"GM {bidderName} (Account: {bidderAccId}) won Item in auction: {auction.Items[0].GetName(Global.WorldMgr.GetDefaultDbcLocale())} (Entry: {auction.Items[0].GetEntry()}" +
                               $" Count: {auction.GetTotalItemCount()}) and pay money: {auction.BidAmount}. Original owner {ownerName} (Account: {ownerAccId})");
            }

            // receiver exist
            if ((bidder != null || bidderAccId != 0)) // && !sAuctionBotConfig.IsBotChar(auction.Bidder))
            {
                MailDraft mail = new(Global.AuctionHouseMgr.BuildItemAuctionMailSubject(AuctionMailType.Won, auction),
                                     Global.AuctionHouseMgr.BuildAuctionWonMailBody(auction.Owner, auction.BidAmount, auction.BuyoutOrUnitPrice));

                // set owner to bidder (to prevent delete Item with sender char deleting)
                // owner in `_data` will set at mail receive and Item extracting
                foreach (Item item in auction.Items)
                {
                    PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_ITEM_OWNER);
                    stmt.AddValue(0, auction.Bidder.GetCounter());
                    stmt.AddValue(1, item.GetGUID().GetCounter());
                    trans.Append(stmt);

                    mail.AddItem(item);
                }

                if (bidder)
                {
                    AuctionWonNotification packet = new();
                    packet.Info.Initialize(auction, auction.Items[0]);
                    bidder.SendPacket(packet);

                    // FIXME: for offline player need also
                    bidder.UpdateCriteria(CriteriaType.AuctionsWon, 1);
                }

                mail.SendMailTo(trans, new MailReceiver(bidder, auction.Bidder), new MailSender(this), MailCheckMask.Copied);
            }
            else
            {
                // bidder doesn't exist, delete the Item
                foreach (Item item in auction.Items)
                    Global.AuctionHouseMgr.RemoveAItem(item.GetGUID(), true, trans);
            }
        }

        //call this method to send mail to auction owner, when auction is successful, it does not clear ram
        public void SendAuctionSold(AuctionPosting auction, Player owner, SQLTransaction trans)
        {
            if (!owner)
                owner = Global.ObjAccessor.FindConnectedPlayer(auction.Owner);

            // owner exist
            if ((owner || Global.CharacterCacheStorage.HasCharacterCacheEntry(auction.Owner))) // && !sAuctionBotConfig.IsBotChar(auction._owner))
            {
                ulong auctionHouseCut = CalculateAuctionHouseCut(auction.BidAmount);
                ulong profit = auction.BidAmount + auction.Deposit - auctionHouseCut;

                //FIXME: what do if owner offline
                if (owner)
                {
                    owner.UpdateCriteria(CriteriaType.MoneyEarnedFromAuctions, profit);
                    owner.UpdateCriteria(CriteriaType.HighestAuctionSale, auction.BidAmount);
                    //send auction owner notification, bidder must be current!
                    owner.GetSession().SendAuctionClosedNotification(auction, (float)WorldConfig.GetIntValue(WorldCfg.MailDeliveryDelay), true);
                }

                new MailDraft(Global.AuctionHouseMgr.BuildItemAuctionMailSubject(AuctionMailType.Sold, auction),
                              Global.AuctionHouseMgr.BuildAuctionSoldMailBody(auction.Bidder, auction.BidAmount, auction.BuyoutOrUnitPrice, (uint)auction.Deposit, auctionHouseCut))
                    .AddMoney(profit)
                    .SendMailTo(trans, new MailReceiver(owner, auction.Owner), new MailSender(this), MailCheckMask.Copied, WorldConfig.GetUIntValue(WorldCfg.MailDeliveryDelay));
            }
        }

        public void SendAuctionExpired(AuctionPosting auction, SQLTransaction trans)
        {
            Player owner = Global.ObjAccessor.FindConnectedPlayer(auction.Owner);

            // owner exist
            if ((owner || Global.CharacterCacheStorage.HasCharacterCacheEntry(auction.Owner))) // && !sAuctionBotConfig.IsBotChar(auction._owner))
            {
                if (owner)
                    owner.GetSession().SendAuctionClosedNotification(auction, 0.0f, false);

                int itemIndex = 0;

                while (itemIndex < auction.Items.Count)
                {
                    MailDraft mail = new(Global.AuctionHouseMgr.BuildItemAuctionMailSubject(AuctionMailType.Expired, auction), "");

                    for (int i = 0; i < SharedConst.MaxMailItems && itemIndex < auction.Items.Count; ++i, ++itemIndex)
                        mail.AddItem(auction.Items[itemIndex]);

                    mail.SendMailTo(trans, new MailReceiver(owner, auction.Owner), new MailSender(this), MailCheckMask.Copied, 0);
                }
            }
            else
            {
                // owner doesn't exist, delete the Item
                foreach (Item item in auction.Items)
                    Global.AuctionHouseMgr.RemoveAItem(item.GetGUID(), true, trans);
            }
        }

        public void SendAuctionRemoved(AuctionPosting auction, Player owner, SQLTransaction trans)
        {
            int itemIndex = 0;

            while (itemIndex < auction.Items.Count)
            {
                MailDraft draft = new(Global.AuctionHouseMgr.BuildItemAuctionMailSubject(AuctionMailType.Cancelled, auction), "");

                for (int i = 0; i < SharedConst.MaxMailItems && itemIndex < auction.Items.Count; ++i, ++itemIndex)
                    draft.AddItem(auction.Items[itemIndex]);

                draft.SendMailTo(trans, owner, new MailSender(this), MailCheckMask.Copied);
            }
        }

        //this function sends mail, when auction is cancelled to old bidder
        public void SendAuctionCancelledToBidder(AuctionPosting auction, SQLTransaction trans)
        {
            Player bidder = Global.ObjAccessor.FindConnectedPlayer(auction.Bidder);

            // bidder exist
            if ((bidder || Global.CharacterCacheStorage.HasCharacterCacheEntry(auction.Bidder))) // && !sAuctionBotConfig.IsBotChar(auction.Bidder))
                new MailDraft(Global.AuctionHouseMgr.BuildItemAuctionMailSubject(AuctionMailType.Removed, auction), "")
                    .AddMoney(auction.BidAmount)
                    .SendMailTo(trans, new MailReceiver(bidder, auction.Bidder), new MailSender(this), MailCheckMask.Copied);
        }

        public void SendAuctionInvoice(AuctionPosting auction, Player owner, SQLTransaction trans)
        {
            if (!owner)
                owner = Global.ObjAccessor.FindConnectedPlayer(auction.Owner);

            // owner exist (online or offline)
            if ((owner || Global.CharacterCacheStorage.HasCharacterCacheEntry(auction.Owner))) // && !sAuctionBotConfig.IsBotChar(auction._owner))
            {
                ByteBuffer tempBuffer = new();
                tempBuffer.WritePackedTime(GameTime.GetGameTime() + WorldConfig.GetIntValue(WorldCfg.MailDeliveryDelay));
                uint eta = tempBuffer.ReadUInt32();

                new MailDraft(Global.AuctionHouseMgr.BuildItemAuctionMailSubject(AuctionMailType.Invoice, auction),
                              Global.AuctionHouseMgr.BuildAuctionInvoiceMailBody(auction.Bidder,
                                                                                 auction.BidAmount,
                                                                                 auction.BuyoutOrUnitPrice,
                                                                                 (uint)auction.Deposit,
                                                                                 CalculateAuctionHouseCut(auction.BidAmount),
                                                                                 WorldConfig.GetUIntValue(WorldCfg.MailDeliveryDelay),
                                                                                 eta))
                    .SendMailTo(trans, new MailReceiver(owner, auction.Owner), new MailSender(this), MailCheckMask.Copied);
            }
        }

        private class PlayerReplicateThrottleData
        {
            public uint Cursor;
            public uint Global;
            public DateTime NextAllowedReplication = DateTime.MinValue;
            public uint Tombstone;

            public bool IsReplicationInProgress()
            {
                return Cursor != Tombstone && Global != 0;
            }
        }

        private class MailedItemsBatch
        {
            public Item[] Items = new Item[SharedConst.MaxMailItems];

            public int ItemsCount;
            public uint Quantity;
            public ulong TotalPrice;

            public bool IsFull()
            {
                return ItemsCount >= Items.Length;
            }

            public void AddItem(Item item, ulong unitPrice)
            {
                Items[ItemsCount++] = item;
                Quantity += item.GetCount();
                TotalPrice += unitPrice * item.GetCount();
            }
        }
    }
}