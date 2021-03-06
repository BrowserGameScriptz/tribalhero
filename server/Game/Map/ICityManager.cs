using System;
using System.Collections.Generic;
using Game.Data;
using Game.Data.Events;
using Game.Logic.Procedures;

namespace Game.Map
{
    public interface ICityManager
    {
        int Count { get; }

        uint GetNextCityId();

        bool TryGetCity(uint cityId, out ICity city);

        void AfterDbLoaded(Procedure procedure);

        IEnumerable<ICity> AllCities();

        void Remove(ICity city);

        void Add(ICity city);

        void DbLoaderAdd(ICity city);

        bool FindCityId(string name, out uint cityId);

        event EventHandler<NewCityEventArgs> CityAdded;

        event EventHandler<EventArgs> CityRemoved;
    }
}