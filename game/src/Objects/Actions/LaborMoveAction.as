/**
 * ...
 * @author Default
 * @version 0.1
 */

package src.Objects.Actions {
	import src.Objects.Actions.IAction;
	import src.Objects.GameObject;
	import src.Objects.Prototypes.StructurePrototype;
	import src.UI.Sidebars.ObjectInfo.Buttons.LaborMoveButton;

	public class LaborMoveAction extends Action implements IAction
	{
		public function LaborMoveAction()
		{
			super(Action.LABOR_MOVE);
		}

		public function toString(): String
		{
			return "Transferring Workers";
		}

		public function getButton(parentObj: GameObject, sender: StructurePrototype): ActionButton
		{
			return new LaborMoveButton(parentObj) as ActionButton;
		}

	}

}

