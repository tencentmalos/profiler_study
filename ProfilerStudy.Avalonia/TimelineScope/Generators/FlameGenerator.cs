using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AvaloniaColor = Avalonia.Media.Color;
using ScottPlotColor = ScottPlot.Color;
using ProfilerStudy.Avalonia.Timeline;

namespace ProfilerStudy.Avalonia.Timeline;

/// <summary>
/// Flame Graph Detail View - Simulates inverted flame graph of execution stack
/// </summary>
public class FlameGenerator : IFlameGenerator
{
    /// <summary>
    /// Execution stack frame data
    /// </summary>
    public class StackFrame
    {
        public string FunctionName { get; set; } = "";
        public double StartTime { get; set; }
        public double EndTime { get; set; }
        public int StackLevel { get; set; }
        public ScottPlotColor Color { get; set; } = Colors.Blue;
        public string Module { get; set; } = "";
        public int FrameIndex { get; set; } = -1;
        public string SourceText { get; set; } = "";
        public string SourceFile { get; set; } = "";
        public int SourceLine { get; set; } = -1;
        public double Duration => EndTime - StartTime;
    }

    /// <summary>
    /// One frame info - similar to OneCurveInfo pattern
    /// </summary>
    public class OneFrameInfo
    {
        public StackFrame      FrameData;
        public Rectangle?      Rectangle;
        public Text?           Label;

        public OneFrameInfo(StackFrame frameData)
        {
            FrameData = frameData;
        }

        public void UpdateVisuals(AvaPlot plot, double visibleTimeRange)
        {
            // Update rectangle if needed
            if (Rectangle != null)
            {
                var x = FrameData.StartTime;
                var width = FrameData.Duration;
                var y = (8 - FrameData.StackLevel - 1) * 20; // Assuming max 8 levels and 20 height
                var height = 20 * 0.9;

                Rectangle.X1 = x;
                Rectangle.X2 = x + width;
                Rectangle.Y1 = y;
                Rectangle.Y2 = y + height;
            }

            // Update label visibility and text based on zoom level
            if (Label != null)
            {
                var pixelWidth = (FrameData.Duration / visibleTimeRange) * 800; // Assume 800px width
                var minWidthForText = Math.Max(2, visibleTimeRange / 50);
                
                if (FrameData.Duration > minWidthForText && pixelWidth > 30)
                {
                    Label.IsVisible = true;
                    
                    // Adjust font size
                    var baseFontSize = Math.Max(8, Math.Min(14, pixelWidth / FrameData.FunctionName.Length * 1.5));
                    var zoomFactor = Math.Min(2.0, Math.Max(0.5, 100.0 / visibleTimeRange));
                    var adjustedFontSize = baseFontSize * Math.Sqrt(zoomFactor);
                    
                    Label.LabelFontSize = (float)Math.Max(6, Math.Min(16, adjustedFontSize));
                    
                    // Truncate text if needed
                    if (pixelWidth < FrameData.FunctionName.Length * Label.LabelFontSize * 0.6)
                    {
                        var maxChars = Math.Max(3, (int)(pixelWidth / (Label.LabelFontSize * 0.6)));
                        if (FrameData.FunctionName.Length > maxChars)
                        {
                            Label.LabelText = FrameData.FunctionName.Substring(0, maxChars - 2) + "..";
                        }
                        else
                        {
                            Label.LabelText = FrameData.FunctionName;
                        }
                    }
                    else
                    {
                        Label.LabelText = FrameData.FunctionName;
                    }
                }
                else
                {
                    Label.IsVisible = false;
                }
            }
        }
    }

    /// <summary>
    /// Flame graph configuration
    /// </summary>
    public class FlameGraphConfig
    {
        public string Title { get; set; } = "Flame Graph - Execution Stack";
        public double TimelineStart { get; set; } = 0;
        public double TimelineEnd { get; set; } = 100;
        public int MaxStackDepth { get; set; } = 10;
        public double FrameHeight { get; set; } = 20;
        public List<StackFrame> StackFrames { get; set; } = new();
    }

    private AvaPlot                 _plot;
    private FlameGraphConfig        _config;
    private List<OneFrameInfo>      _frameList = new List<OneFrameInfo>();
    private Crosshair               _crosshair = null!;
    private Tooltip                 _tooltip = null!;
    private bool                    _mouseInteractionAttached;
    private StackFrame?             _hoveredFrame;

    public FlameGraphConfig     Config => _config;
    public AvaPlot              Plot => _plot;

    public event Action<StackFrame>? FrameClicked;

    public OneFrameInfo this[int index]
    {
        get
        {
            if(index < 0 || index >= _frameList.Count) throw new ArgumentOutOfRangeException("index");

            return _frameList[index];
        }
    }

    // Predefined function names and modules
    private static readonly string[] FunctionNames = new[]
    {
        "main()", "processData()", "calculateMetrics()", "renderFrame()", "updateUI()",
        "handleEvent()", "parseInput()", "validateData()", "saveToDatabase()", "loadConfiguration()",
        "initializeSystem()", "cleanupResources()", "allocateMemory()", "optimizePerformance()", "debugTrace()",
        "networkRequest()", "fileIO()", "cryptoOperation()", "imageProcessing()", "audioDecoding()",
        "videoRendering()", "physicsSimulation()", "aiInference()", "dataCompression()", "cacheUpdate()"
    };

    private static readonly string[] ModuleNames = new[]
    {
        "Core", "UI", "Network", "Database", "Graphics", "Audio", "Physics", "AI", "Utils", "Security"
    };

    public FlameGenerator(AvaPlot targetPlot)
    {
        _plot = targetPlot;
        _config = new FlameGraphConfig();
        SetupFlameGraph();
    }

    public void Configure(FlameGraphConfig config)
    {
        _config = config ?? new FlameGraphConfig();
        SetupFlameGraph();
        _plot.Refresh();
    }

    /// <summary>
    /// Generate simulated flame graph data
    /// </summary>
    private FlameGraphConfig GenerateFlameGraphData()
    {
        var random = new Random(42);
        var frames = new List<StackFrame>();
        var config = new FlameGraphConfig
        {
            TimelineStart = 0,
            TimelineEnd = 100,
            MaxStackDepth = 8
        };

        // Generate main function call stack
        GenerateStackFrames(frames, random, 0, 100, 0, config.MaxStackDepth);

        config.StackFrames = frames;
        return config;
    }

    /// <summary>
    /// Recursively generate execution stack frames
    /// </summary>
    private void GenerateStackFrames(List<StackFrame> frames, Random random, double startTime, double endTime, int currentLevel, int maxLevel)
    {
        if (currentLevel >= maxLevel || endTime - startTime < 1) return;

        double currentTime = startTime;
        int functionIndex = 0;

        while (currentTime < endTime)
        {
            // Randomly select function name and duration
            var functionName = FunctionNames[random.Next(FunctionNames.Length)];
            var module = ModuleNames[random.Next(ModuleNames.Length)];
            var duration = Math.Min(random.NextDouble() * 15 + 2, endTime - currentTime);
            var frameEndTime = currentTime + duration;

            // Use different colors for different levels
            var color = ScottPlotColorUtil.GetMutedColor(currentLevel);
            
            // Add transparency variation
            var alpha = (byte)(180 + (currentLevel * 10) % 75);
            color = new ScottPlotColor(color.R, color.G, color.B, alpha);

            var frame = new StackFrame
            {
                FunctionName = $"{module}.{functionName}",
                StartTime = currentTime,
                EndTime = frameEndTime,
                StackLevel = currentLevel,
                Color = color,
                Module = module
            };

            frames.Add(frame);

            // Recursively generate sub-calls (with probability)
            if (random.NextDouble() > 0.3 && currentLevel < maxLevel - 1)
            {
                // Generate sub-calls during current function execution
                var subCallStart = currentTime + duration * 0.1;
                var subCallEnd = frameEndTime - duration * 0.1;
                
                if (subCallEnd > subCallStart)
                {
                    GenerateStackFrames(frames, random, subCallStart, subCallEnd, currentLevel + 1, maxLevel);
                }
            }

            currentTime = frameEndTime + random.NextDouble() * 2; // Add small gap
            functionIndex++;
        }
    }

    /// <summary>
    /// Setup flame graph plotting
    /// </summary>
    private void SetupFlameGraph()
    {
        _plot.Plot.Clear();
        _frameList.Clear();

        // Draw each stack frame as rectangle
        foreach (var frame in _config.StackFrames)
        {
            var frameInfo = new OneFrameInfo(frame);

            // Calculate rectangle position (inverted flame graph: stack bottom at top)
            var x = frame.StartTime;
            var width = frame.Duration;
            var y = (_config.MaxStackDepth - frame.StackLevel - 1) * _config.FrameHeight;
            var height = _config.FrameHeight * 0.9; // Leave some gap

            // Create rectangle
            var rect = _plot.Plot.Add.Rectangle(x, x + width, y, y + height);
            rect.FillColor = frame.Color;
            rect.LineColor = TimelineScopeThemeHelper.GetPlotColor(TimelineScopeThemeHelper.PanelBorderBrushKey, "#FFD0D9E4");
            rect.LineWidth = 0.5f;
            frameInfo.Rectangle = rect;

            // Add text label (only for rectangles wide enough to display text)
            if (width > 5)
            {
                var text = _plot.Plot.Add.Text(frame.FunctionName, x + width / 2, y + height / 2);
                text.LabelFontSize = (float)Math.Max(8, Math.Min(12, width / frame.FunctionName.Length * 2));
                TimelineScopeThemeHelper.ApplyLabelTheme(text);
                text.LabelAlignment = Alignment.MiddleCenter;
                frameInfo.Label = text;
            }

            _frameList.Add(frameInfo);
        }

        // Setup axes
        _plot.Plot.Axes.SetLimitsX(_config.TimelineStart, _config.TimelineEnd);
        _plot.Plot.Axes.SetLimitsY(-_config.FrameHeight, _config.MaxStackDepth * _config.FrameHeight);
        
        // Hide Y-axis (flame graphs typically don't show Y-axis ticks)
        _plot.Plot.Axes.Left.IsVisible = false;
        _plot.Plot.Axes.Right.IsVisible = false;
        
        // Set X-axis label
        _plot.Plot.Axes.Bottom.Label.Text = "Time (ms)";
        
        // Hide grid
        _plot.Plot.Grid.IsVisible = false;
        TimelineScopeThemeHelper.ApplyPlotTheme(_plot);

        // Add mouse hover functionality
        SetupMouseInteraction();
    }

    /// <summary>
    /// Setup mouse interaction
    /// </summary>
    private void SetupMouseInteraction()
    {
        if (_mouseInteractionAttached is false)
        {
            _plot.PointerMoved += OnMouseMove;
            _plot.PointerExited += OnMouseExit;
            _plot.PointerPressed += OnMousePressed;
            _mouseInteractionAttached = true;
        }

        _crosshair = _plot.Plot.Add.Crosshair(0, 0);
        _crosshair.IsVisible = false;
        TimelineScopeThemeHelper.ApplyCrosshairTheme(_crosshair, TimelineScopeThemeHelper.InfoBrushKey, "#FF2169D6");
        _crosshair.LineWidth = 1;
        _crosshair.LinePattern = LinePattern.Dashed;

        _tooltip = _plot.Plot.Add.Tooltip(new Coordinates(0, 0), "", new Coordinates(0, 0));
        _tooltip.IsVisible = false;
        _tooltip.LineWidth = 1;
        _tooltip.LabelFontSize = 10;
        TimelineScopeThemeHelper.ApplyTooltipTheme(_tooltip);
    }

    /// <summary>
    /// Handle mouse move event
    /// </summary>
    private void OnMouseMove(object? sender, PointerEventArgs e)
    {
        var pos = e.GetPosition(_plot);
        Pixel mousePixel = new(pos.X, pos.Y);
        Coordinates mouseLocation = _plot.Plot.GetCoordinates(mousePixel);

        // Find stack frame under mouse position
        var hoveredFrame = FindFrameAtPosition(mouseLocation.X, mouseLocation.Y);

        if (hoveredFrame != null)
        {
            if (ReferenceEquals(_hoveredFrame, hoveredFrame))
            {
                return;
            }

            _hoveredFrame = hoveredFrame;

            // Show crosshair
            _crosshair.IsVisible = true;
            _crosshair.Position = mouseLocation;

            // Build tooltip text
            var tooltipText = new StringBuilder();
            tooltipText.AppendLine($"Function: {hoveredFrame.FunctionName}");
            tooltipText.AppendLine($"Module: {hoveredFrame.Module}");
            if (hoveredFrame.FrameIndex >= 0)
            {
                tooltipText.AppendLine($"Frame: {hoveredFrame.FrameIndex}");
            }
            tooltipText.AppendLine($"Start: {hoveredFrame.StartTime:F2} ms");
            tooltipText.AppendLine($"End: {hoveredFrame.EndTime:F2} ms");
            tooltipText.AppendLine($"Duration: {hoveredFrame.Duration:F2} ms");
            tooltipText.AppendLine($"Stack Level: {hoveredFrame.StackLevel}");
            if (string.IsNullOrWhiteSpace(hoveredFrame.SourceText) is false)
            {
                tooltipText.AppendLine($"Source: {hoveredFrame.SourceText}");
            }

            // Show call stack path
            var stackPath = GetStackPath(hoveredFrame);
            if (stackPath.Count > 1)
            {
                tooltipText.AppendLine();
                tooltipText.AppendLine("Call Stack:");
                for (int i = 0; i < stackPath.Count; i++)
                {
                    var indent = new string(' ', i * 2);
                    tooltipText.AppendLine($"{indent}→ {stackPath[i].FunctionName}");
                }
            }

            // Set tooltip position
            var tooltipPosition = new Coordinates(
                mouseLocation.X + 5,
                mouseLocation.Y + 10
            );

            _tooltip.LabelText = tooltipText.ToString().Trim();
            _tooltip.TipLocation = mouseLocation;
            _tooltip.LabelLocation = tooltipPosition;
            _tooltip.IsVisible = true;

            _plot.Refresh();
        }
        else
        {
            // Hide crosshair and tooltip
            if (_crosshair.IsVisible || _tooltip.IsVisible)
            {
                _hoveredFrame = null;
                _crosshair.IsVisible = false;
                _tooltip.IsVisible = false;
                _plot.Refresh();
            }
        }
    }

    private void OnMousePressed(object? sender, PointerPressedEventArgs e)
    {
        PointerPointProperties properties = e.GetCurrentPoint(_plot).Properties;
        if (properties.IsLeftButtonPressed is false)
        {
            return;
        }

        var pos = e.GetPosition(_plot);
        Pixel mousePixel = new(pos.X, pos.Y);
        Coordinates mouseLocation = _plot.Plot.GetCoordinates(mousePixel);
        StackFrame? frame = FindFrameAtPosition(mouseLocation.X, mouseLocation.Y);
        if (frame != null)
        {
            FrameClicked?.Invoke(frame);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Handle mouse exit event
    /// </summary>
    private void OnMouseExit(object? sender, PointerEventArgs e)
    {
        _hoveredFrame = null;
        if (_crosshair.IsVisible || _tooltip.IsVisible)
        {
            _crosshair.IsVisible = false;
            _tooltip.IsVisible = false;
            _plot.Refresh();
        }
    }

    /// <summary>
    /// Find stack frame at specified position
    /// </summary>
    private StackFrame? FindFrameAtPosition(double x, double y)
    {
        foreach (var frameInfo in _frameList)
        {
            var frame = frameInfo.FrameData;
            var frameY = (_config.MaxStackDepth - frame.StackLevel - 1) * _config.FrameHeight;
            var frameHeight = _config.FrameHeight * 0.9;

            if (x >= frame.StartTime && x <= frame.EndTime &&
                y >= frameY && y <= frameY + frameHeight)
            {
                return frame;
            }
        }
        return null;
    }

    /// <summary>
    /// Get call stack path
    /// </summary>
    private List<StackFrame> GetStackPath(StackFrame targetFrame)
    {
        var path = new List<StackFrame>();
        
        // Find all stack frames at the same time point, sorted by level
        var timePoint = (targetFrame.StartTime + targetFrame.EndTime) / 2;
        var framesAtTime = _config.StackFrames
            .Where(f => f.StartTime <= timePoint && f.EndTime >= timePoint)
            .OrderBy(f => f.StackLevel)
            .ToList();

        // Build path from root to target frame
        for (int level = 0; level <= targetFrame.StackLevel; level++)
        {
            var frameAtLevel = framesAtTime.FirstOrDefault(f => f.StackLevel == level);
            if (frameAtLevel != null)
            {
                path.Add(frameAtLevel);
            }
        }

        return path;
    }

    /// <summary>
    /// Generate data based on configuration (IImageGenerator interface)
    /// </summary>
    public void GenerateDataByConfig()
    {
        SetupFlameGraph();
    }

    /// <summary>
    /// Update the plot with current data (IImageGenerator interface)
    /// </summary>
    public void UpdatePlot()
    {
        SetupFlameGraph();
        _plot.Refresh();
    }

    /// <summary>
    /// Update X-axis range (IImageGenerator interface)
    /// </summary>
    public void UpdateXRange(double xMin, double xMax, bool isAutoScaleY, bool isXAxisLimit)
    {
        UpdateTimeRange(xMin, xMax);
    }

    /// <summary>
    /// Hide all interactive elements (IImageGenerator interface)
    /// </summary>
    public void HideInteractiveElements()
    {
        if (_crosshair.IsVisible || _tooltip.IsVisible)
        {
            _crosshair.IsVisible = false;
            _tooltip.IsVisible = false;
            _plot.Refresh();
        }
    }

    /// <summary>
    /// Update time range (IFlameGraphGenerator interface)
    /// </summary>
    public void UpdateTimeRange(double startTime, double endTime)
    {
        _plot.Plot.Axes.SetLimitsX(startTime, endTime);
        
        // Update text labels based on new zoom level
        var visibleTimeRange = endTime - startTime;
        foreach (var frameInfo in _frameList)
        {
            frameInfo.UpdateVisuals(_plot, visibleTimeRange);
        }
        
        _plot.Refresh();
    }

    /// <summary>
    /// Regenerate data and update plot
    /// </summary>
    public void RegenerateData()
    {
        SetupFlameGraph();
        _plot.Refresh();
    }

    /// <summary>
    /// Set fixed layout
    /// </summary>
    public void SetFixedLayout(PixelPadding padding)
    {
        _plot.Plot.Layout.Fixed(padding);
    }

    public (double, double) GetDataSourceXRange()
    {
        return (_config.TimelineStart, _config.TimelineEnd);
    }
}
