using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using Avalonia.Data.Converters;
using Avalonia.Svg;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Miscellaneous;
using Svg;
using Svg.Model;
using WireMockInspector.ViewModels;
using Color = Microsoft.Msagl.Drawing.Color;

namespace WireMockInspector.Converters;

public class GraphConverter : IValueConverter
{
    public static readonly GraphConverter Instance = new();

    private class EndNode : Node
    {
        public EndNode(string id) : base(id)
        {
            Attr.Shape = Shape.Circle;
        }
    }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Scenario
            {
                Transitions: var transitions, CurrentTransitionId: var currentTransition,
                CurrentState: var currentState
            })
        {
            Graph drawingGraph = new("graph");
            var currentNodeColor = new Color(0, 120, 215);
            var nodeCache = new Dictionary<string, Node>();
            foreach (var n in transitions.SelectMany(x => new[] { x.From, x.To }).OfType<ScenarioNode>().DistinctBy(x => x.Id))
            {

                var node = n switch
                {
                    ScenarioEdgeNode => new EndNode(n.Id),
                    _ => new Node(n.Id)
                };

                nodeCache[n.Id] = node;

                if (n is ScenarioEdgeNode edgeNode)
                {
                    node.LabelText = string.Empty;
                }

                drawingGraph.AddNode(node);
            }

            var visitedColor = Color.Green;
            foreach (var scenarioTransition in transitions)
            {
                var edge = drawingGraph.AddEdge(scenarioTransition.From.Id, scenarioTransition.Id, scenarioTransition.To.Id);

                if (scenarioTransition.Hit)
                {
                    if (nodeCache.TryGetValue(scenarioTransition.From.Id, out var fromNode))
                    {
                        if (scenarioTransition.From is ScenarioEdgeNode { Current: true })
                        {
                            fromNode.Attr.FillColor = currentNodeColor;
                        }
                        else
                        {
                            fromNode.Attr.FillColor = visitedColor;
                        }
                    }

                    if (nodeCache.TryGetValue(scenarioTransition.To.Id, out var toNode) && toNode is EndNode)
                    {
                        toNode.Attr.FillColor = currentNodeColor;
                    }
                }

                if (scenarioTransition.Id == currentTransition)
                {
                    edge.Attr.Color = Color.Orange;
                }
                else if (scenarioTransition.Hit)
                {
                    edge.Attr.Color = visitedColor;
                }
                else
                {
                    edge.Attr.Color = Color.White;
                }
            }

            //create the graph content 

            drawingGraph.Directed = true;

            drawingGraph.CreateGeometryGraph();
            var maxTextLength = drawingGraph.Nodes.Select(x => x.LabelText?.Length ?? 0).Max() * 8 + 16;
            var recWidth = Math.Max(maxTextLength, 100);

            foreach (var n in drawingGraph.Nodes)
            {
                if (n.Attr.Id == currentState)
                {

                    n.Attr.FillColor = currentNodeColor;
                }

                n.GeometryNode.BoundaryCurve = n switch
                {
                    EndNode _ => CurveFactory.CreateCircle(10, new Microsoft.Msagl.Core.Geometry.Point(0, 0)),
                    _ => CurveFactory.CreateRectangleWithRoundedCorners(recWidth, 50, 3, 3,
                        new Microsoft.Msagl.Core.Geometry.Point(0, 0))
                };
                n.Attr.Color = Color.White;
                n.Label.FontColor = Color.White;
            }

            AssignLabelsDimensions(drawingGraph, maxTextLength);
            drawingGraph.Attr.LayerDirection = LayerDirection.LR;

            LayoutHelpers.CalculateLayout(drawingGraph.GeometryGraph, new SugiyamaLayoutSettings(), null);

            var svg = PrintSvgAsString(drawingGraph);

            var al = new AvaloniaAssetLoader();
            XmlDocument xml = new();
            xml.LoadXml(svg);
            return new Avalonia.Svg.SvgImage()
            {
                Source = new SvgSource()
                {
                    Picture = SvgExtensions.ToModel(SvgDocument.Open(xml), al, out _, out _)
                }
            };
        }

        return null;
    }


    private static string PrintSvgAsString(Graph drawingGraph)
    {
        var ms = new MemoryStream();
        var writer = new StreamWriter(ms);
        var svgWriter = new SvgGraphWriter(writer.BaseStream, drawingGraph);
        svgWriter.Write();
        ms.Position = 0;
        var sr = new StreamReader(ms);
        var payload = sr.ReadToEnd();
        return payload
            .Replace("font-family=\"Arial\"", "font-family=\"Consolas\"");
    }

    private static void AssignLabelsDimensions(Graph drawingGraph, int maxTextLength)
    {
        // In general, the label dimensions should depend on the viewer
        foreach (var na in drawingGraph.Nodes)
        {
            if (string.IsNullOrWhiteSpace(na.LabelText))
            {
                continue;
            }

            var width = 8 * na.LabelText.Length;
            var height = 12;

            na.Label.Width = width;
            na.Label.Height = height;
            na.Label.FontSize = height;
            na.Label.FontName = "Consolas";
        }

        // init geometry labels as well
        foreach (var edge in drawingGraph.Edges)
        {
            // again setting the dimensions, that should depend on Drawing.Label and the viewer, blindly
            // edge.Label.GeometryLabel.Width = 140;
            // edge.Label.GeometryLabel.Height = 60;
        }
    }


    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}