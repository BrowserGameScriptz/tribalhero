﻿package src.Objects.Factories {
    import src.Map.Map;
    import src.Objects.Prototypes.PropertyPrototype;
    import src.Util.BinaryList.*;

    /**
	 * ...
	 * @author Default
	 */
	public class PropertyFactory {

		private static var map: Map;
		private static var properties: BinaryList;

		public static function init(_map: Map, data: XML):void
		{
			map = _map;

			properties = new BinaryList(PropertyPrototype.sortOnTypeAndIndex, PropertyPrototype.compareType);

			for each (var propNode: XML in data.Property.*) {
				var propPrototype: PropertyPrototype = new PropertyPrototype(
				propNode.@index,
				propNode.@type,
				propNode.@name,
				propNode.@datatype,
				propNode.@visibility == "PUBLIC" ? 1 : 0,
				propNode.@perhour.toUpperCase() == "TRUE" ? true : false,
				propNode.@icon,
				propNode.@tooltip
				);
				properties.add(propPrototype, false);
			}

			properties.sort();
		}

		public static function getAllProperties(type: int): Array
		{
			return properties.getRange(type);
		}

		public static function getProperties(type: int, visibility: int): Array
		{
			var props: Array = properties.getRange(type);

			for (var i: int = props.length - 1; i >= 0; i--) {
				if (props[0].visibility != visibility) {
					props.splice(i, 1);
				}
			}

			return props;
		}

	}

}

