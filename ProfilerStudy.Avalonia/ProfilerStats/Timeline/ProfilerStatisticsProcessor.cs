using System;
using System.Linq;
using System.Reflection;
using ProfilerStudy.Avalonia.Timeline;

namespace ProfilerStudy.Avalonia.ProfilerStats.Timeline;

public static class ProfilerStatisticsProcessor
{
	public static DiagramMetadata GetMetadataFromProfilerStatisticsInfo()
	{
		var diagramMeta = new DiagramMetadata();

		var statisticsInfoType = typeof(ProfilerStatisticsInfo);
		var properties = statisticsInfoType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
		foreach (var property in properties)
		{
			var plotAttr = property.GetCustomAttribute<PlotContainerAttribute>();
			if (plotAttr == null)
			{
				continue;
			}

			var plotMeta = BuildCurvePlotMetadata(property, plotAttr);
			if (plotMeta != null)
			{
				diagramMeta.PlotList.Add(plotMeta);
				diagramMeta.PlotDictionary.Add(property.Name, plotMeta);
			}
		}

		return diagramMeta;
	}

	public static bool CanConvertToDouble(Type type)
	{
		type = Nullable.GetUnderlyingType(type) ?? type;
		return typeof(IConvertible).IsAssignableFrom(type);
	}

	public static bool TryConvertToDouble(object value, out double result)
	{
		result = 0.0;
		if (value == null)
		{
			return false;
		}

		try
		{
			result = Convert.ToDouble(value);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static CurvePlotMetadata? BuildCurvePlotMetadata(PropertyInfo plotProperty, PlotContainerAttribute containerAttribute)
	{
		var plotType = plotProperty.PropertyType;
		var properties = plotType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

		var plotMeta = new CurvePlotMetadata
		{
			PropertyName = plotProperty.Name,
			PlotTitle = containerAttribute.PlotTitle,
			IsMainPlot = containerAttribute.IsMainPlot,
			ContainerProperty = plotProperty,
		};

		foreach (var property in properties)
		{
			if (property.GetCustomAttribute<CurveIgnoreFieldAttribute>() != null)
			{
				continue;
			}

			var fieldAttr = property.GetCustomAttribute<CurveFieldAttribute>();
			if (fieldAttr == null)
			{
				continue;
			}

			var fieldMetadata = BuildCurveFieldMetadata(property, fieldAttr);
			if (fieldMetadata != null)
			{
				plotMeta.SubFields.Add(fieldMetadata);
				plotMeta.FieldDictionary.Add(property.Name, fieldMetadata);
			}
		}

		return plotMeta;
	}

	private static CurveFieldMetadata? BuildCurveFieldMetadata(PropertyInfo fieldProperty, CurveFieldAttribute fieldAttribute)
	{
		var fieldType = fieldProperty.PropertyType;
		if (!CanConvertToDouble(fieldType))
		{
			return null;
		}

		return new CurveFieldMetadata
		{
			PropertyName = fieldProperty.Name,
			CurveLabel = fieldAttribute.CurveLabel,
			CurveUnit = fieldAttribute.CurveUnit,
			LineColor = ScottPlotColorUtil.GetColor(fieldAttribute.LineColorIndex),
			FieldProperty = fieldProperty,
		};
	}
}
