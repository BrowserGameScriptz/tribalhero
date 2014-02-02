﻿#region

using System;
using Game.Data;
using Game.Logic.Procedures;
using Game.Setup;

#endregion

namespace Game.Logic.Actions
{
    public class CityResourceCapUpdatePassiveAction : PassiveAction, IScriptable
    {
        private readonly Procedure procedure;

        private IStructure obj;

        public CityResourceCapUpdatePassiveAction(Procedure procedure)
        {
            this.procedure = procedure;
        }

        public CityResourceCapUpdatePassiveAction(uint id, bool isVisible, Procedure procedure)
                : base(id, isVisible)
        {
            this.procedure = procedure;
        }

        public override ActionType Type
        {
            get
            {
                return ActionType.CityResourceCapUpdatePassive;
            }
        }

        public override string Properties
        {
            get
            {
                return string.Empty;
            }
        }

        #region IScriptable Members

        public void ScriptInit(IGameObject gameObject, string[] parms)
        {
            if ((obj = gameObject as IStructure) == null)
            {
                throw new Exception();
            }
            Execute();
        }

        #endregion

        public override Error Validate(string[] parms)
        {
            return Error.ActionNotFound;
        }

        public override Error Execute()
        {
            obj.City.BeginUpdate();
            procedure.SetResourceCap(obj.City);
            obj.City.EndUpdate();
            return Error.Ok;
        }

        public override void UserCancelled()
        {
        }

        public override void WorkerRemoved(bool wasKilled)
        {
        }
    }
}