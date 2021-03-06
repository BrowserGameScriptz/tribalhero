﻿package src.Map
{
    import flash.events.Event;
    import flash.events.EventDispatcher;
    import flash.geom.Point;
    import flash.geom.Rectangle;

    import src.Constants;

    public class Camera extends EventDispatcher
	{
		public static const ON_MOVE: String = "ON_MOVE";

		public var currentPosition: ScreenPosition = new ScreenPosition();

		// Saves the x,y to see if we need to
		private var isDirty: Boolean = false;
		private var updating: Boolean = false;

		//Allows us to cue a position and return to it later
		private var cuePosition: ScreenPosition;
		
		//Zooming factor
		private var _zoomFactor: int = 75;
		private var zoomFactorOverOne: Number = (1.0 / (75 / 100.0));

		public function beginMove(): void {
			if (updating) return;

			updating = true;
		}

		public function endMove(): void {
			updating = false;

			if (!isDirty) {
                return;
            }

			isDirty = false;

			fireOnMove();
		}

		private function fireOnMove() : void {
			if (updating) {
				isDirty = true;
				return;
			}

			dispatchEvent(new Event(ON_MOVE));
		}

		public function reset(): void
		{
			currentPosition = new ScreenPosition();
		}

		public function cue(): void {
			cuePosition = currentPosition.copy();
		}

		public function goToCue(): void {
			currentPosition = cuePosition.copy();
		}

		public function get miniMapX(): int
		{
			return (currentPosition.x / Constants.tileW) * Constants.miniMapTileW;
		}

		public function get miniMapY(): int
		{
			return (currentPosition.y / Constants.tileH) * Constants.miniMapTileH;
		}
		
		public function get miniMapCenter(): Point
		{			
			var point: ScreenPosition = GetCenter();
			return new Point((point.x / Constants.tileW) * Constants.miniMapTileW, (point.y / Constants.tileH) * Constants.miniMapTileH);
		}

		public function Camera(x: Number, y: Number)
		{
			currentPosition.setXAsNumber(x);
            currentPosition.setYAsNumber(y);
		}

		public function MoveLeft(step :int): void
		{
			MoveLeftEx(step);
		}

		private function MoveLeftEx(step: int): void
		{
            currentPosition.x -= step;

			if (currentPosition.x < 0) {
                currentPosition.x = 0;
            }

			fireOnMove();
		}

		public function MoveRight(step: int): void
		{
			MoveRightEx(step);
		}

		private  function MoveRightEx(step: int): void
		{
            currentPosition.x += step;

			if (currentPosition.x > Constants.mapW - Constants.screenW - (Constants.tileW / 2)) {
                currentPosition.x = Constants.mapW - Constants.screenW - (Constants.tileW / 2);
            }
			
			fireOnMove();
		}

		public function MoveDown(step: int): void
		{
			MoveDownEx(step);
		}

		private function MoveDownEx(step: int): void
		{
            currentPosition.y += step;

			if (currentPosition.y > Constants.mapTileH * int(Constants.tileH / 2) - Constants.screenH - int(Constants.tileH / 2)) {
                currentPosition.y = Constants.mapTileH * int(Constants.tileH / 2) - Constants.screenH - int(Constants.tileH / 2);
            }
			
			fireOnMove();
		}

		public function MoveUp(step: int): void
		{
			MoveUpEx(step);			
		}

		private function MoveUpEx(step: int): void
		{
            currentPosition.y -= step;

			if (currentPosition.y < 0)  {
                currentPosition.y = 0;
            }
			
			fireOnMove();
		}

		public function Move(dx: int, dy: int): void
		{
			beginMove();

			if (dx > 0) {
			    MoveRightEx(dx);
            }
			else {
			    MoveLeftEx(-dx);
            }

			if (dy > 0) {
			    MoveDownEx(dy);
            }
			else {
			    MoveUpEx(-dy);
            }

			endMove();
		}

		public function ScrollToCenter(screenPos: ScreenPosition): void
		{
            var zoomFactorPercentage: Number = getZoomFactorPercentage();
			ScrollTo(new ScreenPosition(
                    screenPos.getXAsNumber() - (Number(Constants.screenW) / zoomFactorPercentage) / 2.0,
                    screenPos.getYAsNumber() - (Constants.screenH / zoomFactorPercentage) / 2.0));
		}
		
		public function GetCenter(): ScreenPosition
		{
            var zoomFactorPercentage: Number = getZoomFactorPercentage();
            return new ScreenPosition(
                    currentPosition.getXAsNumber() + (Number(Constants.screenW) / zoomFactorPercentage) / 2.0,
                    currentPosition.getYAsNumber() + (Constants.screenH / zoomFactorPercentage) / 2.0);
		}

		public function CameraRectangle() : Rectangle
		{
			return new Rectangle(
                    currentPosition.x,
                    currentPosition.y,
                    Constants.screenW * zoomFactorOverOne,
                    Constants.screenH * zoomFactorOverOne);
		}

		public function ScrollTo(screenPos: ScreenPosition): void
		{
			if (currentPosition.x < 0) currentPosition.x = 0;
			if (currentPosition.y < 0) currentPosition.y = 0;

			currentPosition.setXAsNumber(Math.min(screenPos.getXAsNumber(), int(Constants.mapW - (Constants.screenW * zoomFactorOverOne) - (Constants.tileW / 2.0))));
			currentPosition.setYAsNumber(Math.min(screenPos.getYAsNumber(), int(Constants.mapTileH * int(Constants.tileH / 2.0) - (Constants.screenH * zoomFactorOverOne) - int(Constants.tileH / 2.0))));

			fireOnMove();
		}

        //noinspection JSUnusedGlobalSymbols
        public function set zoomFactor(factor: int): void {
            _zoomFactor = factor;
            zoomFactorOverOne = (1.0 / getZoomFactorPercentage());
            ScrollTo(currentPosition);
        }

        public function get zoomFactor(): int {
            return _zoomFactor;
        }

		public function getZoomFactorOverOne(): Number {
			return zoomFactorOverOne;
		}
		
		public function getZoomFactorPercentage(): Number {
			return zoomFactor / 100.0;
		}
		
	}
}

