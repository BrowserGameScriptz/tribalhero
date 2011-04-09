﻿package src.Map
{
	import flash.display.*;
	import flash.geom.Point;
	import flash.geom.Rectangle;
	import src.Objects.Factories.ForestFactory;
	import src.Objects.Factories.ObjectFactory;
	import src.Objects.NewCityPlaceholder;
	import src.Util.BinaryList.*;
	import src.Util.Util;
	import src.Constants;
	import src.Map.Map;
	import src.Map.Camera;
	import src.Objects.GameObject;
	import src.Objects.SimpleGameObject;
	import src.Objects.SimpleObject;

	public class Region extends Sprite
	{
		public var id: int;
		private var tiles: Array;
		private var globalX: int;
		private var globalY: int;
		private var bitmapParts: Array;
		private var objects: BinaryList = new BinaryList(SimpleGameObject.sortOnCityIdAndObjId, SimpleGameObject.compareCityIdAndObjId);
		private var map: Map;
		
		public function Region(id: int, data: Array, map: Map)
		{
			mouseEnabled = false;

			this.id = id;
			this.map = map;			
			this.tiles = data;
			
			bitmapParts = new Array();			

			globalX = (id % Constants.mapRegionW) * Constants.regionW;
			globalY = int(id / Constants.mapRegionW) * (Constants.regionH / 2);
			
			createRegion();

			if (Constants.debug == 3)
			{
				/* adds an outline to this region */
				graphics.beginFill(0x000000, 0);
				graphics.lineStyle(2, 0x000000);
				graphics.drawRect(0, 0, width, height);
				graphics.endFill();
			}
		}

		// Removes all of the tiles from this region
		private function cleanTiles(): void {

			for (var i: int = 0; i < bitmapParts.length; i++)
			{
				removeChild(bitmapParts[i]);
				bitmapParts[i].bitmapData.dispose();
				bitmapParts[i] = null;
			}

			bitmapParts = new Array();
		}

		public function disposeData():void
		{
			cleanTiles();

			for each(var gameObj: SimpleGameObject in objects.each())
			{
				map.objContainer.removeObject(gameObj);
			}

			objects.clear();

			for (var i: int = numChildren - 1; i >= 0; i--) {
				removeChildAt(i);
			}

			objects = null;
			map = null;
			tiles = null;
		}

		public function createRegion():void
		{
			if (Constants.debug >= 2)
			Util.log("Creating region id: " + id + " " + globalX + "," + globalY);
			
			for (var a:int = 0; a < Math.ceil(Constants.regionW / Constants.regionBitmapW); a++)
			{
				for (var b:int = 0; b < Math.ceil(Constants.regionH / Constants.regionBitmapH); b++)
				{
					if (Constants.debug>=3)
					Util.log("Creating region part: " + (a * Constants.regionBitmapTileW) + "," + (b * Constants.regionBitmapTileH));

					createRegionPart(Constants.tileset, a * Constants.regionBitmapTileW, b * Constants.regionBitmapTileH);
					break;
				}
				break;
			}
		}

		public function sortObjects():void
		{
			objects.sort();
		}

		public function setTile(x: int, y: int, tileType: int, redraw: Boolean = true): void {
			var pt: Point = getTilePos(x, y);
			
			tiles[pt.y][pt.x] = tileType;
			
			clearPlaceholders(x, y);
			addPlaceholderObjects(tileType, x, y);
			
			if (redraw) 
				this.redraw();
		}

		public function redraw() : void {
			cleanTiles();
			createRegion();
		}

		public function createRegionPart(tileset:TileSet, x: int, y:int):void
		{
			var bg:Bitmap = new Bitmap(new BitmapData(Constants.regionBitmapW + Constants.tileW / 2, Constants.regionBitmapH / 2 + Constants.tileH / 2, true, 0));
			bg.smoothing = false;
			
			var tileHDiv2: int = Constants.tileH / 2;
			var tileHTimes2: int = Constants.tileH * 2;
			var tileWDiv2: int = Constants.tileW / 2;
			var tileWTimes2: int = Constants.tileW * 2;
			var oddShift: int = int(Constants.tileW / 2) * -1;
			var regionStartingX: int = (id % Constants.mapRegionW) * Constants.regionTileW;
			var regionStartingY: int = int(id / Constants.mapRegionW) * Constants.regionTileH;
			
			for (var bY:int = 1; bY <= Constants.regionBitmapTileH; bY++)
			{
				for (var aX:int = 1; aX <= Constants.regionBitmapTileW; aX++)
				{
					var tileX: int = aX - 1 + x;
					var tileY: int = bY - 1 + y;
					var tileid:int = tiles[tileY][tileX];
					
					addPlaceholderObjects(tileid, tileX + regionStartingX, tileY + regionStartingY);

					var tilesetsrcX:int = int(tileid % Constants.tileSetTileW) * Constants.tileW;
					var tilesetsrcY:int = int(tileid / Constants.tileSetTileW) * tileHTimes2;

					var xadd:int = 0;
					var yadd:int = 0;

					if ((bY % 2) == 1) //odd tile
						xadd = oddShift;

					var xcoord:int = int((aX - 1) * Constants.tileW + xadd);
					var ycoord:int = int((bY - 2) * tileHDiv2);

					var drawTo:Point = new Point(xcoord + tileWDiv2, (ycoord + tileHDiv2) - (Constants.tileH));
					var srcRect:Rectangle = new Rectangle(tilesetsrcX, tilesetsrcY, Constants.tileW, tileHTimes2);

					//Util.log(aX + "," + bY + " Tile Id:" + tileid + " draw to " + drawTo.x + "," + drawTo.y + " src " + srcRect.x + "," + srcRect.y + " " + srcRect.width + "," + srcRect.height);
					bg.bitmapData.copyPixels(tileset, srcRect, drawTo, null, null, true);
				}
			}

			bitmapParts.push(bg);

			bg.x = (x / Constants.regionBitmapTileW) * Constants.regionBitmapW;
			bg.y = (y / Constants.regionBitmapTileH) * (Constants.regionBitmapH / 2);

			addChild(bg);

			if (Constants.debug == 3)
			{
				/* adds an outline to each region part	*/
				graphics.beginFill(0, 0);
				graphics.lineStyle(2, 0x0000FF);
				graphics.drawRect(bg.x, bg.y, bg.bitmapData.width, bg.bitmapData.height);
				graphics.endFill();
			}
		}
		
		private function clearPlaceholders(x :int, y: int) : void
		{
			var objs: Array = getObjectsAt(x, y);
			
			for each (var obj: SimpleGameObject in objs) {
				if (objs is NewCityPlaceholder)
					removeGameObject(obj, true);
			}
		}
		
		private function addPlaceholderObjects(tileId: int, x: int, y: int) : void 
		{				
			if (tileId == Constants.cityStartTile) {
				var obj: NewCityPlaceholder = ObjectFactory.getNewCityPlaceholderInstance();
				var coord: Point = MapUtil.getScreenCoord(x, y);
				obj.setProperties(1, 100, coord.x, coord.y);				
				obj.init(map, 0, 0, 0, 0);
				addGameObject(obj, false);
			}
		}

		public function getObjectsAt(x: int, y: int, objClass: Class = null): Array
		{
			var objs: Array = new Array();
			for each(var gameObj: SimpleGameObject in objects.each())
			{
				if (objClass != null && !(gameObj is objClass)) continue;

				if (gameObj.getX() == x && gameObj.getY() == y && gameObj.visible) {
					objs.push(gameObj);
				}
			}

			return objs;
		}

		public function getTileAt(x: int, y: int) : int {
			var pt: Point = getTilePos(x, y);

			return tiles[pt.y][pt.x];
		}

		private function getTilePos(x: int, y: int) : Point {
			var regionStartingX: int = (id % Constants.mapRegionW);
			var regionStartingY: int = int(id / Constants.mapRegionW);

			x -= (regionStartingX * Constants.regionTileW);
			y -= (regionStartingY * Constants.regionTileH);

			return new Point(x, y);
		}

		public function addObject(level: int, type: int, playerId: int, cityId: int, objectId: int, hpPercent: int, objX: int, objY : int, resort: Boolean = true) : SimpleGameObject
		{
			var existingObj: SimpleObject = objects.get([cityId, objectId]);

			if (existingObj != null) //don't add if obj already exists
			{	
				Util.log("Obj id " + objectId + " already exists in region " + id);
				return null;
			}

			var obj: SimpleObject = ObjectFactory.getInstance(type, level);

			if (obj == null)			
				return null;			

			var gameObj: SimpleGameObject = (obj as SimpleGameObject);
			gameObj.name = "Game Obj " + objectId;
			gameObj.init(map, playerId, cityId, objectId, type);

			var coord: Point = MapUtil.getScreenCoord(objX, objY);

			gameObj.setProperties(level, hpPercent, coord.x, coord.y);

			//set object callback when it's selected. Only gameobjects can be selected.
			if (gameObj is GameObject)
				(gameObj as GameObject).setOnSelect(map.selectObject);

			//add to object container and to internal list
			map.objContainer.addObject(gameObj);
			objects.add(gameObj, resort);
			
			//select object if the map is waiting for it to be selected
			if (map.selectViewable != null && map.selectViewable.cityId == gameObj.cityId && map.selectViewable.objectId == gameObj.objectId)
			{
				map.selectObject(gameObj as GameObject);
			}

			return gameObj;
		}

		public function addGameObject(gameObj: SimpleGameObject, resort: Boolean = true):void
		{
			if (gameObj.cityId > 0 && gameObj.objectId > 0)
				removeObject(gameObj.cityId, gameObj.objectId, false);

			map.objContainer.addObject(gameObj);

			objects.add(gameObj, resort);
		}

		public function removeObject(cityId: int, objectId: int, dispose: Boolean = true): SimpleGameObject
		{
			var gameObj: SimpleGameObject = objects.remove([cityId, objectId]);

			if (gameObj == null)
				return null;	

			map.objContainer.removeObject(gameObj, 0, dispose);

			return gameObj;
		}
		
		public function removeGameObject(obj: SimpleGameObject, dispose: Boolean = true) : void
		{
			for (var i: int = 0; i < objects.size(); i++) {
				if (objects.getByIndex(i) != obj)				
					continue;
					
				objects.removeByIndex(i);				
				break;
			}
			
			map.objContainer.removeObject(obj, 0, dispose);
		}

		public function getObject(cityId: int, objectId: int): SimpleGameObject
		{
			return objects.get([cityId, objectId]);
		}

		public function moveWithCamera(camera: Camera):void
		{
			x = globalX - camera.x - int(Constants.tileW / 2);
			y = globalY - camera.y - int(Constants.tileH / 2);
		}

		public static function sortOnId(a:Region, b:Region):Number
		{
			var aId:Number = a.id;
			var bId:Number = b.id;

			if(aId > bId) {
				return 1;
			} else if(aId < bId) {
				return -1;
			} else  {
				return 0;
			}
		}

		public static function compare(a: Region, value: int):int
		{
			return a.id - value;
		}
	}
}

