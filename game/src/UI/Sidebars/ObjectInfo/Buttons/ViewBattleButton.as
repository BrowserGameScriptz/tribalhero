﻿
package src.UI.Sidebars.ObjectInfo.Buttons {
	import flash.events.Event;
	import flash.events.MouseEvent;
	import src.Objects.Factories.*;
	import src.Objects.GameObject;
	import src.Objects.Actions.ActionButton;
	import src.Objects.SimpleGameObject;
	import src.Objects.States.BattleState;
	import src.UI.Cursors.*;
	import src.UI.Dialog.BattleViewer;
	import src.UI.Tooltips.TextTooltip;

	public class ViewBattleButton extends ActionButton
	{
		private var tooltip: TextTooltip;

		public function ViewBattleButton(parentObj: SimpleGameObject)
		{
			super(parentObj, "View Battle");

			tooltip = new TextTooltip("View Battle");

			addEventListener(MouseEvent.CLICK, onMouseClick);
			addEventListener(MouseEvent.MOUSE_OVER, onMouseOver);
			addEventListener(MouseEvent.MOUSE_MOVE, onMouseOver);
			addEventListener(MouseEvent.MOUSE_OUT, onMouseOut);
		}

		// Override disable since this button can always be clicked
		override public function disable():void
		{
		}

		public function onMouseOver(event: MouseEvent):void
		{
			tooltip.show(this);
		}

		public function onMouseOut(event: MouseEvent):void
		{
			tooltip.hide();
		}

		public function onMouseClick(event: Event):void
		{
			if (isEnabled())
			{
				if (parentObj.state is BattleState) {
					var battleViewer: BattleViewer = new BattleViewer((parentObj.state as BattleState).battleId);
					battleViewer.show(null, false);
				}
			}
		}
	}

}

