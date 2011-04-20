namespace Game.Comm
{
    public enum Command : ushort
    {
        Invalid = 1,

        CmdLine = 7,

        #region Internal Messages

        OnConnect = 10001,
        OnDisconnect = 10002,

        #endregion

        #region Testing

        PlaceObjects = 1001,
        MoveObject = 1002,
        FooRegionMoveRight = 1003,

        #endregion

        #region Account Information

        Login = 10,
        QueryXml = 11,
        PlayerUsernameGet = 12,
        CityUsernameGet = 13,
        PlayerNameFromCityName = 14,

        #endregion

        #region Action

        ActionCancel = 51,
        ActionCompleted = 52,
        ActionStarted = 53,
        ActionRescheduled = 54,

        #endregion

        #region Notification

        NotificationAdd = 61,
        NotificationRemove = 62,
        NotificationUpdate = 63,
        NotificationLocate = 64,

        #endregion

        #region Reference

        ReferenceAdd = 71,
        ReferenceRemove = 72,

        #endregion

        #region Region

        RegionRoadDestroy = 102,
        RegionRoadBuild = 103,
        RegionSetTile = 104,
        RegionGet = 105,
        CityRegionGet = 106,

        #endregion

        #region Road

        RoadAdd = 150,
        RoadRemove = 151,

        #endregion

        #region Object

        ObjectAdd = 201,
        ObjectUpdate = 202,
        ObjectRemove = 203,
        ObjectMove = 204,

        #endregion

        #region Structure

        StructureInfo = 300,
        StructureBuild = 301,
        StructureUpgrade = 302,
        StructureChange = 303,
        StructureLaborMove = 304,
        StructureDowngrade = 305,
        StructureSelfDestroy = 306,
        TechAdded = 311,
        TechUpgrade = 312,
        TechRemoved = 313,
        TechUpgraded = 314,
        TechCleared = 315,

        #endregion

        #region Forest

        ForestInfo = 350,
        ForestCampCreate = 351,
        ForestCampRemove = 352,

        #endregion

        #region City

        CityObjectAdd = 451,
        CityObjectUpdate = 452,
        CityObjectRemove = 453,

        CityResourceSend = 460,
        CityResourcesUpdate = 462,
        CityUnitList = 463,
        CityLocateByName = 464,
        CityRadiusUpdate = 465,
        CityLocate = 466,
        CityAttackDefensePointUpdate = 467,
        CityHideNewUnitsUpdate = 468,

        CityBattleStarted = 490,
        CityBattleEnded = 491,

        CityCreate = 498,
        CityCreateInitial = 499,

        #endregion

        #region Troop

        UnitTrain = 501,
        UnitUpgrade = 502,
        UnitTemplateUpgraded = 503,

        TroopInfo = 600,
        TroopAttack = 601,
        TroopDefend = 602,
        TroopRetreat = 603,
        TroopAdded = 611,
        TroopUpdated = 612,
        TroopRemoved = 613,

        TroopLocalSet = 621,

        #endregion

        #region Market

        MarketBuy = 901,
        MarketSell = 902,
        MarketPrices = 903,

        #endregion

        #region Battle

        BattleSubscribe = 700,
        BattleUnsubscribe = 701,
        BattleAttack = 702,
        BattleReinforceAttacker = 703,
        BattleReinforceDefender = 704,
        BattleEnded = 705,
        BattleSkipped = 706,
        BattleNewRound = 707,

        #endregion

        #region Misc
        ResourceGather = 801,
        #endregion

        #region Tribe
        TribeInfo = 901,
        TribeCreate = 902,
        TribeDelete = 903,
        TribeUpdate = 904,
        TribeUpgrade = 905,
        TribesmanAdd = 911,
        TribesmanRemove = 912,
        TribesmanUpdate = 913,
        TribeAssignementList = 921,
        TribeAssignementCreate = 922,
        TribeAssignementJoin = 923,
        TribeIncomingList = 931,

        #endregion
    }
}