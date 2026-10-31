using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using NightCourier;
using UnityEditor;
using UnityEngine;

// Export from the live graph, so the artist's guide cannot drift from gameplay data.
public static class CourierMapArtGuideExporter
{
    private static string Number(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    [MenuItem("Tools/Night Courier/Export Map Art Guide")]
    public static void Export()
    {
        const string folder = "ArtPreviews/MapArtGuide";
        Directory.CreateDirectory(folder);
        var svg = new StringBuilder("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"2400\" height=\"1640\" viewBox=\"0 0 1200 820\">\n");
        // Transparent canvas; each named group can be hidden in an illustration editor.
        svg.AppendLine("<g id=\"canvas-border\" fill=\"none\" stroke=\"#B5C3BA\"><rect x=\"1\" y=\"1\" width=\"1198\" height=\"818\"/></g>");
        svg.AppendLine("<g id=\"route-reference-hide-before-export\" fill=\"none\" stroke-width=\"3\" stroke-linecap=\"round\" stroke-linejoin=\"round\">");
        foreach (var edge in CourierStreetMap.Edges)
        {
            string color = edge.Surface == StreetSurface.Heat ? "#DE9185" : edge.Surface == StreetSurface.Water ? "#87BFCC" : edge.Surface == StreetSurface.Bumpy ? "#C4AA79" : "#7D9891";
            svg.Append("<polyline stroke=\"").Append(color).Append("\" ");
            if (edge.Surface == StreetSurface.Bumpy) svg.Append("stroke-dasharray=\"6 6\" ");
            svg.Append("points=\"").Append(string.Join(" ", edge.Points.Select(p => Number(p.X) + "," + Number(p.Y)))).AppendLine("\"/>");
        }
        svg.AppendLine("</g><g id=\"node-reference-hide-before-export\" font-family=\"sans-serif\" font-size=\"14\" text-anchor=\"middle\">");
        var csv = new StringBuilder("node_id,stop_id,x_from_left,y_from_top,x_at_2x,y_at_2x\n");
        foreach (var node in CourierStreetMap.Nodes)
        {
            string x = Number(node.Point.X), y = Number(node.Point.Y);
            svg.Append("<circle cx=\"").Append(x).Append("\" cy=\"").Append(y).AppendLine("\" r=\"6\" fill=\"#E9B46C\" stroke=\"#172128\" stroke-width=\"2\"/>");
            svg.Append("<text x=\"").Append(x).Append("\" y=\"").Append(Number(node.Point.Y - 12)).Append("\" fill=\"#F1E7D2\" stroke=\"#172128\" stroke-width=\"3\" paint-order=\"stroke\">").Append(node.Id).AppendLine("</text>");
            csv.Append(node.Id).Append(',').Append(node.StopId).Append(',').Append(x).Append(',').Append(y).Append(',').Append(Number(node.Point.X * 2)).Append(',').Append(Number(node.Point.Y * 2)).AppendLine();
        }
        svg.AppendLine("</g></svg>");
        File.WriteAllText(folder + "/map-route-guide.svg", svg.ToString(), new UTF8Encoding(false));
        File.WriteAllText(folder + "/map-node-coordinates.csv", csv.ToString(), new UTF8Encoding(false));
        Debug.Log("MAP_ART_GUIDE exported to " + Path.GetFullPath(folder) + ": transparent 2400x1640 guide + node coordinates. Hide guide layers when exporting background artwork.");
    }
}
