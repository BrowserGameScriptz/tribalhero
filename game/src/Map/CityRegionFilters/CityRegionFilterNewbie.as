package src.Map.CityRegionFilters 
{
	import fl.lang.Locale;
	import src.Constants;
	import src.Map.CityRegionFilters.CityRegionFilter;
	import src.Map.CityRegionLegend;
	import src.Map.CityRegionObject;
	import src.Objects.Factories.ObjectFactory;
	import flash.geom.*;
	import src.UI.Tooltips.MinimapInfoTooltip;
	import flash.events.*;
	import src.Global;
	import flash.display.*;
	/**
	 * ...
	 * @author Anthony Lam
	 */
	public class CityRegionFilterNewbie extends CityRegionFilter 
	{
		override public function getName(): String {
			return "Newbie";
		}

		override public function applyCity(obj: CityRegionObject) : void {
			// If it's our city, we just show a special flag
			var img: DisplayObject;

			img = new DOT_SPRITE;
			obj.sprite = img;
			
			if (Global.map.cities.get(obj.groupId)) {
				obj.transform.colorTransform = new ColorTransform();
			} else if(obj.extraProps.isNewbie) {
				obj.transform.colorTransform = new ColorTransform(.5, .5, .5, 1, 0, 0, 255);
			} else {
				obj.transform.colorTransform = new ColorTransform(.5, .5, .5, 1, DEFAULT_COLORS[0].r, DEFAULT_COLORS[0].g, DEFAULT_COLORS[0].b);
			}
			obj.addChild(img);
		}
		
		override public function applyLegend(legend: CityRegionLegend) : void {
			var icon: DisplayObject = new DOT_SPRITE;
			legend.add(icon, Locale.loadString("MINIMAP_LEGEND_CITY"));
			
			icon = new DOT_SPRITE;
			icon.transform.colorTransform = new ColorTransform(.5, .5, .5, 1, 0, 0, 255);
			legend.add(icon, Locale.loadString("MINIMAP_LEGEND_NEWBIE_YES"));
			
			icon = new DOT_SPRITE;
			icon.transform.colorTransform = new ColorTransform(.5, .5, .5, 1, DEFAULT_COLORS[0].r, DEFAULT_COLORS[0].g, DEFAULT_COLORS[0].b);
			legend.add(icon, Locale.loadString("MINIMAP_LEGEND_NEWBIE_NO"));
			
			legend.setLegendTitle(Locale.loadString("MINIMAP_LEGEND_NEWBIE"));
		}
	}
}