using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ScottPlot.Avalonia;

namespace GridRendering;

public partial class MainWindow : Window
{
    private const string Json = "Prolog";
    private AvaPlot gridPlot;
    private TextBox InputJsonPath;
    private GridModel GM;
    private void DrawHoles()
    {
        gridPlot.Plot.Clear();
        GM = new GridModel(){GridPlot = gridPlot};
        var PathToJson = InputJsonPath.Text != "" ? InputJsonPath.Text : Json;
        GM.GetHoles(PathToJson);
        GM.DrawAllHoles();
        gridPlot.Refresh(); 
    }
    public MainWindow()
    {
        InitializeComponent();
        gridPlot = this.Find<AvaPlot>("GridPlot");
        InputJsonPath = this.Find<TextBox>("InputPathToJson");
        InputJsonPath.FontSize = 16;
        InputJsonPath.Text = Json;
        gridPlot.Plot.Axes.SquareUnits();
        DrawHoles();
    }

    //кнопка для отрисовки положения робота и изменения статуса трубок с выделением цветом
    public void ClickHandler(object sender, RoutedEventArgs args) => DrawHoles();

    public void ClickRenew(object sender, RoutedEventArgs args)
    {
        var StatesList = new List<KeyValuePair<int[], string>>();
        //Просто для теста
        GM.CoordsInfo.Keys.ToList().Where(Pair => Pair[0] % 2 == 0 && Pair[1] % 2 == 0).ToList().ForEach(Pair => StatesList.Add(new KeyValuePair<int[], string>(Pair, "Inaccessible")));    
        GM.ChangeHoleStates(StatesList);
        StatesList.Clear();
        //Также просто для теста
        GM.CoordsInfo.Keys.ToList().Where(Pair => Pair[0] % 5 == 0 && Pair[1] % 5 == 0).ToList().ForEach(Pair => StatesList.Add(new KeyValuePair<int[], string>(Pair, "Done")));
        GM.ChangeHoleStates(StatesList);
    }
}