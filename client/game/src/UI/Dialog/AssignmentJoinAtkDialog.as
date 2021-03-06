﻿package src.UI.Dialog {

    import flash.events.Event;

    import src.Global;
    import src.Map.City;
    import src.Map.TileLocator;
    import src.Objects.Effects.Formula;
    import src.Objects.Troop.*;
    import src.Util.DateUtil;
    import src.Util.StringHelper;

    public class AssignmentJoinAtkDialog extends AttackTroopDialog {

		protected var assignment: *;
		protected var distance: int;
	
		public function AssignmentJoinAtkDialog(city: City, onAccept: Function, assignment: *):void
		{
			super(city, onAccept, false);
			
			title = "Join Assignment";
			
			this.assignment = assignment;
			this.distance = TileLocator.distance(city.primaryPosition.x, city.primaryPosition.y, 1, assignment.x, assignment.y, 1);
		}

		override protected function updateSpeedInfo(e:Event = null):void 
		{
			var stub: TroopStub = getTroop();			
			if (stub.getIndividualUnitCount() == 0) {
				lblTroopSpeed.setText(StringHelper.localize("TROOP_CREATE_DRAG_HINT"));
			}
			else {				
				var moveTime: int = Formula.moveTimeTotal(city, stub.getSpeed(city), distance, true);
				if (Global.map.getServerTime() + moveTime > assignment.endTime) {
					var diff: int = Global.map.getServerTime() + moveTime - assignment.endTime;
					lblTroopSpeed.setText("Your units will be "+ DateUtil.niceTime(diff)+" late. Choose faster units to arrive on time.");
				}
				else {
					lblTroopSpeed.setText(StringHelper.localize("TROOP_CREATE_DRAG_HINT"));
				}
			}
		}
	}

}

