using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ScottPlot;
using ScottPlot.Avalonia;
using KVP = System.Collections.Generic.KeyValuePair<string, ScottPlot.Color>;
using CurrHoleState = System.Collections.Generic.KeyValuePair<int[], string>;

namespace GridRendering;

class HoleState
{
    public string? state{get; set;}
    public string? saved{get; set;}
    public int[]? index{get; set;}
    public double[]? geom_coord{get;set;}
    public override string ToString() => $"\"state\":{state},\"saved\":{saved},\"index\":[{index[0]},{index[1]}],\"geom_coord\"[{geom_coord[0]},{geom_coord[1]}]";
}

class FingerState
{
    public bool valve{get; set;} //???
    public double[]? position{get; set;}
}

class DataContext
{
    public double step_x{get; set;}
    public double step_y{get; set;}
    public double radius{get; set;}
    public string? name{get; set;}
    public Dictionary<string, HoleState>? map{get; set;}
    public Dictionary<string, FingerState>? fingers{get; set;}    
}
class GridModel
{
    private Dictionary<string, Color> HoleStatesDescription = new(new List<KVP>([new KVP("Done", Colors.Green),
                                                                                 new KVP("Finger", Colors.Brown),
                                                                                 new KVP("Planned", Colors.Yellow),
                                                                                 new KVP("Goal", Colors.Orange),
                                                                                 new KVP("Inaccessible", Colors.Red),
                                                                                 new KVP("Accessible", Colors.Blue),
                                                                                 new KVP("Neutral", Colors.LightBlue)]));
    public void DrawHole(AvaPlot SomeGridPlot, KeyValuePair<int[], string> SomeHoleState, double radius)
    {
        var X = IndexXToDouble(SomeHoleState.Key);
        var Y = IndexYToDouble(SomeHoleState.Key);
        var CircleSchemaOfHole = SomeGridPlot.Plot.Add.Circle(xCenter: X, yCenter: Y, radius: radius);
        CircleSchemaOfHole.FillStyle.Color = HoleStatesDescription[SomeHoleState.Value];
        CircleSchemaOfHole.LineStyle.Color = Colors.Black;
        //GridPlot.Plot.Add.Text($"{SomeHoleState.Key[0]};{SomeHoleState.Key[1]}", X, Y); ПОПРОБОВАТЬ РЕАЛИЗОВАТЬ ПОДПИСЬ КООРДИНАТ ОТВЕРСТИЙ С ПОМОЩЬЮ ScottPlot 5.0
    }
    private const string IOF = "OUTPUT";
    public double XStep{get; set;}
    public double YStep{get; set;}
    public double Radius{get; set;}
    private double[]? XBoards, YBoards;
    private double[] SetXBoards()
    {
        var IndexesArray = CoordsInfo.Keys.ToList();
        var XMin = IndexesArray.Min(Pair => Pair[0]);
        var XMax = IndexesArray.Max(Pair => Pair[0]);
        return [XMin * XStep - Radius - 10, XMax * XStep + Radius + 10];
    }
    private double[] SetYBoards()
    {
        var IndexesArray = CoordsInfo.Keys.ToList();
        var YMin = IndexesArray.Min(Pair => Pair[1]);
        var YMax = IndexesArray.Max(Pair => Pair[1]);
        return [YMin * YStep - Radius - 10, YMax * YStep + Radius + 10];
    }
    private double IndexXToDouble(int[] Index) => Index[0] * XStep;
    private double IndexYToDouble(int[] Index) => Index[1] * YStep;
    public Dictionary<int[], string>? CoordsInfo{get; set;} //отражает структуру сетки
    public AvaPlot? GridPlot{get; set;}
    public void ChangeColorStateOfHole(int[] index, string StringStatus) => CoordsInfo[index] = StringStatus;
    public static CurrHoleState HoleToCoordInfo(HoleState SomeHoleState) => new(SomeHoleState.index, SomeHoleState.saved);
    public void ChangeHoleStates(List<CurrHoleState> HoleStates)
    {
        HoleStates.ForEach(Pair => CoordsInfo[Pair.Key] = Pair.Value);
        DrawAllHoles();
    }

    public void GetHoles(string PathToJson)
    {
        using(var sr = new StreamReader(PathToJson))
        {
            var JsonContainment = sr.ReadToEnd();
            var dataContext = JsonSerializer.Deserialize<DataContext>(JsonContainment);
            var ListOfCoordStates = new List<CurrHoleState>();
            XStep = dataContext.step_x;
            YStep = dataContext.step_y;
            Radius = dataContext.radius;
            foreach(var Pair in dataContext.map)
                ListOfCoordStates.Add(HoleToCoordInfo(Pair.Value));
            CoordsInfo = new(ListOfCoordStates);
            XBoards = SetXBoards();
            YBoards = SetYBoards();
        }
    }   
    public void DrawAllHoles()
    {
        GridPlot.Plot.Clear();
        GridPlot.Plot.Axes.SetLimitsX(XBoards[0], XBoards[1]);
        GridPlot.Plot.Axes.SetLimitsY(YBoards[0], YBoards[1]);
        foreach(var Pair in CoordsInfo)
            DrawHole(GridPlot, Pair, Radius);
        GridPlot.Refresh();
    }
    public void OutputToFile()
    {
        using(var sw = new StreamWriter(IOF))
        {
            foreach(var Pair in CoordsInfo)
            {
                var s = $"[{Pair.Key[0]},{Pair.Key[1]}],{Pair.Value}";
                sw.WriteLine(s);
            }
        }
    }
}
